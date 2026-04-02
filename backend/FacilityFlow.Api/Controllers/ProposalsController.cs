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

    public ProposalsController(AppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
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

        var quote = await _db.Quotes.FindAsync(req.QuoteId)
            ?? throw new NotFoundException("Quote not found.");

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            QuoteId = req.QuoteId,
            Price = req.Price,
            ScopeOfWork = req.ScopeOfWork,
            Status = ProposalStatus.Draft,
            PublicToken = "pr-" + Guid.NewGuid().ToString("N")
        };

        _db.Proposals.Add(proposal);
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
        var proposal = await _db.Proposals.FindAsync(id)
            ?? throw new NotFoundException("Proposal not found.");

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException("Only draft proposals can be updated.");

        proposal.Price = req.Price;
        proposal.ScopeOfWork = req.ScopeOfWork;
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
        var proposal = await _db.Proposals.FirstOrDefaultAsync(p => p.PublicToken == token)
            ?? throw new NotFoundException("Proposal not found.");

        return Ok(await BuildProposalDto(proposal.Id));
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

        return new ProposalDto(
            proposal.Id,
            proposal.ServiceRequestId,
            proposal.QuoteId,
            proposal.Price,
            proposal.ScopeOfWork,
            proposal.Status.ToString(),
            proposal.PublicToken,
            proposal.SentAt,
            proposal.ClientResponse,
            proposal.ClientRespondedAt,
            srSummary,
            quoteSummary
        );
    }
}
