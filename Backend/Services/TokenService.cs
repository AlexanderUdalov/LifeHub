
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LifeHub.Services;
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.Parse(id!);
    }
}