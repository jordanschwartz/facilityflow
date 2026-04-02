using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.VendorInvites;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.StateMachines;
using FacilityFlow.Infrastructure.Persistence;
using FacilityFlow.Infrastructure.Services;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/service-requests")]
[Authorize]
public class ServiceRequestsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;

    public ServiceRequestsController(AppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? priority,
        [FromQuery] Guid? clientId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.ServiceRequests
            .Include(sr => sr.Client).ThenInclude(c => c.User)
            .Include(sr => sr.CreatedBy)
            .Include(sr => sr.Quotes)
            .Include(sr => sr.Proposal)
            .Include(sr => sr.WorkOrder)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ServiceRequestStatus>(status, true, out var parsedStatus))
            query = query.Where(sr => sr.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Priority>(priority, true, out var parsedPriority))
            query = query.Where(sr => sr.Priority == parsedPriority);

        if (clientId.HasValue)
            query = query.Where(sr => sr.ClientId == clientId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(sr => sr.Title.ToLower().Contains(search.ToLower())
                                   || sr.Description.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(sr => sr.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummary).ToList();
        return Ok(new PagedResult<ServiceRequestSummaryDto>(dtos, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestRequest req)
    {
        var client = await _db.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == req.ClientId)
            ?? throw new NotFoundException("Client not found.");

        var userId = User.GetUserId();

        var sr = new ServiceRequest
        {
            Id = Guid.NewGuid(),
            Title = req.Title,
            Description = req.Description,
            Location = req.Location,
            Category = req.Category,
            Priority = req.Priority,
            Status = ServiceRequestStatus.New,
            ClientId = req.ClientId,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ServiceRequests.Add(sr);
        await _db.SaveChangesAsync();

        // Notify client
        await _notifications.CreateAsync(client.UserId, "ServiceRequest.Created",
            $"A new service request '{sr.Title}' has been created for your account.",
            $"/service-requests/{sr.Id}");

        var result = await GetSrWithDetails(sr.Id);
        return CreatedAtAction(nameof(GetById), new { id = sr.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.Client).ThenInclude(c => c.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.Quotes)
            .Include(s => s.Proposal)
            .Include(s => s.WorkOrder)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Service request not found.");

        return Ok(MapToDetail(sr));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceRequestRequest req)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.Client).ThenInclude(c => c.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.Quotes)
            .Include(s => s.Proposal)
            .Include(s => s.WorkOrder)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Service request not found.");

        sr.Title = req.Title;
        sr.Description = req.Description;
        sr.Location = req.Location;
        sr.Category = req.Category;
        sr.Priority = req.Priority;
        sr.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(MapToDetail(sr));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateServiceRequestStatusRequest req)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.Client).ThenInclude(c => c.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.Quotes)
            .Include(s => s.Proposal)
            .Include(s => s.WorkOrder)
            .Include(s => s.Attachments)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Service request not found.");

        if (!ServiceRequestStateMachine.CanTransition(sr.Status, req.Status))
            throw new InvalidTransitionException(sr.Status.ToString(), req.Status.ToString());

        sr.Status = req.Status;
        sr.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(MapToDetail(sr));
    }

    // ---- Vendor Invites sub-resource ----

    [HttpPost("{id:guid}/invites")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreateInvites(Guid id, [FromBody] CreateVendorInvitesRequest req)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.VendorInvites)
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new NotFoundException("Service request not found.");

        var existingVendorIds = sr.VendorInvites.Select(vi => vi.VendorId).ToHashSet();
        var created = new List<VendorInvite>();
        var skipped = new List<Guid>();

        foreach (var vendorId in req.VendorIds)
        {
            if (existingVendorIds.Contains(vendorId))
            {
                skipped.Add(vendorId);
                continue;
            }

            var vendor = await _db.Vendors.FindAsync(vendorId);
            if (vendor == null)
            {
                skipped.Add(vendorId);
                continue;
            }

            var invite = new VendorInvite
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = id,
                VendorId = vendorId,
                Status = VendorInviteStatus.Invited,
                SentAt = DateTime.UtcNow
            };

            var quote = new Quote
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = id,
                VendorId = vendorId,
                Price = 0m,
                ScopeOfWork = string.Empty,
                Status = QuoteStatus.Requested,
                PublicToken = "qt-" + Guid.NewGuid().ToString("N")
            };

            _db.VendorInvites.Add(invite);
            _db.Quotes.Add(quote);
            created.Add(invite);

            // Notify vendor
            await _notifications.CreateAsync(vendor.UserId, "VendorInvite.Received",
                $"You have been invited to quote on service request: {sr.Title}",
                $"/quotes/submit/{quote.PublicToken}");
        }

        // Auto-transition to Sourcing if status is New
        if (created.Any() && sr.Status == ServiceRequestStatus.New)
        {
            sr.Status = ServiceRequestStatus.Sourcing;
            sr.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Build response DTOs
        var createdDtos = new List<VendorInviteDto>();
        foreach (var inv in created)
        {
            var v = await _db.Vendors.Include(vn => vn.User).FirstOrDefaultAsync(vn => vn.Id == inv.VendorId);
            var q = await _db.Quotes.FirstOrDefaultAsync(qt => qt.ServiceRequestId == id && qt.VendorId == inv.VendorId);
            createdDtos.Add(new VendorInviteDto(
                inv.Id,
                inv.ServiceRequestId,
                inv.VendorId,
                inv.Status.ToString(),
                inv.SentAt,
                new VendorSummaryDto(v!.Id, v.CompanyName, v.Trades, v.Rating),
                q == null ? null : new QuoteSummaryDto(q.Id, q.Status.ToString(), q.Price == 0 ? null : q.Price, q.SubmittedAt)
            ));
        }

        return Ok(new CreateVendorInvitesResponse(createdDtos, skipped));
    }

    [HttpGet("{id:guid}/invites")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetInvites(Guid id)
    {
        var sr = await _db.ServiceRequests.FindAsync(id)
            ?? throw new NotFoundException("Service request not found.");

        var invites = await _db.VendorInvites
            .Include(vi => vi.Vendor).ThenInclude(v => v.User)
            .Where(vi => vi.ServiceRequestId == id)
            .ToListAsync();

        var dtos = new List<VendorInviteDto>();
        foreach (var inv in invites)
        {
            var q = await _db.Quotes.FirstOrDefaultAsync(qt => qt.ServiceRequestId == id && qt.VendorId == inv.VendorId);
            dtos.Add(new VendorInviteDto(
                inv.Id,
                inv.ServiceRequestId,
                inv.VendorId,
                inv.Status.ToString(),
                inv.SentAt,
                new VendorSummaryDto(inv.Vendor.Id, inv.Vendor.CompanyName, inv.Vendor.Trades, inv.Vendor.Rating),
                q == null ? null : new QuoteSummaryDto(q.Id, q.Status.ToString(), q.Price == 0 ? null : q.Price, q.SubmittedAt)
            ));
        }

        return Ok(dtos);
    }

    // ---- Helpers ----

    private static ServiceRequestSummaryDto MapToSummary(ServiceRequest sr) =>
        new(
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

    private static ServiceRequestDto MapToDetail(ServiceRequest sr) =>
        new(
            sr.Id,
            sr.Title,
            sr.Description,
            sr.Location,
            sr.Category,
            sr.Priority.ToString(),
            sr.Status.ToString(),
            sr.ClientId,
            sr.CreatedById,
            sr.CreatedAt,
            sr.UpdatedAt,
            new ClientSummaryDto(sr.Client.Id, sr.Client.CompanyName, sr.Client.Phone),
            sr.CreatedBy.Adapt<UserDto>(),
            sr.Quotes.Count,
            sr.Proposal != null,
            sr.WorkOrder != null,
            sr.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList()
        );

    private async Task<ServiceRequestDto> GetSrWithDetails(Guid id)
    {
        var sr = await _db.ServiceRequests
            .Include(s => s.Client).ThenInclude(c => c.User)
            .Include(s => s.CreatedBy)
            .Include(s => s.Quotes)
            .Include(s => s.Proposal)
            .Include(s => s.WorkOrder)
            .Include(s => s.Attachments)
            .FirstAsync(s => s.Id == id);
        return MapToDetail(sr);
    }
}
