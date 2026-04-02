using FacilityFlow.Api.Extensions;
using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/vendors")]
[Authorize]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public VendorsController(AppDbContext db) => _db = db;

    // ── Helper ────────────────────────────────────────────────────────────────

    private static VendorDto ToDto(Vendor v) => new(
        v.Id,
        v.UserId,
        v.CompanyName,
        v.PrimaryContactName,
        v.Email,
        v.Phone,
        v.PrimaryZip,
        v.ServiceRadiusMiles,
        v.Trades,
        v.ZipCodes,
        v.Rating,
        v.IsActive,
        v.IsDnu,
        v.DnuReason,
        v.User?.Adapt<UserDto>());

    // ── Vendor CRUD ───────────────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? trade,
        [FromQuery] string? zip,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isDnu,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Vendors.Include(v => v.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(trade))
            query = query.Where(v => v.Trades.Contains(trade));

        if (!string.IsNullOrWhiteSpace(zip))
            query = query.Where(v => v.ZipCodes.Contains(zip) || v.PrimaryZip == zip);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.CompanyName.ToLower().Contains(search.ToLower()));

        if (isActive.HasValue)
            query = query.Where(v => v.IsActive == isActive.Value);

        if (isDnu.HasValue)
            query = query.Where(v => v.IsDnu == isDnu.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(v => v.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(ToDto).ToList();
        return Ok(new PagedResult<VendorDto>(dtos, total, page, pageSize));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vendor = await _db.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Vendor not found.");

        return Ok(ToDto(vendor));
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest req)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            CompanyName = req.CompanyName,
            PrimaryContactName = req.PrimaryContactName,
            Email = req.Email,
            Phone = req.Phone,
            PrimaryZip = req.PrimaryZip.Trim(),
            ServiceRadiusMiles = req.ServiceRadiusMiles,
            Trades = req.Trades,
            ZipCodes = req.ZipCodes,
            IsActive = req.IsActive,
            IsDnu = req.IsDnu,
            DnuReason = req.DnuReason
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        // Reload with User nav if UserId provided
        if (vendor.UserId.HasValue)
            await _db.Entry(vendor).Reference(v => v.User).LoadAsync();

        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, ToDto(vendor));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorRequest req)
    {
        var vendor = await _db.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.CompanyName = req.CompanyName;
        vendor.PrimaryContactName = req.PrimaryContactName;
        vendor.Email = req.Email;
        vendor.Phone = req.Phone;
        vendor.PrimaryZip = req.PrimaryZip.Trim();
        vendor.ServiceRadiusMiles = req.ServiceRadiusMiles;
        vendor.Trades = req.Trades;
        vendor.ZipCodes = req.ZipCodes ?? vendor.ZipCodes;
        vendor.IsActive = req.IsActive;

        await _db.SaveChangesAsync();
        return Ok(ToDto(vendor));
    }

    [HttpPatch("{id:guid}/dnu")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> ToggleDnu(Guid id, [FromBody] ToggleDnuRequest req)
    {
        var vendor = await _db.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.IsDnu = req.IsDnu;
        vendor.DnuReason = req.IsDnu ? req.Reason : null;

        await _db.SaveChangesAsync();
        return Ok(ToDto(vendor));
    }

    // ── Nearby / Sourcing ─────────────────────────────────────────────────────

    [HttpGet("nearby")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetNearby(
        [FromQuery] string zip,
        [FromQuery] int radiusMiles = 50,
        [FromQuery] string? trade = null)
    {
        if (string.IsNullOrWhiteSpace(zip))
            return BadRequest(new { error = "zip is required." });

        var zipPrefix = zip.Length >= 3 ? zip[..3] : zip;

        var query = _db.Vendors
            .Where(v => v.IsActive && (v.PrimaryZip == zip || v.PrimaryZip.StartsWith(zipPrefix)))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(trade))
            query = query.Where(v => v.Trades.Contains(trade));

        var vendors = await query.ToListAsync();

        // Load completed job counts and last-used dates for matched vendors
        var vendorIds = vendors.Select(v => v.Id).ToList();

        var workOrderStats = await _db.WorkOrders
            .Where(wo => vendorIds.Contains(wo.VendorId))
            .GroupBy(wo => wo.VendorId)
            .Select(g => new
            {
                VendorId = g.Key,
                CompletedJobCount = g.Count(wo => wo.Status == WorkOrderStatus.Completed),
                LastUsedDate = g.Max(wo => (DateTime?)wo.ServiceRequest.CreatedAt)
            })
            .ToListAsync();

        var statsMap = workOrderStats.ToDictionary(s => s.VendorId);

        var results = vendors.Select(v =>
        {
            statsMap.TryGetValue(v.Id, out var stats);
            return new VendorSourcingResultDto(
                v.Id,
                v.CompanyName,
                v.PrimaryContactName,
                v.Email,
                v.PrimaryZip,
                v.ServiceRadiusMiles,
                v.Trades,
                v.IsDnu,
                v.DnuReason,
                stats?.CompletedJobCount ?? 0,
                stats?.LastUsedDate
            );
        }).ToList();

        return Ok(results);
    }

    // ── Notes ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/notes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetNotes(Guid id)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Id == id);
        if (!exists) throw new NotFoundException("Vendor not found.");

        var notes = await _db.VendorNotes
            .Include(n => n.CreatedBy)
            .Where(n => n.VendorId == id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var dtos = notes.Select(n => new VendorNoteDto(
            n.Id,
            n.VendorId,
            n.Text,
            n.AttachmentUrl,
            n.AttachmentFilename,
            n.CreatedBy.Name,
            n.CreatedAt
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/notes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreateNote(Guid id, [FromBody] CreateVendorNoteRequest req)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Id == id);
        if (!exists) throw new NotFoundException("Vendor not found.");

        var userId = User.GetUserId();

        var note = new VendorNote
        {
            Id = Guid.NewGuid(),
            VendorId = id,
            Text = req.Text,
            AttachmentUrl = req.AttachmentUrl,
            AttachmentFilename = req.AttachmentFilename,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        _db.VendorNotes.Add(note);
        await _db.SaveChangesAsync();

        var creator = await _db.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");

        var dto = new VendorNoteDto(
            note.Id,
            note.VendorId,
            note.Text,
            note.AttachmentUrl,
            note.AttachmentFilename,
            creator.Name,
            note.CreatedAt
        );

        return CreatedAtAction(nameof(GetNotes), new { id }, dto);
    }

    [HttpDelete("{id:guid}/notes/{noteId:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> DeleteNote(Guid id, Guid noteId)
    {
        var note = await _db.VendorNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.VendorId == id)
            ?? throw new NotFoundException("Note not found.");

        var userId = User.GetUserId();
        var userRole = User.GetRole();

        if (note.CreatedById != userId && userRole != "Operator")
            throw new ForbiddenException("You do not have permission to delete this note.");

        _db.VendorNotes.Remove(note);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ── Payments ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/payments")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetPayments(Guid id)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Id == id);
        if (!exists) throw new NotFoundException("Vendor not found.");

        var payments = await _db.VendorPayments
            .Where(p => p.VendorId == id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var dtos = payments.Select(p => new VendorPaymentDto(
            p.Id,
            p.VendorId,
            p.WorkOrderId,
            p.Amount,
            p.Status,
            p.PaidAt,
            p.Notes,
            p.CreatedAt
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost("{id:guid}/payments")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreatePayment(Guid id, [FromBody] CreateVendorPaymentRequest req)
    {
        var exists = await _db.Vendors.AnyAsync(v => v.Id == id);
        if (!exists) throw new NotFoundException("Vendor not found.");

        var payment = new VendorPayment
        {
            Id = Guid.NewGuid(),
            VendorId = id,
            WorkOrderId = req.WorkOrderId,
            Amount = req.Amount,
            Status = req.Status,
            PaidAt = req.PaidAt,
            Notes = req.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.VendorPayments.Add(payment);
        await _db.SaveChangesAsync();

        var dto = new VendorPaymentDto(
            payment.Id,
            payment.VendorId,
            payment.WorkOrderId,
            payment.Amount,
            payment.Status,
            payment.PaidAt,
            payment.Notes,
            payment.CreatedAt
        );

        return CreatedAtAction(nameof(GetPayments), new { id }, dto);
    }

    [HttpPut("{id:guid}/payments/{paymentId:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdatePayment(Guid id, Guid paymentId, [FromBody] UpdateVendorPaymentRequest req)
    {
        var payment = await _db.VendorPayments.FirstOrDefaultAsync(p => p.Id == paymentId && p.VendorId == id)
            ?? throw new NotFoundException("Payment not found.");

        payment.Status = req.Status;
        payment.PaidAt = req.PaidAt;
        payment.Notes = req.Notes;

        await _db.SaveChangesAsync();

        var dto = new VendorPaymentDto(
            payment.Id,
            payment.VendorId,
            payment.WorkOrderId,
            payment.Amount,
            payment.Status,
            payment.PaidAt,
            payment.Notes,
            payment.CreatedAt
        );

        return Ok(dto);
    }
}
