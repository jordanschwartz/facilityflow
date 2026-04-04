using System.Security.Claims;

namespace FacilityFlow.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string GetRole(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role)!;

    public static List<string> GetPermissions(this ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue("permissions");
        if (string.IsNullOrEmpty(claim)) return new List<string>();
        return claim.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
    }
}
