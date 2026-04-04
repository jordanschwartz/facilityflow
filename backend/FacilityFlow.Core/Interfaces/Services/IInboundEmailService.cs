namespace FacilityFlow.Core.Interfaces.Services;

public interface IInboundEmailService
{
    Task ProcessInboundEmailAsync(string snsMessageBody);
    Task<Guid?> ResolveServiceRequestIdAsync(string? replyToAddress, string? subject);
}
