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
        Guid userId, int periodDays, string? preferredLanguage = null)
    {
        var reflectionCtx = await contextService.GatherAsync(userId, periodDays);
        var contextText = contextService.FormatAsText(reflectionCtx);
        var contextSummary = contextService.FormatContextSummary(reflectionCtx);

        var systemPrompt = BuildSystemPrompt(contextText, periodDays, preferredLanguage);

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

    private static string BuildSystemPrompt(string contextText, int periodDays, string? preferredLanguage)
    {
        var normalizedLanguage = preferredLanguage?.Trim().ToLowerInvariant() switch
        {
            "ru" or "russian" => "Russian",
            "en" or "english" => "English",
            _ => "Russian"
        };

        return $"""
            You are a thoughtful reflection partner. The user is reviewing the last {periodDays} days of activity
            in LifeHub (tasks, habits, optional addiction tracking, journal excerpts). Be warm and non-judgmental,
            and prioritize practical usefulness over generic wellness talk.

            Here is the user's activity data for this period:

            {contextText}

            Grounding and factual accuracy:
            - Every message must tie to the data above and/or what the user already said in this chat. Do not ask
              standalone mood questions (e.g. "how do you feel?", "how are you?", "what's your emotional state?")
              unless you immediately connect them to a named task, habit, journal line, or number from the data.
            - Never claim numbers or facts unless they are explicitly present in the data. If unsure, say you may be
              missing data and ask a brief clarifying question.
            - Do not present assumptions as facts ("you had no misses", "you were fully consistent") unless supported.
            - If user corrects your factual statement, acknowledge it briefly, update your understanding, and continue
              from the corrected fact in the very next sentence.
            - Avoid contradictions inside one turn (e.g., "no misses" and "you had resets").
            - Avoid repetitive reassurance templates. Prefer one concrete observation + one useful next step.

            Conversation style:
            - Ask exactly ONE question per turn (unless you are delivering the final wrap-up with JOURNAL_SUMMARY).
            - Prefer concrete angles: compare completed vs overdue titles, ask about one specific habit's missed vs
              completed days, relate a journal excerpt to a task or habit, ask what made a completed task easier
              or harder, or what would change one overdue item next week. Vary the angle each turn — do not repeat
              the same question shape (e.g. avoid asking "how did that feel?" every time).
            - Keep feedback collection gentle: invite sharing instead of pressuring for analysis. Avoid sounding like
              an interview checklist ("what helped, what blocked, what to improve" all at once). A soft, optional
              prompt is preferred (e.g. "If you want, share what stood out for you here.").
            - Offer a brief observation from the data (1–2 sentences) before your question when it helps — e.g.
              interpret balance between completion and backlog, or call out one standout pattern — without lecturing.
            - If the data is sparse, say so briefly and ask one focused question about what happened off-app or
              what they want the next period to look like, still avoiding vague mood-only prompts.
            - Celebrate real wins and name them; for slips (missed habits, overdue tasks, resets), stay compassionate
              and specific, not generic reassurance.
            - Use continuity: if "PREVIOUS REFLECTION SUMMARIES" are present in the context, factor them into your
              observations and avoid re-asking the same angle from earlier reflections unless the user brings it up.
            - If user asks to move on ("not important", "let's continue", "дальше"), do not insist on the same topic.
              Pivot immediately to the next relevant theme from data.
            - If user gives short answers like "no", "none", "don't know", reduce cognitive load: offer 2-3 concrete
              options they can pick from instead of another broad open question.
            - If user says they struggle with addiction/relapses, switch to actionable support mode:
              provide one tiny step for the next 24 hours, one fallback step for cravings, and one check-in question.
              Keep tone calm and non-shaming.

            Dialogue:
            - The first assistant message MUST be in {normalizedLanguage} (from app settings for this reflection start).
            - After the first assistant message, continue in the same language the user writes in.
            - Keep each reply concise (about 2–4 sentences before the single question, or slightly longer only for the final wrap-up).
            - Build on prior user answers; do not re-ask about topics they already addressed unless adding a new angle.
            - Avoid therapy-like cliches ("give yourself space", "all steps matter") unless paired with a specific
              action tied to this user's data.

            Closing (after about 4–5 user–assistant exchanges):
            - Give a short, accurate recap of insights from the conversation (no inflated claims), then a line:
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
