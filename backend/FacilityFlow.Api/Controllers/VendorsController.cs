using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.DTOs.Vendors;
using FacilityFlow.Core.Entities;
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

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? trade,
        [FromQuery] string? zip,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Vendors.Include(v => v.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(trade))
            query = query.Where(v => v.Trades.Contains(trade));

        if (!string.IsNullOrWhiteSpace(zip))
            query = query.Where(v => v.ZipCodes.Contains(zip));

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.CompanyName.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(v => v.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(v => new VendorDto(
            v.Id,
            v.UserId,
            v.CompanyName,
            v.Phone,
            v.Trades,
            v.ZipCodes,
            v.Rating,
            v.User.Adapt<UserDto>()
        )).ToList();

        return Ok(new PagedResult<VendorDto>(dtos, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateVendorRequest req)
    {
        var user = await _db.Users.FindAsync(req.UserId)
            ?? throw new NotFoundException("User not found.");

        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            CompanyName = req.CompanyName,
            Phone = req.Phone,
            Trades = req.Trades,
            ZipCodes = req.ZipCodes
        };

        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync();

        var result = new VendorDto(vendor.Id, vendor.UserId, vendor.CompanyName, vendor.Phone, vendor.Trades, vendor.ZipCodes, vendor.Rating, user.Adapt<UserDto>());
        return CreatedAtAction(nameof(GetById), new { id = vendor.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var vendor = await _db.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Vendor not found.");

        var dto = new VendorDto(vendor.Id, vendor.UserId, vendor.CompanyName, vendor.Phone, vendor.Trades, vendor.ZipCodes, vendor.Rating, vendor.User.Adapt<UserDto>());
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVendorRequest req)
    {
        var vendor = await _db.Vendors.Include(v => v.User).FirstOrDefaultAsync(v => v.Id == id)
            ?? throw new NotFoundException("Vendor not found.");

        vendor.CompanyName = req.CompanyName;
        vendor.Phone = req.Phone;
        vendor.Trades = req.Trades;
        vendor.ZipCodes = req.ZipCodes;

        await _db.SaveChangesAsync();

        var dto = new VendorDto(vendor.Id, vendor.UserId, vendor.CompanyName, vendor.Phone, vendor.Trades, vendor.ZipCodes, vendor.Rating, vendor.User.Adapt<UserDto>());
        return Ok(dto);
    }
}
