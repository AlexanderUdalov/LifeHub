
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LifeHub.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(ApplicationContext context, IConfiguration config) : ControllerBase
{
    private readonly PasswordHasher<User> _passwordHasher = new();


    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
            return BadRequest("User already exists");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = _passwordHasher.HashPassword(null!, request.Password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Unauthorized();

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized();

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(config["Jwt:ExpiresMinutes"]!)),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256
            )
        );

        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }
}
