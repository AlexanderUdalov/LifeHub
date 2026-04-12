using System.Text.Json;
using LifeHub.DTOs;
using LifeHub.Models;
using Microsoft.Extensions.AI;

namespace LifeHub.Services;

public class AddictionTriggerGuidanceService(IChatClient? chatClient = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<GenerateTriggerGuidanceResponse> GenerateAsync(
        Addiction addiction,
        int currentStreakDays,
        IReadOnlyList<AddictionReset> recentResets,
        IReadOnlyList<AddictionTriggerEvent> recentTriggerEvents,
        string languageCode)
    {
        var isRussian = languageCode.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
        var fallback = BuildFallback(addiction, currentStreakDays, recentResets.Count, recentTriggerEvents.Count, isRussian);

        if (chatClient is null)
            return fallback;

        try
        {
            var prompt = BuildPrompt(addiction, currentStreakDays, recentResets, recentTriggerEvents, isRussian);
            var response = await chatClient.GetResponseAsync([new ChatMessage(ChatRole.User, prompt)]);
            var parsed = TryParseResponse(response.Text);
            return parsed ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static GenerateTriggerGuidanceResponse? TryParseResponse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var payload = DeserializePayload(text) ?? TryDeserializeEmbeddedJson(text);
        if (payload is null)
            return null;

        var title = payload.Title?.Trim();
        var subtitle = payload.Subtitle?.Trim();
        var tips = payload.Tips?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct()
            .Take(5)
            .ToList();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(subtitle) || tips is null || tips.Count == 0)
            return null;

        return new GenerateTriggerGuidanceResponse(title, subtitle, tips);
    }

    private static TriggerGuidancePayload? DeserializePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<TriggerGuidancePayload>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    private static TriggerGuidancePayload? TryDeserializeEmbeddedJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
            return null;

        var json = text[start..(end + 1)];
        return DeserializePayload(json);
    }

    private static string BuildPrompt(
        Addiction addiction,
        int currentStreakDays,
        IReadOnlyList<AddictionReset> recentResets,
        IReadOnlyList<AddictionTriggerEvent> recentTriggerEvents,
        bool isRussian)
    {
        var tipsLanguage = isRussian ? "Russian" : "English";
        var triggerHistory = recentTriggerEvents.Count == 0
            ? "No trigger events logged recently."
            : string.Join('\n', recentTriggerEvents
                .OrderByDescending(x => x.EventAt)
                .Take(8)
                .Select(x =>
                    $"- {x.EventAt:u}; outcome: {x.Outcome}; note: {(string.IsNullOrWhiteSpace(x.Note) ? "(no note)" : x.Note)}"));

        var resetHistory = recentResets.Count == 0
            ? "No relapse resets in the last 30 days."
            : string.Join('\n', recentResets
                .OrderByDescending(x => x.ResetAt)
                .Take(8)
                .Select(x => $"- {x.ResetAt:u}; journal-linked: {x.JournalEntryId.HasValue}"));

        return $$"""
            You are helping a user during an active addiction trigger moment.
            Return STRICT JSON only with this shape:
            {
              "title": "short supportive title",
              "subtitle": "one short specific sentence",
              "tips": ["tip1", "tip2", "tip3", "tip4", "tip5"]
            }

            Rules:
            - Language: {{tipsLanguage}}
            - Be concrete and personalized.
            - Mention the user's motivation/description if available.
            - Keep each tip to one short sentence.
            - Do not use markdown, numbering, or extra keys.
            - Tips should be actionable in the next 1-5 minutes.

            Addiction info:
            - Title: {{addiction.Title}}
            - Description: {{(string.IsNullOrWhiteSpace(addiction.Description) ? "(not provided)" : addiction.Description)}}
            - Current streak days: {{currentStreakDays}}
            - Resets in last 30 days: {{recentResets.Count}}
            - Trigger events in last 30 days: {{recentTriggerEvents.Count}}

            Recent trigger history:
            {{triggerHistory}}

            Recent reset history:
            {{resetHistory}}
            """;
    }

    private static GenerateTriggerGuidanceResponse BuildFallback(
        Addiction addiction,
        int currentStreakDays,
        int resetCount,
        int triggerCount,
        bool isRussian)
    {
        if (isRussian)
        {
            var subtitle = string.IsNullOrWhiteSpace(addiction.Description)
                ? $"Сейчас важны 5 спокойных минут. Текущая серия — {currentStreakDays} дн."
                : $"Помни, зачем ты это делаешь: {addiction.Description.Trim()}";
            return new GenerateTriggerGuidanceResponse(
                $"Пауза: {addiction.Title}",
                subtitle,
                [
                    "Сделай 10 медленных вдохов и выдохов, считая каждый цикл.",
                    "Уйди из места-триггера на 2 минуты и смени позу тела.",
                    $"Посмотри на серию: {currentStreakDays} дн. Ты уже вложил много усилий.",
                    $"Запиши одной фразой, что ты чувствуешь сейчас (триггеров за 30 дн.: {triggerCount}).",
                    $"Отложи решение на 15 минут. За этот период было срывов: {resetCount}."
                ]);
        }

        var enSubtitle = string.IsNullOrWhiteSpace(addiction.Description)
            ? $"Focus on the next five calm minutes. Current streak: {currentStreakDays} days."
            : $"Remember your why: {addiction.Description.Trim()}";
        return new GenerateTriggerGuidanceResponse(
            $"Pause: {addiction.Title}",
            enSubtitle,
            [
                "Take 10 slow breaths and count each full cycle.",
                "Step away from the trigger place for two minutes.",
                $"Your streak is {currentStreakDays} days, protect that progress.",
                $"Write one sentence about what you feel now (recent triggers: {triggerCount}).",
                $"Delay the decision for 15 minutes. Recent resets: {resetCount}."
            ]);
    }

    private sealed record TriggerGuidancePayload(
        string? Title,
        string? Subtitle,
        IReadOnlyList<string>? Tips
    );
}
