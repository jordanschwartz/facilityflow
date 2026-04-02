namespace FacilityFlow.Core.Entities;

public class Vendor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<string> Trades { get; set; } = new();
    public List<string> ZipCodes { get; set; } = new();
    public decimal? Rating { get; set; }
    public User User { get; set; } = null!;
    public ICollection<VendorInvite> Invites { get; set; } = new List<VendorInvite>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
    public ICollection<WorkOrder> WorkOrders { get; set; } = new List<WorkOrder>();
}
