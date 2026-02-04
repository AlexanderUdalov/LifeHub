
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
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

        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(CreateJwtToken(user))));
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

        return Ok(new AuthResponse(new JwtSecurityTokenHandler().WriteToken(CreateJwtToken(user))));
    }


    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> Get()
    {
        var userId = User.GetUserId();
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        return new UserDTO(user.Name, user.Email, new JwtSecurityTokenHandler().WriteToken(CreateJwtToken(user)));
    }

    [Authorize]
    [HttpPatch]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> Update(UpdateRequest request)
    {
        var userId = User.GetUserId();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        if (request.NewPassword is not null)
        {
            if (request.CurrentPassword is null)
                return BadRequest();

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
            if (result == PasswordVerificationResult.Failed)
                return BadRequest();

            user.PasswordHash = _passwordHasher.HashPassword(null!, request.NewPassword);
        }

        user.Name = request.Name ?? user.Name;
        user.Email = request.Email ?? user.Email;

        await context.SaveChangesAsync();

        return new UserDTO(user.Name, user.Email, new JwtSecurityTokenHandler().WriteToken(CreateJwtToken(user)));
    }

    private JwtSecurityToken CreateJwtToken(User user)
    {
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
        return token;
    }

    [Authorize]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete()
    {
        var userId = User.GetUserId();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);

        if (user is null)
            return NotFound();

        context.Users.Remove(user);
        // todo: remove tasks
        await context.SaveChangesAsync();

        return NoContent();
    }
}
