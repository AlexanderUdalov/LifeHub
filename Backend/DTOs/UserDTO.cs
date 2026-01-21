using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace LifeHub.DTOs;

public record RegisterRequest
(
    string Name,
    string Email,
    string Password
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(string Token);