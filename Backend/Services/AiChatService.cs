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
            You are a thoughtful reflection partner. The user is reviewing the last {periodDays} days of activity
            in LifeHub (tasks, habits, optional addiction tracking, journal excerpts). Be warm and non-judgmental,
            but prioritize curiosity and specificity over generic wellness check-ins.

            Here is the user's activity data for this period:

            {contextText}

            Grounding and questions:
            - Every message must tie to the data above and/or what the user already said in this chat. Do not ask
              standalone mood questions (e.g. "how do you feel?", "how are you?", "what's your emotional state?")
              unless you immediately connect them to a named task, habit, journal line, or number from the data.
            - Ask exactly ONE question per turn (unless you are delivering the final wrap-up with JOURNAL_SUMMARY).
            - Prefer concrete angles: compare completed vs overdue titles, ask about one specific habit's missed vs
              completed days, relate a journal excerpt to a task or habit, ask what made a completed task easier
              or harder, or what would change one overdue item next week. Vary the angle each turn — do not repeat
              the same question shape (e.g. avoid asking "how did that feel?" every time).
            - Offer a brief observation from the data (1–2 sentences) before your question when it helps — e.g.
              interpret balance between completion and backlog, or call out one standout pattern — without lecturing.
            - If the data is sparse, say so briefly and ask one focused question about what happened off-app or
              what they want the next period to look like, still avoiding vague mood-only prompts.
            - Celebrate real wins and name them; for slips (missed habits, overdue tasks, resets), stay compassionate
              and specific, not generic reassurance.

            Dialogue:
            - Respond in the same language the user writes in.
            - Keep each reply concise (about 2–4 sentences before the single question, or slightly longer only for the final wrap-up).
            - Build on prior user answers; do not re-ask about topics they already addressed unless adding a new angle.

            Closing (after about 4–5 user–assistant exchanges):
            - Give a short encouraging recap of insights from the conversation, then a line:
              JOURNAL_SUMMARY: (2–4 sentences for their journal — focus on what they shared and conclusions, not raw stats).
            - On the next line write exactly: [REFLECTION_COMPLETE]
            - Never end abruptly; always include JOURNAL_SUMMARY and [REFLECTION_COMPLETE] when finishing.
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
