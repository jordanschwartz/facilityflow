using Amazon;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using FacilityFlow.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FacilityFlow.Infrastructure.Services;

public class SesEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SesEmailService> _logger;

    public SesEmailService(IConfiguration configuration, ILogger<SesEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody, byte[]? attachment = null, string? attachmentName = null)
    {
        var fromAddress = _configuration["Aws:Ses:FromAddress"];
        var fromName = _configuration["Aws:Ses:FromName"] ?? "FacilityFlow";
        var region = _configuration["Aws:Ses:Region"] ?? "us-east-1";

        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            _logger.LogWarning("SES FromAddress is not configured. Skipping email to {To}", to);
            return;
        }

        // In dev, redirect all emails to the override recipient
        var devOverride = _configuration["Aws:Ses:DevOverrideRecipient"];
        var actualRecipient = to;
        if (!string.IsNullOrWhiteSpace(devOverride))
        {
            _logger.LogInformation("Dev override active: redirecting email from {OriginalTo} to {DevTo}", to, devOverride);
            subject = $"[DEV → {to}] {subject}";
            actualRecipient = devOverride;
        }

        try
        {
            var profile = _configuration["Aws:Profile"];
            var chain = !string.IsNullOrWhiteSpace(profile)
                ? new Amazon.Runtime.CredentialManagement.CredentialProfileStoreChain()
                : null;
            Amazon.Runtime.AWSCredentials? credentials = null;
            if (chain != null && chain.TryGetAWSCredentials(profile!, out var creds))
                credentials = creds;
            using var client = credentials != null
                ? new AmazonSimpleEmailServiceV2Client(credentials, RegionEndpoint.GetBySystemName(region))
                : new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(region));

            if (attachment is { Length: > 0 } && !string.IsNullOrWhiteSpace(attachmentName))
            {
                await SendRawEmailAsync(client, fromAddress, fromName, actualRecipient, subject, htmlBody, attachment, attachmentName);
            }
            else
            {
                await SendSimpleEmailAsync(client, fromAddress, fromName, actualRecipient, subject, htmlBody);
            }

            _logger.LogInformation("Email sent to {To} with subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
        }
    }

    private static async Task SendSimpleEmailAsync(
        AmazonSimpleEmailServiceV2Client client, string fromAddress, string fromName,
        string to, string subject, string htmlBody)
    {
        var request = new SendEmailRequest
        {
            FromEmailAddress = $"{fromName} <{fromAddress}>",
            Destination = new Destination { ToAddresses = new List<string> { to } },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = htmlBody }
                    }
                }
            }
        };

        await client.SendEmailAsync(request);
    }

    private static async Task SendRawEmailAsync(
        AmazonSimpleEmailServiceV2Client client, string fromAddress, string fromName,
        string to, string subject, string htmlBody, byte[] attachment, string attachmentName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        bodyBuilder.Attachments.Add(attachmentName, attachment, new ContentType("application", "pdf"));

        message.Body = bodyBuilder.ToMessageBody();

        using var memoryStream = new MemoryStream();
        await message.WriteToAsync(memoryStream);
        memoryStream.Position = 0;

        var request = new SendEmailRequest
        {
            FromEmailAddress = $"{fromName} <{fromAddress}>",
            Destination = new Destination { ToAddresses = new List<string> { to } },
            Content = new EmailContent
            {
                Raw = new RawMessage { Data = memoryStream }
            }
        };

        await client.SendEmailAsync(request);
    }
}
