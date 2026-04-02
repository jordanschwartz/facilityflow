using FacilityFlow.Core.DTOs.Auth;
using FacilityFlow.Core.DTOs.Clients;
using FacilityFlow.Core.DTOs.Common;
using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Exceptions;
using FacilityFlow.Infrastructure.Persistence;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _db.Clients.Include(c => c.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.CompanyName.ToLower().Contains(search.ToLower())
                                  || c.User.Name.ToLower().Contains(search.ToLower())
                                  || c.User.Email.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = items.Select(c => new ClientDto(
            c.Id,
            c.UserId,
            c.CompanyName,
            c.Phone,
            c.Address,
            c.User.Adapt<UserDto>()
        )).ToList();

        return Ok(new PagedResult<ClientDto>(dtos, total, page, pageSize));
    }

    [HttpPost]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest req)
    {
        var user = await _db.Users.FindAsync(req.UserId)
            ?? throw new NotFoundException("User not found.");

        var client = new Client
        {
            Id = Guid.NewGuid(),
            UserId = req.UserId,
            CompanyName = req.CompanyName,
            Phone = req.Phone,
            Address = req.Address
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var result = new ClientDto(client.Id, client.UserId, client.CompanyName, client.Phone, client.Address, user.Adapt<UserDto>());
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _db.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Client not found.");

        var dto = new ClientDto(
            client.Id,
            client.UserId,
            client.CompanyName,
            client.Phone,
            client.Address,
            client.User.Adapt<UserDto>()
        );
        return Ok(dto);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Operator")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientRequest req)
    {
        var client = await _db.Clients.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new NotFoundException("Client not found.");

        client.CompanyName = req.CompanyName;
        client.Phone = req.Phone;
        client.Address = req.Address;

        await _db.SaveChangesAsync();

        var dto = new ClientDto(
            client.Id,
            client.UserId,
            client.CompanyName,
            client.Phone,
            client.Address,
            client.User.Adapt<UserDto>()
        );
        return Ok(dto);
    }
}
