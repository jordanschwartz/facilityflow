using FacilityFlow.Application.DTOs.Vendors;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Core.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Application.Queries.Vendors;

public record GetVendorPaymentsQuery(Guid VendorId) : IRequest<List<VendorPaymentDto>>;

public class GetVendorPaymentsQueryHandler : IRequestHandler<GetVendorPaymentsQuery, List<VendorPaymentDto>>
{
    private readonly IRepository<Vendor> _vendorRepo;
    private readonly IRepository<VendorPayment> _paymentRepo;

    public GetVendorPaymentsQueryHandler(IRepository<Vendor> vendorRepo, IRepository<VendorPayment> paymentRepo)
    {
        _vendorRepo = vendorRepo;
        _paymentRepo = paymentRepo;
    }

    public async Task<List<VendorPaymentDto>> Handle(GetVendorPaymentsQuery request, CancellationToken cancellationToken)
    {
        if (!await _vendorRepo.ExistsAsync(request.VendorId))
            throw new NotFoundException("Vendor not found.");

        var payments = await _paymentRepo.Query()
            .Where(p => p.VendorId == request.VendorId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(p => new VendorPaymentDto(
            p.Id,
            p.VendorId,
            p.WorkOrderId,
            p.Amount,
            p.Status,
            p.PaidAt,
            p.Notes,
            p.CreatedAt
        )).ToList();
    }
}
