using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.ServiceRequests;
using FacilityFlow.Core.DTOs.WorkOrders;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkOrdersController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? vendorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.WorkOrders
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(wo => wo.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(wo => wo.Vendor)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<WorkOrderStatus>(status, true, out var parsedStatus))
            query = query.Where(wo => wo.Status == parsedStatus);

        if (vendorId.HasValue)
            query = query.Where(wo => wo.VendorId == vendorId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(wo => wo.ServiceRequest.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(MapToSummary).ToList();
        return Ok(new PagedResult<WorkOrderSummaryDto>(dtos, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(w => w.Vendor)
            .Include(w => w.Attachments)
            .FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new NotFoundException("Work order not found.");

        return Ok(MapToDetail(wo));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Operator,Vendor")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWorkOrderStatusRequest req)
    {
        var wo = await _db.WorkOrders
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Client)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Quotes)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.Proposal)
            .Include(w => w.ServiceRequest)
                .ThenInclude(sr => sr.WorkOrder)
            .Include(w => w.Vendor)
            .Include(w => w.Attachments)
            .FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new NotFoundException("Work order not found.");

        wo.Status = req.Status;

        if (!string.IsNullOrWhiteSpace(req.VendorNotes))
            wo.VendorNotes = req.VendorNotes;

        if (req.Status == WorkOrderStatus.Completed)
            wo.CompletedAt = DateTime.UtcNow;

        // Closing the work order completes the service request
        if (req.Status == WorkOrderStatus.Closed)
        {
            wo.ServiceRequest.Status = ServiceRequestStatus.Completed;
            wo.ServiceRequest.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(MapToDetail(wo));
    }

    private static ServiceRequestSummaryDto MapSrToSummary(Core.Entities.ServiceRequest sr) =>
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

    private static WorkOrderSummaryDto MapToSummary(Core.Entities.WorkOrder wo) =>
        new(
            wo.Id,
            wo.ServiceRequestId,
            wo.Status.ToString(),
            wo.CompletedAt,
            MapSrToSummary(wo.ServiceRequest),
            new VendorSummaryDto(wo.Vendor.Id, wo.Vendor.CompanyName, wo.Vendor.Trades, wo.Vendor.Rating)
        );

    private static WorkOrderDto MapToDetail(Core.Entities.WorkOrder wo) =>
        new(
            wo.Id,
            wo.ServiceRequestId,
            wo.ProposalId,
            wo.VendorId,
            wo.Status.ToString(),
            wo.VendorNotes,
            wo.CompletedAt,
            MapSrToSummary(wo.ServiceRequest),
            new VendorSummaryDto(wo.Vendor.Id, wo.Vendor.CompanyName, wo.Vendor.Trades, wo.Vendor.Rating),
            wo.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList()
        );
}
