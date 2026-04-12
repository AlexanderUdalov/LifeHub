using LifeHub.DTOs;
using LifeHub.Models;
using LifeHub.Services;
using Xunit;

namespace Backend.Tests;

public class AiChatServicePromptTests
{
    [Fact]
    public void TriggerGuidanceFallback_Russian_UsesDescriptionInSubtitle()
    {
        var method = typeof(AddictionTriggerGuidanceService).GetMethod(
            "BuildFallback",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        var addiction = new Addiction
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Smoking",
            Description = "Хочу дышать свободно и быть энергичным",
            Color = "#ef4444",
            CreatedAt = DateTime.UtcNow
        };

        var result = method!.Invoke(null, [addiction, 12, 2, 5, true]) as GenerateTriggerGuidanceResponse;
        Assert.NotNull(result);
        Assert.Contains("Хочу дышать свободно", result!.Subtitle);
        Assert.Equal(5, result.Tips.Count);
    }

    [Fact]
    public void BuildSystemPrompt_UsesPreferredLanguageForFirstMessage()
    {
        var prompt = InvokeBuildSystemPrompt("ctx", 7, "ru");

        Assert.Contains("The first assistant message MUST be in Russian", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_DefaultsToRussianForUnknownLanguage()
    {
        var prompt = InvokeBuildSystemPrompt("ctx", 7, "de");

        Assert.Contains("The first assistant message MUST be in Russian", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_IncludesSoftFeedbackGuidance()
    {
        var prompt = InvokeBuildSystemPrompt("ctx", 14, "en");

        Assert.Contains("Keep feedback collection gentle: invite sharing instead of pressuring for analysis.", prompt);
        Assert.Contains("A soft, optional", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_IncludesPreviousReflectionContinuityGuidance()
    {
        var prompt = InvokeBuildSystemPrompt("ctx", 30, "en");

        Assert.Contains("Use continuity: if \"PREVIOUS REFLECTION SUMMARIES\" are present in the context", prompt);
    }

    private static string InvokeBuildSystemPrompt(string contextText, int periodDays, string preferredLanguage)
    {
        var method = typeof(AiChatService).GetMethod(
            "BuildSystemPrompt",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(method);

        var result = method!.Invoke(null, [contextText, periodDays, preferredLanguage]) as string;
        Assert.NotNull(result);
        return result!;
    }
}
