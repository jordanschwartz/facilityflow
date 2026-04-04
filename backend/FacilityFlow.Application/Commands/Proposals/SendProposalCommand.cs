using System.Security.Claims;
using FacilityFlow.Application.DTOs.Proposals;
using FacilityFlow.Application.Queries.Proposals;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FacilityFlow.Application.Commands.Proposals;

public record SendProposalCommand(Guid Id) : IRequest<ProposalDto>;

public class SendProposalCommandHandler : IRequestHandler<SendProposalCommand, ProposalDto>
{
    private readonly IProposalRepository _proposals;
    private readonly IRepository<ProposalVersion> _versions;
    private readonly IRepository<OutboundEmail> _outboundEmails;
    private readonly INotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IEmailService _emailService;
    private readonly IProposalPdfService _pdfService;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SendProposalCommandHandler(
        IProposalRepository proposals,
        IRepository<ProposalVersion> versions,
        IRepository<OutboundEmail> outboundEmails,
        INotificationService notifications,
        IActivityLogger activityLogger,
        IEmailService emailService,
        IProposalPdfService pdfService,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _proposals = proposals;
        _versions = versions;
        _outboundEmails = outboundEmails;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _emailService = emailService;
        _pdfService = pdfService;
        _fileStorage = fileStorage;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ProposalDto> Handle(SendProposalCommand command, CancellationToken cancellationToken)
    {
        var proposal = await _proposals.Query()
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Proposal not found.");

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only draft proposals can be sent.");

        _versions.Add(new ProposalVersion
        {
            ProposalId = proposal.Id,
            VersionNumber = proposal.Version,
            Price = proposal.Price,
            VendorCost = proposal.VendorCost,
            MarginPercentage = proposal.MarginPercentage,
            ScopeOfWork = proposal.ScopeOfWork,
            Summary = proposal.Summary,
            NotToExceedPrice = proposal.NotToExceedPrice,
            CreatedAt = DateTime.UtcNow,
            ChangeNotes = "Sent to client"
        });

        proposal.Status = ProposalStatus.Sent;
        proposal.SentAt = DateTime.UtcNow;

        // Move SR to PendingApproval
        proposal.ServiceRequest.Status = ServiceRequestStatus.PendingApproval;
        proposal.ServiceRequest.UpdatedAt = DateTime.UtcNow;

        await _proposals.SaveChangesAsync();

        if (proposal.ServiceRequest.Client.UserId.HasValue)
        {
            await _notifications.CreateAsync(
                proposal.ServiceRequest.Client.UserId.Value,
                "Proposal.Sent",
                $"A proposal has been sent for your service request: {proposal.ServiceRequest.Title}",
                $"/proposals/view/{proposal.PublicToken}");
        }

        // Send email to client
        var client = proposal.ServiceRequest.Client;
        var sr = proposal.ServiceRequest;
        var woNum = sr.WorkOrderNumber ?? $"WO-{sr.Id.ToString("N")[..8].ToUpper()}";
        var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var viewUrl = $"{baseUrl}/proposals/view/{proposal.PublicToken}";

        var (emailSubject, emailHtml) = EmailTemplates.ProposalSent(
            client.ContactName, woNum, sr.Title,
            proposal.Price.ToString("C"), viewUrl);

        var pdfBytes = await _pdfService.GenerateAsync(proposal.Id);
        var proposalNum = proposal.ProposalNumber ?? $"P-{proposal.Id.ToString("N")[..8].ToUpper()}";
        var pdfFileName = $"Proposal-{proposalNum}.pdf";

        var replyTo = Helpers.EmailAddressing.GetReplyToAddress(woNum);
        await _emailService.SendEmailAsync(client.Email, emailSubject, emailHtml, pdfBytes, pdfFileName, replyTo);

        // Record outbound email
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value ?? "System";
        Guid.TryParse(userIdClaim, out var sentById);

        var outboundEmail = new OutboundEmail
        {
            ServiceRequestId = proposal.ServiceRequestId,
            RecipientAddress = client.Email,
            RecipientName = client.ContactName,
            Subject = emailSubject,
            BodyHtml = emailHtml,
            SentAt = DateTime.UtcNow,
            SentById = sentById,
            SentByName = userName,
            EmailType = OutboundEmailType.ProposalSent,
            ConversationId = woNum
        };

        using var pdfStream = new MemoryStream(pdfBytes);
        var (attachmentUrl, _) = await _fileStorage.SaveFileAsync(
            "outbound-email-attachments", pdfStream, pdfFileName, "application/pdf");
        outboundEmail.Attachments.Add(new OutboundEmailAttachment
        {
            FileName = pdfFileName,
            ContentType = "application/pdf",
            FilePath = attachmentUrl,
            FileSize = pdfBytes.Length
        });

        _outboundEmails.Add(outboundEmail);
        await _outboundEmails.SaveChangesAsync();

        await _activityLogger.LogAsync(
            proposal.ServiceRequestId, null,
            $"Sent proposal to {client.ContactName} via email",
            ActivityLogCategory.Communication, string.Empty, null);

        var result = await _proposals.GetWithFullDetailsAsync(command.Id);
        return GetProposalByIdQueryHandler.BuildProposalDto(result!);
    }
}
