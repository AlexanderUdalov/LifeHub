using LifeHub.Models;

namespace LifeHub.DTOs;

public record RegisterRequest
(
    string Name,
    string Email,
    string Password
);

public record LoginRequest
(
    string Email,
    string Password
);

public record UpdateRequest
(
    string? Name,
    string? Email,
    string? CurrentPassword,
    string? NewPassword
);

public record UserDTO
(
    string Name,
    string Email,
    string Token
);

public record AuthResponse(string Token);