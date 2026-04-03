using FacilityFlow.Application.Commands.ServiceRequests;
using FacilityFlow.Application.Commands.WorkOrders;
using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Application.Queries.WorkOrders;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using FacilityFlow.Core.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/work-orders")]
[Authorize]
public class WorkOrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWorkOrderPdfService _pdfService;
    private readonly IRepository<VendorInvite> _vendorInvites;

    public WorkOrdersController(IMediator mediator, IWorkOrderPdfService pdfService, IRepository<VendorInvite> vendorInvites)
    {
        _mediator = mediator;
        _pdfService = pdfService;
        _vendorInvites = vendorInvites;
    }

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? vendorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetWorkOrdersQuery(status, vendorId, page, pageSize));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetWorkOrderByIdQuery(id));
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Operator,Vendor")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateWorkOrderStatusRequest req)
    {
        var result = await _mediator.Send(new UpdateWorkOrderStatusCommand(id, req));
        return Ok(result);
    }

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "Operator,Vendor")]
    [RequestSizeLimit(104_857_600)]
    public async Task<IActionResult> UploadAttachment(Guid id, IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var result = await _mediator.Send(
            new UploadWorkOrderAttachmentCommand(id, stream, file.FileName, file.ContentType));
        return Ok(result);
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Roles = "Operator,Vendor")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId)
    {
        await _mediator.Send(new DeleteWorkOrderAttachmentCommand(id, attachmentId));
        return NoContent();
    }

    [HttpPost("{serviceRequestId:guid}/send")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> SendWorkOrder(Guid serviceRequestId, [FromBody] SendWorkOrderRequest req)
    {
        var result = await _mediator.Send(new SendWorkOrderToVendorCommand(serviceRequestId, req.VendorInviteId));
        return Ok(result);
    }

    [HttpGet("{serviceRequestId:guid}/preview-pdf")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> PreviewPdf(Guid serviceRequestId, [FromQuery] Guid vendorInviteId)
    {
        var pdf = await _pdfService.GeneratePdfAsync(serviceRequestId, vendorInviteId);
        return File(pdf, "application/pdf", $"WorkOrder-Preview.pdf");
    }

    [HttpGet("view/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewByToken(string token)
    {
        var result = await _mediator.Send(new GetWorkOrderByTokenQuery(token));
        return Ok(result);
    }

    [HttpGet("view/{token}/pdf")]
    [AllowAnonymous]
    public async Task<IActionResult> ViewPdfByToken(string token)
    {
        var invite = await _vendorInvites.Query()
            .FirstOrDefaultAsync(vi => vi.PublicToken == token);
        if (invite == null) return NotFound();
        var pdf = await _pdfService.GeneratePdfAsync(invite.ServiceRequestId, invite.Id);
        return File(pdf, "application/pdf", "WorkOrder.pdf");
    }
}
