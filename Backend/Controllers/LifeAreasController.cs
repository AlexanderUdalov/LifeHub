using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/lifeareas")]
public class LifeAreasController(ApplicationContext context) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<LifeAreaDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<LifeAreaDTO>>> GetAll()
    {
        var userId = User.GetUserId();
        var areas = await context.LifeAreas
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.Name)
            .Select(x => x.ToDTO())
            .ToListAsync();

        return Ok(areas);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LifeAreaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LifeAreaDTO>> Get(Guid id)
    {
        var userId = User.GetUserId();
        var area = await context.LifeAreas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (area is null)
            return NotFound();

        return Ok(area.ToDTO());
    }

    [HttpPost]
    [ProducesResponseType(typeof(LifeAreaDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LifeAreaDTO>> Create([FromBody] CreateLifeAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Color))
            return BadRequest();

        var userId = User.GetUserId();
        var count = await context.LifeAreas.CountAsync(x => x.UserId == userId);
        if (count >= 10)
            return BadRequest("LifeAreas limit reached (10).");

        var area = new LifeArea
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Color = request.Color.Trim()
        };

        context.LifeAreas.Add(area);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = area.Id }, area.ToDTO());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LifeAreaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LifeAreaDTO>> Update(Guid id, [FromBody] UpdateLifeAreaRequest request)
    {
        var userId = User.GetUserId();
        var area = await context.LifeAreas.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (area is null)
            return NotFound();

        if (request.Name is not null)
        {
            var trimmed = request.Name.Trim();
            if (trimmed.Length == 0)
                return BadRequest();
            area.Name = trimmed;
        }

        if (request.Color is not null)
        {
            var trimmed = request.Color.Trim();
            if (trimmed.Length == 0)
                return BadRequest();
            area.Color = trimmed;
        }

        await context.SaveChangesAsync();
        return Ok(area.ToDTO());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = User.GetUserId();
        var area = await context.LifeAreas.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (area is null)
            return NotFound();

        context.LifeAreas.Remove(area);
        await context.SaveChangesAsync();
        return NoContent();
    }
}
