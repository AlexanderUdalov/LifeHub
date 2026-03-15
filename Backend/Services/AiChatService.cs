using System.Collections.Concurrent;
using Microsoft.Extensions.AI;

namespace LifeHub.Services;

public class AiChatService(
    ReflectionContextService contextService,
    IConfiguration configuration,
    IChatClient? chatClient = null)
{
    private static readonly ConcurrentDictionary<Guid, CachedPrompt> PromptCache = new();

    private record CachedPrompt(string SystemPrompt, DateTimeOffset CreatedAt);

    private int MaxSteps => configuration.GetValue("Ai:MaxReflectionSteps", 5);

    public async Task<(Guid ContextId, string ContextSummary, string Message)> StartReflectionAsync(
        Guid userId, int periodDays)
    {
        var reflectionCtx = await contextService.GatherAsync(userId, periodDays);
        var contextText = contextService.FormatAsText(reflectionCtx);
        var contextSummary = contextService.FormatContextSummary(reflectionCtx);

        var systemPrompt = BuildSystemPrompt(contextText, periodDays);

        var contextId = Guid.NewGuid();
        PromptCache[contextId] = new CachedPrompt(systemPrompt, DateTimeOffset.UtcNow);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt)
        };

        var response = await GetChatClient().GetResponseAsync(messages);
        var assistantText = response.Text ?? "";

        return (contextId, contextSummary, assistantText);
    }

    public async Task<(string Message, bool IsComplete, string? JournalSummary)> SendMessageAsync(
        Guid contextId, List<ChatMessageDTO> history)
    {
        if (!PromptCache.TryGetValue(contextId, out var cached))
            throw new InvalidOperationException("Reflection context not found or expired.");

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, cached.SystemPrompt)
        };

        foreach (var msg in history)
        {
            var role = msg.Role.ToLowerInvariant() switch
            {
                "user" => ChatRole.User,
                "assistant" => ChatRole.Assistant,
                _ => ChatRole.User
            };
            messages.Add(new ChatMessage(role, msg.Content));
        }

        var response = await GetChatClient().GetResponseAsync(messages);
        var assistantText = response.Text ?? "";

        bool hasCompleteMarker = assistantText.Contains("[REFLECTION_COMPLETE]");
        bool isComplete = hasCompleteMarker;

        string? journalSummary = null;
        if (hasCompleteMarker)
        {
            const string journalPrefix = "JOURNAL_SUMMARY:";
            var markerIdx = assistantText.IndexOf("[REFLECTION_COMPLETE]", StringComparison.Ordinal);
            var beforeMarker = assistantText[..markerIdx].TrimEnd();
            var summaryLineStart = beforeMarker.IndexOf(journalPrefix, StringComparison.OrdinalIgnoreCase);
            if (summaryLineStart >= 0)
            {
                journalSummary = beforeMarker[(summaryLineStart + journalPrefix.Length)..].Trim();
                assistantText = beforeMarker[..summaryLineStart].TrimEnd();
            }
            else
            {
                assistantText = beforeMarker;
            }
        }
        else if (isComplete)
        {
            assistantText = assistantText.TrimEnd();
        }

        return (assistantText, isComplete, journalSummary);
    }

    private IChatClient GetChatClient() =>
        chatClient ?? throw new InvalidOperationException(
            "AI is not configured. Set Ai:ApiKey in appsettings or environment variables.");

    private static string BuildSystemPrompt(string contextText, int periodDays)
    {
        return $"""
            You are an empathetic personal reflection coach. The user is reviewing their life progress
            over the last {periodDays} days. Your role is to help them reflect on their achievements
            and struggles with warmth and understanding.

            Here is the user's activity data for this period:

            {contextText}

            Guidelines:
            - Ask ONE thoughtful reflective question at a time
            - Base your questions on the actual data provided above
            - If the user completed many tasks or maintained habit streaks, acknowledge and celebrate their effort
            - If there are missed habits, overdue tasks, or addiction resets, approach with compassion — not judgment
            - Help the user identify patterns and understand what helped or hindered them
            - Use a warm, supportive tone — like a caring friend, not a strict coach
            - Respond in the same language the user writes in
            - Keep responses concise (2-4 sentences per message)
            - After about 4–5 exchanges, wrap up naturally: give a brief encouraging summary of insights, then add a line:
              JOURNAL_SUMMARY: (2–4 sentences summarizing the user's results and emotions for this period, like a short journal note — focus on what the user shared, not the data).
            - On the next line after JOURNAL_SUMMARY, write exactly: [REFLECTION_COMPLETE]
            - Do not end the dialogue abruptly; always provide the summary and JOURNAL_SUMMARY before [REFLECTION_COMPLETE].
            """;
    }

    public static void CleanupExpiredPrompts(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var expired = PromptCache
            .Where(kvp => kvp.Value.CreatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expired)
            PromptCache.TryRemove(key, out _);
    }
}

public record ChatMessageDTO(string Role, string Content);
