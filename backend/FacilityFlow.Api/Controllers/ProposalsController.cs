using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Proposals;
using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.VendorInvites;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using FacilityFlow.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Authorize]
public class ProposalsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;
    private readonly IAiSummaryService _aiSummaryService;

    private const string DefaultTermsAndConditions =
        "1. Payment Terms: Payment is due within 30 days of work completion.\n" +
        "2. Warranty: All work is warranted for 90 days from completion.\n" +
        "3. Scope: This proposal covers only the work described above. Additional work will require a separate proposal.\n" +
        "4. Scheduling: Proposed dates are estimates and subject to change based on site conditions and availability.\n" +
        "5. Access: Client agrees to provide reasonable access to the work area during scheduled hours.\n" +
        "6. Cancellation: Cancellation after approval may be subject to a cancellation fee for materials already procured.";

    public ProposalsController(AppDbContext db, NotificationService notifications, IAiSummaryService aiSummaryService)
    {
        _db = db;
        _notifications = notifications;
        _aiSummaryService = aiSummaryService;
    }

    [HttpPost("api/service-requests/{serviceRequestId:guid}/proposals")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create(Guid serviceRequestId, [FromBody] CreateProposalRequest req)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.Proposal)
            .Include(s => s.Client)
            .FirstOrDefaultAsync(s => s.Id == serviceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        if (sr.Proposal != null)
            throw new InvalidOperationException("A proposal already exists for this service request.");

        var quote = await _db.Quotes
            .Include(q => q.Attachments)
            .FirstOrDefaultAsync(q => q.Id == req.QuoteId)
            ?? throw new NotFoundException("Quote not found.");

        var vendorCost = quote.Price;
        var price = vendorCost * (1 + req.MarginPercentage / 100m);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            QuoteId = req.QuoteId,
            VendorCost = vendorCost,
            MarginPercentage = req.MarginPercentage,
            Price = price,
            ScopeOfWork = req.ScopeOfWork ?? quote.ScopeOfWork,
            Summary = req.Summary,
            NotToExceedPrice = req.NotToExceedPrice,
            UseNtePricing = req.UseNtePricing,
            ProposedStartDate = req.ProposedStartDate,
            EstimatedDuration = req.EstimatedDuration,
            TermsAndConditions = req.TermsAndConditions ?? DefaultTermsAndConditions,
            InternalNotes = req.InternalNotes,
            Status = ProposalStatus.Draft,
            Version = 1,
            PublicToken = "pr-" + Guid.NewGuid().ToString("N")
        };

        _db.Proposals.Add(proposal);

        // Copy selected attachments or all quote attachments
        var attachmentIds = req.AttachmentIds != null && req.AttachmentIds.Length > 0
            ? req.AttachmentIds
            : quote.Attachments.Select(a => a.Id).ToArray();

        foreach (var attachmentId in attachmentIds)
        {
            _db.ProposalAttachments.Add(new ProposalAttachment
            {
                ProposalId = proposal.Id,
                AttachmentId = attachmentId
            });
        }

        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = proposal.Id }, await BuildProposalDto(proposal.Id));
    }

    [HttpGet("api/service-requests/{serviceRequestId:guid}/proposals")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetByServiceRequest(Guid serviceRequestId)
    {
        var proposal = await _db.Proposals
            .FirstOrDefaultAsync(p => p.ServiceRequestId == serviceRequestId);
        if (proposal == null) return NoContent();
        var dto = await BuildProposalDto(proposal.Id);
        return Ok(dto);
    }

    [HttpGet("api/proposals/{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var dto = await BuildProposalDto(id);
        return Ok(dto);
    }

    [HttpPut("api/proposals/{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProposalRequest req)
    {
        var proposal = await _db.Proposals
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Proposal not found.");

        // If proposal was already sent, create a version snapshot before updating
        if (proposal.Status == ProposalStatus.Sent)
        {
            CreateVersionSnapshot(proposal, req.ChangeNotes);
            proposal.Status = ProposalStatus.Draft;
        }
        else if (proposal.Status != ProposalStatus.Draft)
        {
            throw new InvalidOperationException("Only draft or sent proposals can be updated.");
        }

        if (req.MarginPercentage.HasValue)
        {
            proposal.MarginPercentage = req.MarginPercentage.Value;
            proposal.Price = proposal.VendorCost * (1 + req.MarginPercentage.Value / 100m);
        }

        if (req.ScopeOfWork != null) proposal.ScopeOfWork = req.ScopeOfWork;
        if (req.Summary != null) proposal.Summary = req.Summary;
        if (req.NotToExceedPrice.HasValue) proposal.NotToExceedPrice = req.NotToExceedPrice;
        if (req.UseNtePricing.HasValue) proposal.UseNtePricing = req.UseNtePricing.Value;
        if (req.ProposedStartDate.HasValue) proposal.ProposedStartDate = req.ProposedStartDate;
        if (req.EstimatedDuration != null) proposal.EstimatedDuration = req.EstimatedDuration;
        if (req.TermsAndConditions != null) proposal.TermsAndConditions = req.TermsAndConditions;
        if (req.InternalNotes != null) proposal.InternalNotes = req.InternalNotes;

        if (req.AttachmentIds != null)
        {
            // Replace attachments
            _db.ProposalAttachments.RemoveRange(proposal.Attachments);
            foreach (var attachmentId in req.AttachmentIds)
            {
                _db.ProposalAttachments.Add(new ProposalAttachment
                {
                    ProposalId = proposal.Id,
                    AttachmentId = attachmentId
                });
            }
        }

        proposal.Version++;
        await _db.SaveChangesAsync();

        return Ok(await BuildProposalDto(id));
    }

    [HttpPost("api/proposals/{id:guid}/send")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Send(Guid id)
    {
        var proposal = await _db.Proposals
            .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Proposal not found.");

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only draft proposals can be sent.");

        // Create version snapshot on send
        CreateVersionSnapshot(proposal, "Sent to client");

        proposal.Status = ProposalStatus.Sent;
        proposal.SentAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify client
        await _notifications.CreateAsync(
            proposal.ServiceRequest.Client.UserId,
            "Proposal.Sent",
            $"A proposal has been sent for your service request: {proposal.ServiceRequest.Title}",
            $"/proposals/view/{proposal.PublicToken}");

        return Ok(await BuildProposalDto(id));
    }

    [HttpPost("api/proposals/{id:guid}/generate-summary")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GenerateSummary(Guid id, [FromBody] GenerateSummaryRequest req)
    {
        // Verify proposal exists
        var exists = await _db.Proposals.AnyAsync(p => p.Id == id);
        if (!exists) throw new NotFoundException("Proposal not found.");

        var summary = await _aiSummaryService.GenerateProposalSummaryAsync(
            req.ScopeOfWork, req.Notes, req.JobDescription, req.AdditionalContext);

        return Ok(new GenerateSummaryResponse(summary));
    }

    [HttpPost("api/proposals/{id:guid}/respond")]
    public async Task<IActionResult> Respond(Guid id, [FromBody] RespondToProposalRequest req)
    {
        Proposal? proposal;

        // Support both authenticated and token-based access
        if (!string.IsNullOrWhiteSpace(req.Token))
        {
            proposal = await _db.Proposals
                .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
                .Include(p => p.Quote)
                .FirstOrDefaultAsync(p => p.PublicToken == req.Token)
                ?? throw new NotFoundException("Proposal not found.");
        }
        else
        {
            proposal = await _db.Proposals
                .Include(p => p.ServiceRequest).ThenInclude(sr => sr.Client)
                .Include(p => p.Quote)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new NotFoundException("Proposal not found.");
        }

        if (proposal.Status != ProposalStatus.Sent)
            throw new InvalidOperationException("Proposal is not in a state that can be responded to.");

        var decision = req.Decision.ToLower();
        if (decision != "approved" && decision != "rejected")
            throw new InvalidOperationException("Decision must be 'approved' or 'rejected'.");

        proposal.Status = decision == "approved" ? ProposalStatus.Approved : ProposalStatus.Rejected;
        proposal.ClientResponse = req.ClientResponse;
        proposal.ClientRespondedAt = DateTime.UtcNow;

        if (proposal.Status == ProposalStatus.Approved)
        {
            // Transition SR to Approved
            proposal.ServiceRequest.Status = ServiceRequestStatus.Approved;
            proposal.ServiceRequest.UpdatedAt = DateTime.UtcNow;

            // Create work order
            var workOrder = new WorkOrder
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = proposal.ServiceRequestId,
                ProposalId = proposal.Id,
                VendorId = proposal.Quote.VendorId,
                Status = WorkOrderStatus.Assigned
            };
            _db.WorkOrders.Add(workOrder);

            // Notify vendor (only if vendor has a linked user account)
            var vendor = await _db.Vendors.FindAsync(proposal.Quote.VendorId);
            if (vendor != null && vendor.UserId.HasValue)
            {
                await _notifications.CreateAsync(vendor.UserId.Value, "WorkOrder.Assigned",
                    $"You have been assigned a new work order for: {proposal.ServiceRequest.Title}",
                    $"/work-orders/{workOrder.Id}");
            }

            // Notify operators
            var operators = await _db.Users.Where(u => u.Role == UserRole.Operator).ToListAsync();
            foreach (var op in operators)
            {
                await _notifications.CreateAsync(op.Id, "Proposal.Approved",
                    $"Proposal approved for: {proposal.ServiceRequest.Title}",
                    $"/service-requests/{proposal.ServiceRequestId}");
            }
        }

        await _db.SaveChangesAsync();
        return Ok(await BuildProposalDto(proposal.Id));
    }

    [HttpGet("api/proposals/view/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewByToken(string token)
    {
        var proposal = await _db.Proposals
            .Include(p => p.ServiceRequest)
            .Include(p => p.Attachments).ThenInclude(pa => pa.Attachment)
            .FirstOrDefaultAsync(p => p.PublicToken == token)
            ?? throw new NotFoundException("Proposal not found.");

        var attachments = proposal.Attachments.Select(pa =>
            new ClientProposalAttachmentDto(pa.Attachment.Id, pa.Attachment.Filename, pa.Attachment.Url))
            .ToList();

        var clientDto = new ClientProposalDto(
            proposal.Id,
            proposal.Price,
            proposal.ScopeOfWork,
            proposal.Summary,
            proposal.NotToExceedPrice,
            proposal.UseNtePricing,
            proposal.ProposedStartDate,
            proposal.EstimatedDuration,
            proposal.TermsAndConditions,
            proposal.Status.ToString(),
            proposal.SentAt,
            proposal.ClientResponse,
            proposal.ClientRespondedAt,
            attachments,
            new ClientProposalServiceRequestDto(
                proposal.ServiceRequest.Title,
                proposal.ServiceRequest.Location,
                proposal.ServiceRequest.Category));

        return Ok(clientDto);
    }

    [HttpGet("api/proposals/{id:guid}/versions")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetVersions(Guid id)
    {
        var exists = await _db.Proposals.AnyAsync(p => p.Id == id);
        if (!exists) throw new NotFoundException("Proposal not found.");

        var versions = await _db.ProposalVersions
            .Where(v => v.ProposalId == id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ProposalVersionDto(
                v.Id, v.ProposalId, v.VersionNumber, v.Price,
                v.VendorCost, v.MarginPercentage, v.ScopeOfWork,
                v.Summary, v.NotToExceedPrice, v.CreatedAt, v.ChangeNotes))
            .ToListAsync();

        return Ok(versions);
    }

    private void CreateVersionSnapshot(Proposal proposal, string? changeNotes)
    {
        var version = new ProposalVersion
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
            ChangeNotes = changeNotes
        };
        _db.ProposalVersions.Add(version);
    }

    private async Task<ProposalDto> BuildProposalDto(Guid proposalId)
    {
        var proposal = await _db.Proposals
            .Include(p => p.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(p => p.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(p => p.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(p => p.Quote)
            .Include(p => p.Attachments).ThenInclude(pa => pa.Attachment)
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == proposalId)
            ?? throw new NotFoundException("Proposal not found.");

        var sr = proposal.ServiceRequest;
        var srSummary = new ServiceRequestSummaryDto(
            sr.Id,
            sr.Title,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null
        );

        var quoteSummary = new QuoteSummaryDto(
            proposal.Quote.Id,
            proposal.Quote.Status.ToString(),
            proposal.Quote.Price == 0 ? null : proposal.Quote.Price,
            proposal.Quote.SubmittedAt
        );

        var attachments = proposal.Attachments.Select(pa =>
            new ProposalAttachmentDto(
                pa.Id,
                pa.ProposalId,
                pa.AttachmentId,
                new AttachmentDto(pa.Attachment.Id, pa.Attachment.Url, pa.Attachment.Filename, pa.Attachment.MimeType)))
            .ToList();

        var versions = proposal.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new ProposalVersionDto(
                v.Id, v.ProposalId, v.VersionNumber, v.Price,
                v.VendorCost, v.MarginPercentage, v.ScopeOfWork,
                v.Summary, v.NotToExceedPrice, v.CreatedAt, v.ChangeNotes))
            .ToList();

        return new ProposalDto(
            proposal.Id,
            proposal.ServiceRequestId,
            proposal.QuoteId,
            proposal.Price,
            proposal.VendorCost,
            proposal.MarginPercentage,
            proposal.ScopeOfWork,
            proposal.Summary,
            proposal.SummaryGeneratedByAi,
            proposal.NotToExceedPrice,
            proposal.UseNtePricing,
            proposal.ProposedStartDate,
            proposal.EstimatedDuration,
            proposal.TermsAndConditions,
            proposal.InternalNotes,
            proposal.Status.ToString(),
            proposal.PublicToken,
            proposal.Version,
            proposal.SentAt,
            proposal.ClientResponse,
            proposal.ClientRespondedAt,
            srSummary,
            quoteSummary,
            attachments,
            versions
        );
    }
}
