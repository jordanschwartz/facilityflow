using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Quotes;
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
public class QuotesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly NotificationService _notifications;

    public QuotesController(AppDbContext db, NotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    [HttpGet("api/service-requests/{serviceRequestId:guid}/quotes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetQuotes(Guid serviceRequestId)
    {
        var sr = await _db.ServiceRequests.FindAsync(serviceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var quotes = await _db.Quotes
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .Where(q => q.ServiceRequestId == serviceRequestId)
            .ToListAsync();

        var dtos = quotes.Select(q => new QuoteDto(
            q.Id,
            q.ServiceRequestId,
            q.VendorId,
            q.Price,
            q.ScopeOfWork,
            q.Status.ToString(),
            q.PublicToken,
            q.SubmittedAt,
            new VendorSummaryDto(q.Vendor.Id, q.Vendor.CompanyName, q.Vendor.Trades, q.Vendor.Rating),
            q.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            q.ProposedStartDate,
            q.EstimatedDurationValue,
            q.EstimatedDurationUnit,
            q.NotToExceedPrice,
            q.Assumptions,
            q.Exclusions,
            q.VendorAvailability,
            q.ValidUntil,
            q.LineItems.Select(li => new QuoteLineItemDto(li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice)).ToList()
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost("api/service-requests/{serviceRequestId:guid}/quotes")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> CreateQuote(Guid serviceRequestId, [FromBody] SubmitQuoteRequest req)
    {
        var sr = await _db.ServiceRequests.FindAsync(serviceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var quote = new Core.Entities.Quote
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = serviceRequestId,
            VendorId = Guid.Empty, // operator-created quote placeholder
            Price = req.Price,
            ScopeOfWork = req.ScopeOfWork,
            Status = QuoteStatus.Submitted,
            PublicToken = "qt-" + Guid.NewGuid().ToString("N"),
            SubmittedAt = DateTime.UtcNow
        };

        _db.Quotes.Add(quote);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetQuotes), new { serviceRequestId }, new { quote.Id });
    }

    [HttpPatch("api/quotes/{id:guid}/status")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuoteStatusRequest req)
    {
        var quote = await _db.Quotes
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new NotFoundException("Quote not found.");

        if (req.Status == QuoteStatus.Selected)
        {
            // Auto-reject all other quotes for this SR
            var otherQuotes = await _db.Quotes
                .Where(q => q.ServiceRequestId == quote.ServiceRequestId && q.Id != id && q.Status != QuoteStatus.Rejected)
                .ToListAsync();
            foreach (var other in otherQuotes)
                other.Status = QuoteStatus.Rejected;

            // Transition SR to PendingApproval
            var sr = await _db.ServiceRequests.FindAsync(quote.ServiceRequestId);
            if (sr != null && sr.Status == ServiceRequestStatus.Quoting)
            {
                sr.Status = ServiceRequestStatus.PendingApproval;
                sr.UpdatedAt = DateTime.UtcNow;
            }
        }

        quote.Status = req.Status;
        await _db.SaveChangesAsync();

        var dto = new QuoteDto(
            quote.Id,
            quote.ServiceRequestId,
            quote.VendorId,
            quote.Price,
            quote.ScopeOfWork,
            quote.Status.ToString(),
            quote.PublicToken,
            quote.SubmittedAt,
            new VendorSummaryDto(quote.Vendor.Id, quote.Vendor.CompanyName, quote.Vendor.Trades, quote.Vendor.Rating),
            quote.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            quote.ProposedStartDate,
            quote.EstimatedDurationValue,
            quote.EstimatedDurationUnit,
            quote.NotToExceedPrice,
            quote.Assumptions,
            quote.Exclusions,
            quote.VendorAvailability,
            quote.ValidUntil,
            quote.LineItems.Select(li => new QuoteLineItemDto(li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice)).ToList()
        );

        return Ok(dto);
    }

    [HttpGet("api/quotes/submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByToken(string token)
    {
        var quote = await _db.Quotes
            .Include(q => q.ServiceRequest)
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.PublicToken == token)
            ?? throw new NotFoundException("Quote not found.");

        return Ok(new
        {
            serviceRequest = new
            {
                title = quote.ServiceRequest.Title,
                location = quote.ServiceRequest.Location,
                category = quote.ServiceRequest.Category,
                description = quote.ServiceRequest.Description,
            },
            quote = new QuoteDto(
                quote.Id,
                quote.ServiceRequestId,
                quote.VendorId,
                quote.Price,
                quote.ScopeOfWork,
                quote.Status.ToString(),
                quote.PublicToken,
                quote.SubmittedAt,
                new VendorSummaryDto(quote.Vendor.Id, quote.Vendor.CompanyName, quote.Vendor.Trades, quote.Vendor.Rating),
                quote.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
                quote.ProposedStartDate,
                quote.EstimatedDurationValue,
                quote.EstimatedDurationUnit,
                quote.NotToExceedPrice,
                quote.Assumptions,
                quote.Exclusions,
                quote.VendorAvailability,
                quote.ValidUntil,
                quote.LineItems.Select(li => new QuoteLineItemDto(li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice)).ToList()
            )
        });
    }

    [HttpPost("api/quotes/submit/{token}/attachments")]
    [AllowAnonymous]
    [RequestSizeLimit(104_857_600)] // 100 MB
    public async Task<IActionResult> UploadAttachment(string token, IFormFile file, IWebHostEnvironment env)
    {
        var quote = await _db.Quotes
            .FirstOrDefaultAsync(q => q.PublicToken == token)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            return BadRequest("Quote is no longer accepting attachments.");

        var allowed = new[] { "image/jpeg", "image/png", "image/webp", "image/gif", "image/heic",
                               "video/mp4", "video/quicktime", "video/x-msvideo",
                               "application/pdf" };
        if (!allowed.Contains(file.ContentType))
            return BadRequest("File type not allowed. Accepted: images, videos, PDF.");

        var uploadsDir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", quote.Id.ToString());
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName);
        var safeFilename = Guid.NewGuid().ToString("N") + ext;
        var filePath = Path.Combine(uploadsDir, safeFilename);

        await using (var stream = System.IO.File.Create(filePath))
            await file.CopyToAsync(stream);

        var attachment = new Core.Entities.Attachment
        {
            Id = Guid.NewGuid(),
            QuoteId = quote.Id,
            Filename = file.FileName,
            MimeType = file.ContentType,
            Url = $"/uploads/{quote.Id}/{safeFilename}",
        };

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();

        return Ok(new AttachmentDto(attachment.Id, attachment.Url, attachment.Filename, attachment.MimeType));
    }

    [HttpDelete("api/quotes/submit/{token}/attachments/{attachmentId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteAttachment(string token, Guid attachmentId, IWebHostEnvironment env)
    {
        var quote = await _db.Quotes
            .FirstOrDefaultAsync(q => q.PublicToken == token)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            return BadRequest("Quote is no longer accepting changes.");

        var attachment = await _db.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.QuoteId == quote.Id)
            ?? throw new NotFoundException("Attachment not found.");

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        var filePath = Path.Combine(webRoot, attachment.Url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("api/quotes/submit/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitByToken(string token, [FromBody] SubmitQuoteRequest req)
    {
        var quote = await _db.Quotes
            .Include(q => q.Vendor).ThenInclude(v => v.User)
            .Include(q => q.ServiceRequest)
            .Include(q => q.Attachments)
            .Include(q => q.LineItems)
            .FirstOrDefaultAsync(q => q.PublicToken == token)
            ?? throw new NotFoundException("Quote not found.");

        if (quote.Status != QuoteStatus.Requested)
            throw new InvalidOperationException("Quote has already been submitted or is no longer accepting responses.");

        quote.Price = req.Price;
        quote.ScopeOfWork = req.ScopeOfWork;
        quote.ProposedStartDate = req.ProposedStartDate;
        quote.EstimatedDurationValue = req.EstimatedDurationValue;
        quote.EstimatedDurationUnit = req.EstimatedDurationUnit;
        quote.NotToExceedPrice = req.NotToExceedPrice;
        quote.Assumptions = req.Assumptions;
        quote.Exclusions = req.Exclusions;
        quote.VendorAvailability = req.VendorAvailability;
        quote.ValidUntil = req.ValidUntil;
        quote.Status = QuoteStatus.Submitted;
        quote.SubmittedAt = DateTime.UtcNow;

        // Replace line items
        quote.LineItems.Clear();
        if (req.LineItems != null)
        {
            foreach (var li in req.LineItems)
            {
                quote.LineItems.Add(new Core.Entities.QuoteLineItem
                {
                    Id = Guid.NewGuid(),
                    QuoteId = quote.Id,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice
                });
            }
        }

        // Update invite status
        var invite = await _db.VendorInvites
            .FirstOrDefaultAsync(vi => vi.ServiceRequestId == quote.ServiceRequestId && vi.VendorId == quote.VendorId);
        if (invite != null)
            invite.Status = VendorInviteStatus.Quoted;

        // Transition SR to Quoting if in Sourcing
        if (quote.ServiceRequest.Status == ServiceRequestStatus.Sourcing)
        {
            quote.ServiceRequest.Status = ServiceRequestStatus.Quoting;
            quote.ServiceRequest.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        // Notify operators about the new quote submission
        var operators = await _db.Users.Where(u => u.Role == UserRole.Operator).ToListAsync();
        foreach (var op in operators)
        {
            await _notifications.CreateAsync(op.Id, "Quote.Submitted",
                $"A new quote has been submitted for service request: {quote.ServiceRequest.Title}",
                $"/service-requests/{quote.ServiceRequestId}");
        }

        var dto = new QuoteDto(
            quote.Id,
            quote.ServiceRequestId,
            quote.VendorId,
            quote.Price,
            quote.ScopeOfWork,
            quote.Status.ToString(),
            quote.PublicToken,
            quote.SubmittedAt,
            new VendorSummaryDto(quote.Vendor.Id, quote.Vendor.CompanyName, quote.Vendor.Trades, quote.Vendor.Rating),
            quote.Attachments.Select(a => new AttachmentDto(a.Id, a.Url, a.Filename, a.MimeType)).ToList(),
            quote.ProposedStartDate,
            quote.EstimatedDurationValue,
            quote.EstimatedDurationUnit,
            quote.NotToExceedPrice,
            quote.Assumptions,
            quote.Exclusions,
            quote.VendorAvailability,
            quote.ValidUntil,
            quote.LineItems.Select(li => new QuoteLineItemDto(li.Id, li.Description, li.Quantity, li.UnitPrice, li.Quantity * li.UnitPrice)).ToList()
        );

        return Ok(dto);
    }
}
