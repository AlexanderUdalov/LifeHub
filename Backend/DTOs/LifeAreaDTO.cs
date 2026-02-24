using LifeHub.Models;

namespace LifeHub.DTOs;

public record LifeAreaDTO(
    Guid Id,
    string Name,
    string Color,
    string? Emoji
);

public record CreateLifeAreaRequest(
    string Name,
    string Color,
    string? Emoji
);

public record UpdateLifeAreaRequest(
    string? Name,
    string? Color,
    string? Emoji
);

public static class LifeAreaMapping
{
    public static LifeAreaDTO ToDTO(this LifeArea area) =>
        new(
            area.Id,
            area.Name,
            area.Color,
            area.Emoji
        );
}
