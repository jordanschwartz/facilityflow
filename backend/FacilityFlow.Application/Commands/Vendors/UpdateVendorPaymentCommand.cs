using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Commands.Vendors;

public record UpdateVendorPaymentCommand(Guid VendorId, Guid PaymentId, UpdateVendorPaymentRequest Request) : IRequest<VendorPaymentDto>;

public class UpdateVendorPaymentCommandHandler : IRequestHandler<UpdateVendorPaymentCommand, VendorPaymentDto>
{
    private readonly IRepository<VendorPayment> _paymentRepo;

    public UpdateVendorPaymentCommandHandler(IRepository<VendorPayment> paymentRepo) => _paymentRepo = paymentRepo;

    public async Task<VendorPaymentDto> Handle(UpdateVendorPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepo.Query()
            .FirstOrDefaultAsync(p => p.Id == request.PaymentId && p.VendorId == request.VendorId, cancellationToken)
            ?? throw new NotFoundException("Payment not found.");

        payment.Status = request.Request.Status;
        payment.PaidAt = request.Request.PaidAt;
        payment.Notes = request.Request.Notes;

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
