using FacilityFlow.Core.Enums;

namespace FacilityFlow.Core.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string PrimaryContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PrimaryZip { get; set; } = string.Empty;
    public int ServiceRadiusMiles { get; set; } = 25;
    public List<string> Trades { get; set; } = new();
    public List<string> ZipCodes { get; set; } = new();
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDnu { get; set; } = false;
    public string? DnuReason { get; set; }
    public VendorStatus Status { get; set; } = VendorStatus.Active;
    public string? Website { get; set; }
    public int? ReviewCount { get; set; }
    public string? GoogleProfileUrl { get; set; }
    public User? User { get; set; }
    public ICollection<VendorInvite> Invites { get; set; } = new List<VendorInvite>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
    public ICollection<VendorNote> Notes { get; set; } = new List<VendorNote>();
    public ICollection<VendorPayment> Payments { get; set; } = new List<VendorPayment>();
}
