using FacilityFlow.Core.Entities;
using FacilityFlow.Core.Enums;
using FacilityFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FacilityFlow.Infrastructure.SeedData;

public static class DbSeeder
{
    // Stable GUIDs for idempotent seeding
    private static readonly Guid OperatorUserId    = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Client1UserId     = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Client2UserId     = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private static readonly Guid Vendor1UserId     = Guid.Parse("00000000-0000-0000-0000-000000000004");
    private static readonly Guid Vendor2UserId     = Guid.Parse("00000000-0000-0000-0000-000000000005");
    private static readonly Guid Vendor3UserId     = Guid.Parse("00000000-0000-0000-0000-000000000006");
    private static readonly Guid Vendor4UserId     = Guid.Parse("00000000-0000-0000-0000-000000000007");
    private static readonly Guid Vendor5UserId     = Guid.Parse("00000000-0000-0000-0000-000000000008");

    private static readonly Guid Client1Id         = Guid.Parse("00000000-0000-0000-0001-000000000001");
    private static readonly Guid Client2Id         = Guid.Parse("00000000-0000-0000-0001-000000000002");
    private static readonly Guid Vendor1Id         = Guid.Parse("00000000-0000-0000-0002-000000000001");
    private static readonly Guid Vendor2Id         = Guid.Parse("00000000-0000-0000-0002-000000000002");
    private static readonly Guid Vendor3Id         = Guid.Parse("00000000-0000-0000-0002-000000000003");
    private static readonly Guid Vendor4Id         = Guid.Parse("00000000-0000-0000-0002-000000000004");
    private static readonly Guid Vendor5Id         = Guid.Parse("00000000-0000-0000-0002-000000000005");

    private static readonly Guid SR1Id             = Guid.Parse("00000000-0000-0000-0003-000000000001");
    private static readonly Guid SR2Id             = Guid.Parse("00000000-0000-0000-0003-000000000002");
    private static readonly Guid SR3Id             = Guid.Parse("00000000-0000-0000-0003-000000000003");

    private static readonly Guid Invite1Id         = Guid.Parse("00000000-0000-0000-0004-000000000001");
    private static readonly Guid Invite2Id         = Guid.Parse("00000000-0000-0000-0004-000000000002");
    private static readonly Guid Invite3Id         = Guid.Parse("00000000-0000-0000-0004-000000000003");
    private static readonly Guid Invite4Id         = Guid.Parse("00000000-0000-0000-0004-000000000004");

    private static readonly Guid Quote1Id          = Guid.Parse("00000000-0000-0000-0005-000000000001");
    private static readonly Guid Quote2Id          = Guid.Parse("00000000-0000-0000-0005-000000000002");
    private static readonly Guid Quote3Id          = Guid.Parse("00000000-0000-0000-0005-000000000003");
    private static readonly Guid Quote4Id          = Guid.Parse("00000000-0000-0000-0005-000000000004");

