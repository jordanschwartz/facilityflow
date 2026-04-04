using System.Text.Json;
using System.Text.RegularExpressions;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FacilityFlow.Infrastructure.Services;

public partial class InboundEmailService : IInboundEmailService
{
    private readonly AppDbContext _db;
    private readonly IFileStorageService _fileStorage;
    private readonly IActivityLogger _activityLogger;
    private readonly ILogger<InboundEmailService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public InboundEmailService(
        AppDbContext db,
        IFileStorageService fileStorage,
        IActivityLogger activityLogger,
        ILogger<InboundEmailService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _db = db;
        _fileStorage = fileStorage;
        _activityLogger = activityLogger;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task ProcessInboundEmailAsync(string snsMessageBody)
    {
        try
        {
            var snsMessage = JsonDocument.Parse(snsMessageBody);
            var messageType = snsMessage.RootElement.GetProperty("Type").GetString();

            if (messageType == "SubscriptionConfirmation")
            {
                await HandleSubscriptionConfirmationAsync(snsMessage);
                return;
            }

            if (messageType != "Notification")
            {
                _logger.LogWarning("Unknown SNS message type: {Type}", messageType);
                return;
            }

            var sesNotificationJson = snsMessage.RootElement.GetProperty("Message").GetString();
            if (string.IsNullOrEmpty(sesNotificationJson))
            {
                _logger.LogWarning("Empty SES notification message");
                return;
            }

            var sesNotification = JsonDocument.Parse(sesNotificationJson);
            var mailElement = sesNotification.RootElement.GetProperty("mail");
            var sesMessageId = mailElement.GetProperty("messageId").GetString() ?? string.Empty;

            // Check for duplicate
            var exists = await _db.InboundEmails.AnyAsync(e => e.MessageId == sesMessageId);
            if (exists)
            {
                _logger.LogInformation("Duplicate inbound email skipped: {MessageId}", sesMessageId);
                return;
            }

            // Extract email content (base64-encoded in the "content" field)
            var content = sesNotification.RootElement.GetProperty("content").GetString();
            if (string.IsNullOrEmpty(content))
            {
                _logger.LogWarning("No email content in SES notification for {MessageId}", sesMessageId);
                return;
            }

            var emailBytes = Convert.FromBase64String(content);
            using var emailStream = new MemoryStream(emailBytes);
            var mimeMessage = await MimeMessage.LoadAsync(emailStream);

            var fromMailbox = mimeMessage.From.Mailboxes.FirstOrDefault();
            var fromAddress = fromMailbox?.Address ?? string.Empty;
            var fromName = fromMailbox?.Name;
            var subject = mimeMessage.Subject ?? string.Empty;

            // Extract reply-to address for service request resolution
            var replyToAddress = mimeMessage.To.Mailboxes
                .Select(m => m.Address)
                .FirstOrDefault(a => a.Contains("reply+", StringComparison.OrdinalIgnoreCase));

            var serviceRequestId = await ResolveServiceRequestIdAsync(replyToAddress, subject);

            // Serialize headers as JSON
            var headers = mimeMessage.Headers
                .Select(h => new { h.Field, h.Value })
                .ToList();
            var rawHeaders = JsonSerializer.Serialize(headers);

            var inboundEmail = new InboundEmail
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = serviceRequestId,
                FromAddress = fromAddress,
                FromName = fromName,
                Subject = subject,
                BodyText = mimeMessage.TextBody,
                BodyHtml = mimeMessage.HtmlBody,
                ReceivedAt = DateTime.UtcNow,
                MessageId = sesMessageId,
                RawHeaders = rawHeaders
            };

            // Save attachments
            foreach (var attachment in mimeMessage.Attachments)
            {
                if (attachment is not MimePart mimePart || mimePart.Content == null) continue;

                using var attachmentStream = new MemoryStream();
                await mimePart.Content.DecodeToAsync(attachmentStream);
                attachmentStream.Position = 0;

                var fileName = mimePart.FileName ?? $"attachment-{Guid.NewGuid():N}";
                var contentType = mimePart.ContentType.MimeType;
                var fileSize = attachmentStream.Length;

                var (url, _) = await _fileStorage.SaveFileAsync(
                    $"inbound-emails/{inboundEmail.Id}",
                    attachmentStream,
                    fileName,
                    contentType);

                inboundEmail.Attachments.Add(new InboundEmailAttachment
                {
                    Id = Guid.NewGuid(),
                    InboundEmailId = inboundEmail.Id,
                    FileName = fileName,
                    ContentType = contentType,
                    FilePath = url,
                    FileSize = fileSize
                });
            }

            _db.InboundEmails.Add(inboundEmail);
            await _db.SaveChangesAsync();

            // Log activity if linked to a service request
            if (serviceRequestId.HasValue)
            {
                var attachmentCount = inboundEmail.Attachments.Count;
                var description = $"Received email from {fromName ?? fromAddress}";
                if (attachmentCount > 0)
                    description += $" with {attachmentCount} attachment{(attachmentCount > 1 ? "s" : "")}";

                var sr = await _db.ServiceRequests
                    .Include(s => s.WorkOrder)
                    .FirstAsync(s => s.Id == serviceRequestId.Value);

                await _activityLogger.LogAsync(
                    serviceRequestId.Value,
                    sr.WorkOrder?.Id,
                    description,
                    ActivityLogCategory.Communication,
                    fromName ?? fromAddress,
                    null);
            }

            _logger.LogInformation("Processed inbound email {MessageId} from {From}, linked to SR {ServiceRequestId}",
                sesMessageId, fromAddress, serviceRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process inbound email");
            throw;
        }
    }

    public async Task<Guid?> ResolveServiceRequestIdAsync(string? replyToAddress, string? subject)
    {
        // Try reply-to address pattern: reply+{workOrderNumber}@
        if (!string.IsNullOrEmpty(replyToAddress))
        {
            var match = ReplyToPattern().Match(replyToAddress);
            if (match.Success)
            {
                var workOrderNumber = match.Groups[1].Value;
                var sr = await _db.ServiceRequests
                    .FirstOrDefaultAsync(s => s.WorkOrderNumber == workOrderNumber);
                if (sr != null) return sr.Id;
            }
        }

        // Fallback: try parsing work order number from subject
        if (!string.IsNullOrEmpty(subject))
        {
            var match = WorkOrderNumberPattern().Match(subject);
            if (match.Success)
            {
                var workOrderNumber = match.Value;
                var sr = await _db.ServiceRequests
                    .FirstOrDefaultAsync(s => s.WorkOrderNumber == workOrderNumber);
                if (sr != null) return sr.Id;
            }
        }

        return null;
    }

    private async Task HandleSubscriptionConfirmationAsync(JsonDocument snsMessage)
    {
        var subscribeUrl = snsMessage.RootElement.GetProperty("SubscribeURL").GetString();
        if (string.IsNullOrEmpty(subscribeUrl))
        {
            _logger.LogWarning("SubscriptionConfirmation missing SubscribeURL");
            return;
        }

        _logger.LogInformation("Confirming SNS subscription: {Url}", subscribeUrl);
        var client = _httpClientFactory.CreateClient();
        await client.GetAsync(subscribeUrl);
    }

    [GeneratedRegex(@"reply\+([^@]+)@", RegexOptions.IgnoreCase)]
    private static partial Regex ReplyToPattern();

    [GeneratedRegex(@"WO-\d+", RegexOptions.IgnoreCase)]
    private static partial Regex WorkOrderNumberPattern();
}
