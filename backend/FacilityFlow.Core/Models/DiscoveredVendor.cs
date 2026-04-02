namespace FacilityFlow.Core.Models;

public class DiscoveredVendor
{
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public decimal? Rating { get; set; }
    public int? ReviewCount { get; set; }
    public string? GoogleProfileUrl { get; set; }
}
