namespace FacilityFlow.Core.Interfaces.Services;

public interface INotificationService
{
    Task CreateAsync(Guid userId, string type, string message, string? link = null);
}
