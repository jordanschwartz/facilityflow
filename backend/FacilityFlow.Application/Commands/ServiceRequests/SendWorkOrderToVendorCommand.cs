using FacilityFlow.Application.DTOs.WorkOrders;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using FacilityFlow.Core.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FacilityFlow.Application.Commands.ServiceRequests;

public record SendWorkOrderToVendorCommand(Guid ServiceRequestId, Guid VendorInviteId) : IRequest<SendWorkOrderResponse>;

public class SendWorkOrderToVendorCommandHandler : IRequestHandler<SendWorkOrderToVendorCommand, SendWorkOrderResponse>
{
    private readonly IServiceRequestRepository _serviceRequests;
    private readonly IRepository<VendorInvite> _vendorInvites;
    private readonly IRepository<Quote> _quotes;
    private readonly IRepository<WorkOrderDocument> _workOrderDocuments;
    private readonly IWorkOrderPdfService _pdfService;
    private readonly IFileStorageService _fileStorage;
    private readonly INotificationService _notifications;
    private readonly IActivityLogger _activityLogger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public SendWorkOrderToVendorCommandHandler(
        IServiceRequestRepository serviceRequests,
        IRepository<VendorInvite> vendorInvites,
        IRepository<Quote> quotes,
        IRepository<WorkOrderDocument> workOrderDocuments,
        IWorkOrderPdfService pdfService,
        IFileStorageService fileStorage,
        INotificationService notifications,
        IActivityLogger activityLogger,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _serviceRequests = serviceRequests;
        _vendorInvites = vendorInvites;
        _quotes = quotes;
        _workOrderDocuments = workOrderDocuments;
        _pdfService = pdfService;
        _fileStorage = fileStorage;
        _notifications = notifications;
        _activityLogger = activityLogger;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<SendWorkOrderResponse> Handle(SendWorkOrderToVendorCommand command, CancellationToken cancellationToken)
    {
        var sr = await _serviceRequests.GetWithDetailsAsync(command.ServiceRequestId)
            ?? throw new NotFoundException("Service request not found.");

        var invite = await _vendorInvites.Query()
            .Include(vi => vi.Vendor)
            .FirstOrDefaultAsync(vi => vi.Id == command.VendorInviteId && vi.ServiceRequestId == command.ServiceRequestId, cancellationToken)
            ?? throw new NotFoundException("Vendor invite not found.");

        var allowedStatuses = new[] { VendorInviteStatus.Candidate, VendorInviteStatus.WorkOrderSent, VendorInviteStatus.Viewed };
        if (!allowedStatuses.Contains(invite.Status))
            throw new InvalidOperationException("Vendor invite must be in Candidate, WorkOrderSent, or Viewed status to send a work order.");

        // Generate PDF
        var pdfBytes = await _pdfService.GeneratePdfAsync(command.ServiceRequestId, command.VendorInviteId);

        // Save PDF to file storage
        using var stream = new MemoryStream(pdfBytes);
        var woNum = !string.IsNullOrWhiteSpace(sr.WorkOrderNumber)
            ? sr.WorkOrderNumber
            : $"WO-{sr.Id.ToString("N")[..8].ToUpper()}";
        var (pdfUrl, _) = await _fileStorage.SaveFileAsync(
            "work-orders",
            stream,
            $"{woNum}-{invite.Vendor.CompanyName.Replace(" ", "-")}.pdf",
            "application/pdf");

        // Determine version number
        var previousVersion = await _workOrderDocuments.Query()
            .Where(d => d.ServiceRequestId == command.ServiceRequestId && d.VendorInviteId == command.VendorInviteId)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync(cancellationToken);
        var version = (previousVersion?.Version ?? 0) + 1;

        // Create WorkOrderDocument record
        var doc = new WorkOrderDocument
        {
            Id = Guid.NewGuid(),
            ServiceRequestId = command.ServiceRequestId,
            VendorInviteId = command.VendorInviteId,
            Version = version,
            PdfUrl = pdfUrl,
            SentAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _workOrderDocuments.Add(doc);

        // Create Quote record for future quote submission (skip if one already exists for this vendor)
        var quoteToken = "";
        var existingQuote = await _quotes.Query()
            .FirstOrDefaultAsync(q => q.ServiceRequestId == command.ServiceRequestId && q.VendorId == invite.VendorId, cancellationToken);
        if (existingQuote != null)
        {
            quoteToken = existingQuote.PublicToken;
        }
        else
        {
            quoteToken = "qt-" + Guid.NewGuid().ToString("N");
            var quote = new Quote
            {
                Id = Guid.NewGuid(),
                ServiceRequestId = command.ServiceRequestId,
                VendorId = invite.VendorId,
                Price = 0m,
                ScopeOfWork = string.Empty,
                Status = QuoteStatus.Requested,
                PublicToken = quoteToken
            };
            _quotes.Add(quote);
        }

        // Update VendorInvite status
        invite.Status = VendorInviteStatus.WorkOrderSent;

        // Update ServiceRequest status to Sourcing if New or Qualifying
        if (sr.Status == ServiceRequestStatus.New || sr.Status == ServiceRequestStatus.Qualifying)
        {
            sr.Status = ServiceRequestStatus.Sourcing;
            sr.UpdatedAt = DateTime.UtcNow;
        }

        await _serviceRequests.SaveChangesAsync();

        // Log activity
        await _activityLogger.LogAsync(
            command.ServiceRequestId, null,
            $"Sent work order to {invite.Vendor.CompanyName}",
            ActivityLogCategory.Communication, string.Empty, null);

        // Notify vendor if they have a user account
        if (invite.Vendor.UserId.HasValue)
        {
            await _notifications.CreateAsync(invite.Vendor.UserId.Value, "WorkOrder.Sent",
                $"You have received a work order for: {sr.Title}",
                $"/work-orders/view/{invite.PublicToken}");
        }

        // Send email to vendor
        var baseUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var quoteUrl = $"{baseUrl}/quotes/submit/{quoteToken}";
        var viewUrl = $"{baseUrl}/work-orders/view/{invite.PublicToken}";
        var (emailSubject, emailHtml) = EmailTemplates.WorkOrderDispatch(
            invite.Vendor.CompanyName, woNum, sr.Title,
            sr.Location, sr.Priority.ToString(), sr.Description,
            viewUrl, quoteUrl);

        var pdfFileName = $"{woNum}-{invite.Vendor.CompanyName.Replace(" ", "-")}.pdf";
        var replyTo = Helpers.EmailAddressing.GetReplyToAddress(woNum);
        await _emailService.SendEmailAsync(invite.Vendor.Email, emailSubject, emailHtml, pdfBytes, pdfFileName, replyTo);

        return new SendWorkOrderResponse(invite.PublicToken!, pdfUrl);
    }
}
