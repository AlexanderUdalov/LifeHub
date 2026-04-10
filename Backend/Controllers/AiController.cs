using LifeHub.DTOs;
using LifeHub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AiController(AiChatService aiChatService) : ControllerBase
{
    [HttpPost("reflection/start")]
    [ProducesResponseType(typeof(StartReflectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StartReflectionResponse>> StartReflection(StartReflectionRequest request)
    {
        if (request.PeriodDays is < 1 or > 90)
            return BadRequest("Period must be between 1 and 90 days.");

        var userId = User.GetUserId();
        var (contextId, contextSummary, message) = await aiChatService.StartReflectionAsync(
            userId,
            request.PeriodDays,
            request.Language);

        return new StartReflectionResponse(contextId, contextSummary, message);
    }

    [HttpPost("reflection/message")]
    [ProducesResponseType(typeof(ReflectionMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReflectionMessageResponse>> SendMessage(SendReflectionMessageRequest request)
    {
        if (request.Messages.Count == 0)
            return BadRequest("Messages list cannot be empty.");

        try
        {
            var (message, isComplete, journalSummary) = await aiChatService.SendMessageAsync(request.ContextId, request.Messages);
            return new ReflectionMessageResponse(message, isComplete, journalSummary);
        }
        catch (InvalidOperationException)
        {
            return BadRequest("Reflection context not found or expired. Please start a new reflection.");
        }
    }
}
