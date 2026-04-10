using LifeHub.Services;

namespace LifeHub.DTOs;

public record StartReflectionRequest(int PeriodDays, string? Language = null);

public record StartReflectionResponse(
    Guid ContextId,
    string ContextSummary,
    string Message
);

public record SendReflectionMessageRequest(
    Guid ContextId,
    List<ChatMessageDTO> Messages
);

public record ReflectionMessageResponse(
    string Message,
    bool IsComplete,
    string? JournalSummary = null
);
