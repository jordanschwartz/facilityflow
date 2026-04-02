using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;

namespace FacilityFlow.Application.Commands.Vendors;

public record CreateVendorPaymentCommand(Guid VendorId, CreateVendorPaymentRequest Request) : IRequest<VendorPaymentDto>;

public class CreateVendorPaymentCommandHandler : IRequestHandler<CreateVendorPaymentCommand, VendorPaymentDto>
{
    private readonly IRepository<Vendor> _vendorRepo;
    private readonly IRepository<VendorPayment> _paymentRepo;

    public CreateVendorPaymentCommandHandler(IRepository<Vendor> vendorRepo, IRepository<VendorPayment> paymentRepo)
    {
        _vendorRepo = vendorRepo;
        _paymentRepo = paymentRepo;
    }

    public async Task<VendorPaymentDto> Handle(CreateVendorPaymentCommand request, CancellationToken cancellationToken)
    {
        if (!await _vendorRepo.ExistsAsync(request.VendorId))
            throw new NotFoundException("Vendor not found.");

        var req = request.Request;
        var payment = new VendorPayment
        {
            Id = Guid.NewGuid(),
            VendorId = request.VendorId,
            WorkOrderId = req.WorkOrderId,
            Amount = req.Amount,
            Status = req.Status,
            PaidAt = req.PaidAt,
            Notes = req.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _paymentRepo.Add(payment);
        await _paymentRepo.SaveChangesAsync();

        return new VendorPaymentDto(
            payment.Id,
            payment.VendorId,
            payment.WorkOrderId,
            payment.Amount,
            payment.Status,
            payment.PaidAt,
            payment.Notes,
            payment.CreatedAt
        );
    }
}