    private static readonly Guid Proposal1Id       = Guid.Parse("00000000-0000-0000-0006-000000000001");
    private static readonly Guid WorkOrder1Id      = Guid.Parse("00000000-0000-0000-0007-000000000001");

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync()) return;

        // ---- Users ----
        var users = new List<User>
        {
            new()
            {
                Id = OperatorUserId,
                Email = "admin@facilityflow.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                FirstName = "Admin",
                LastName = "Operator",
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Client1UserId,
                Email = "client1@acme.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client123!"),
                FirstName = "Alice",
                LastName = "Acme",
                Role = UserRole.Client,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Client2UserId,
                Email = "client2@buildright.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Client123!"),
                FirstName = "Bob",
                LastName = "BuildRight",
                Role = UserRole.Client,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Vendor1UserId,
                Email = "vendor.hvac@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor123!"),
                FirstName = "Victor",
                LastName = "HVAC",
                Role = UserRole.Vendor,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Vendor2UserId,
                Email = "vendor.electrical@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor123!"),
                FirstName = "Ellie",
                LastName = "Electric",
                Role = UserRole.Vendor,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Vendor3UserId,
                Email = "vendor.plumbing@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor123!"),
                FirstName = "Pete",
                LastName = "Plumbing",
                Role = UserRole.Vendor,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Vendor4UserId,
                Email = "vendor.roofing@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor123!"),
                FirstName = "Rachel",
                LastName = "Roofing",
                Role = UserRole.Vendor,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Vendor5UserId,
                Email = "vendor.general@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Vendor123!"),
                FirstName = "Gary",
                LastName = "General",
                Role = UserRole.Vendor,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
        };
        db.Users.AddRange(users);
        await db.SaveChangesAsync();

        // ---- Clients ----
        var clients = new List<Client>
        {
            new()
            {
                Id = Client1Id,
                UserId = Client1UserId,
                CompanyName = "Acme Corporation",
                Phone = "555-0101",
                Address = "100 Acme Way, Springfield, IL 62701"
            },
            new()
            {
                Id = Client2Id,
                UserId = Client2UserId,
                CompanyName = "BuildRight LLC",
                Phone = "555-0202",
                Address = "200 Build Ave, Chicago, IL 60601"
            },
        };
        db.Clients.AddRange(clients);
        await db.SaveChangesAsync();

        // ---- Vendors ----
        var vendors = new List<Vendor>
        {
            new()
            {
                Id = Vendor1Id,
                UserId = Vendor1UserId,
                CompanyName = "CoolAir HVAC Services",
                Phone = "555-1001",
                Trades = new List<string> { "HVAC", "Refrigeration" },
                ZipCodes = new List<string> { "62701", "62702", "62703" },
                Rating = 4.8m
            },
            new()
            {
                Id = Vendor2Id,
                UserId = Vendor2UserId,
                CompanyName = "BrightSpark Electrical",
                Phone = "555-1002",
                Trades = new List<string> { "Electrical", "Lighting" },
                ZipCodes = new List<string> { "60601", "60602", "60610" },
                Rating = 4.5m
            },
            new()
            {
                Id = Vendor3Id,
                UserId = Vendor3UserId,
                CompanyName = "FlowPro Plumbing",
                Phone = "555-1003",
                Trades = new List<string> { "Plumbing", "Drain Cleaning" },
                ZipCodes = new List<string> { "62701", "60601", "60602" },
                Rating = 4.2m
            },
            new()
            {
                Id = Vendor4Id,
                UserId = Vendor4UserId,
                CompanyName = "TopSeal Roofing",
                Phone = "555-1004",
                Trades = new List<string> { "Roofing", "Waterproofing" },
                ZipCodes = new List<string> { "62701", "62702" },
                Rating = 4.7m
            },
            new()
            {
                Id = Vendor5Id,
                UserId = Vendor5UserId,
                CompanyName = "AllPro General Contractors",
                Phone = "555-1005",
                Trades = new List<string> { "General Contractor", "Carpentry", "Painting" },
                ZipCodes = new List<string> { "62701", "60601", "60602", "60610" },
                Rating = 4.0m
            },
        };
        db.Vendors.AddRange(vendors);
        await db.SaveChangesAsync();

        // ---- Service Requests ----
        var now = DateTime.UtcNow;
        var serviceRequests = new List<ServiceRequest>
        {
            new()
            {
                Id = SR1Id,
                Title = "HVAC System Inspection",
                Description = "Annual inspection of rooftop HVAC units at main facility. Units appear to be underperforming.",
                Location = "100 Acme Way, Building A, Roof",
                Category = "HVAC",
                Priority = Priority.Medium,
                Status = ServiceRequestStatus.New,
                ClientId = Client1Id,
                CreatedById = Client1UserId,
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now.AddDays(-10)
            },
            new()
            {
                Id = SR2Id,
                Title = "Electrical Panel Upgrade",
                Description = "Main electrical panel needs upgrade from 200A to 400A service. Outdated breakers need replacement.",
                Location = "200 Build Ave, Utility Room B1",
                Category = "Electrical",
                Priority = Priority.High,
                Status = ServiceRequestStatus.PendingQuotes,
                ClientId = Client2Id,
                CreatedById = Client2UserId,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-5)
            },
            new()
            {
                Id = SR3Id,
                Title = "Flat Roof Repair - Water Intrusion",
                Description = "Water intrusion detected in northeast corner of roof. Membrane is damaged and requires immediate repair.",
                Location = "100 Acme Way, Building C, Roof",
                Category = "Roofing",
                Priority = Priority.Urgent,
                Status = ServiceRequestStatus.POReceived,
                ClientId = Client1Id,
                CreatedById = Client1UserId,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-2)
            },
        };
        db.ServiceRequests.AddRange(serviceRequests);
        await db.SaveChangesAsync();

        // ---- Vendor Invites ----
        var invites = new List<VendorInvite>
        {
            new()
            {
                Id = Invite1Id,
                ServiceRequestId = SR2Id,
                VendorId = Vendor2Id,
                Status = VendorInviteStatus.QuoteSubmitted,
                SentAt = now.AddDays(-18)
            },
            new()
            {
                Id = Invite2Id,
                ServiceRequestId = SR2Id,
                VendorId = Vendor5Id,
                Status = VendorInviteStatus.Candidate,
                SentAt = now.AddDays(-18)
            },
            new()
            {
                Id = Invite3Id,
                ServiceRequestId = SR3Id,
                VendorId = Vendor4Id,
                Status = VendorInviteStatus.QuoteSubmitted,
                SentAt = now.AddDays(-28)
            },
        };
        db.VendorInvites.AddRange(invites);
        await db.SaveChangesAsync();

        // ---- Quotes ----
        var quotes = new List<Quote>
        {
            new()
            {
                Id = Quote1Id,
                ServiceRequestId = SR2Id,
                VendorId = Vendor2Id,
                Price = 8500.00m,
                ScopeOfWork = "Complete panel upgrade from 200A to 400A. Replace all breakers, upgrade service entrance, ensure code compliance. Estimated 2 days labor.",
                Status = QuoteStatus.Submitted,
                PublicToken = "qt-" + Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa").ToString("N"),
                SubmittedAt = now.AddDays(-15)
            },
            new()
            {
                Id = Quote2Id,
                ServiceRequestId = SR2Id,
                VendorId = Vendor5Id,
                Price = 0m,
                ScopeOfWork = string.Empty,
                Status = QuoteStatus.Requested,
                PublicToken = "qt-" + Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb").ToString("N"),
                SubmittedAt = null
            },
            new()
            {
                Id = Quote3Id,
                ServiceRequestId = SR3Id,
                VendorId = Vendor4Id,
                Price = 12750.00m,
                ScopeOfWork = "Full membrane replacement on northeast section (approx 2000 sq ft). Remove damaged material, install new TPO membrane, seal all penetrations. 3-year workmanship warranty.",
                Status = QuoteStatus.Selected,
                PublicToken = "qt-" + Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc").ToString("N"),
                SubmittedAt = now.AddDays(-25)
            },
        };
        db.Quotes.AddRange(quotes);
        await db.SaveChangesAsync();

        // ---- Proposal ----
        var proposals = new List<Proposal>
        {
            new()
            {
                Id = Proposal1Id,
                ServiceRequestId = SR3Id,
                QuoteId = Quote3Id,
                Price = 12750.00m,
                ScopeOfWork = "Full membrane replacement on northeast section (approx 2000 sq ft). Remove damaged material, install new TPO membrane, seal all penetrations. 3-year workmanship warranty.",
                Status = ProposalStatus.Approved,
                PublicToken = "pr-" + Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd").ToString("N"),
                SentAt = now.AddDays(-10),
                ClientResponse = "Approved. Please proceed as soon as possible.",
                ClientRespondedAt = now.AddDays(-8)
            },
        };
        db.Proposals.AddRange(proposals);
        await db.SaveChangesAsync();

        // ---- Work Order ----
        var workOrders = new List<WorkOrder>
        {
            new()
            {
                Id = WorkOrder1Id,
                ServiceRequestId = SR3Id,
                ProposalId = Proposal1Id,
                VendorId = Vendor4Id,
                Status = WorkOrderStatus.InProgress,
                VendorNotes = "Materials ordered. Work scheduled to begin Monday.",
                CompletedAt = null
            },
        };
        db.WorkOrders.AddRange(workOrders);
        await db.SaveChangesAsync();
    }
}
