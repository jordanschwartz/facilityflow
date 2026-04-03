namespace FacilityFlow.Core.Entities;

public class Client
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? WorkOrderPrefix { get; set; }
    public User? User { get; set; }
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
