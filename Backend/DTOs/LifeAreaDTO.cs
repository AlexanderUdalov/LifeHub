using LifeHub.Models;

namespace LifeHub.DTOs;

public record LifeAreaDTO(
    Guid Id,
    string Name,
    string Color
);

public record CreateLifeAreaRequest(
    string Name,
    string Color
);

public record UpdateLifeAreaRequest(
    string? Name,
    string? Color
);

public static class LifeAreaMapping
{
    public static LifeAreaDTO ToDTO(this LifeArea area) =>
        new(
            area.Id,
            area.Name,
            area.Color
        );
}
