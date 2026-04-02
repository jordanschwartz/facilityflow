namespace FacilityFlow.Core.Interfaces.Services;

public interface IGeocodingService
{
    Task<(double Latitude, double Longitude)?> GeocodeZipAsync(string zip);
}
