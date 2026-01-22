using System.Security.Claims;

namespace LifeHub.Services;
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        // todo: why JwtRegisteredClaimNames.Sub not working?
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id!);
    }
}