using API.Authorization;
using Domain.Security;
using Domain.Services.Chat;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : BaseController
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>Get all conversations for the current user.</summary>
    [HttpGet("conversations")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetConversations([FromQuery] string? source)
    {
        var conversations = await _chatService.GetConversationsAsync(UserId ?? "unknown", source);
        return Ok(conversations);
    }

    /// <summary>Get messages for a conversation.</summary>
    [HttpGet("conversations/{id}/messages")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> GetMessages(int id)
    {
        var messages = await _chatService.GetMessagesAsync(id);
        return Ok(messages);
    }

    /// <summary>Create a new conversation.</summary>
    [HttpPost("conversations")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequest request)
    {
        var conv = await _chatService.CreateConversationAsync(UserId ?? "unknown", request.Title, request.Source);
        return Ok(conv);
    }

    /// <summary>Delete a conversation.</summary>
    [HttpDelete("conversations/{id}")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task<IActionResult> DeleteConversation(int id)
    {
        await _chatService.DeleteConversationAsync(id);
        return NoContent();
    }

    /// <summary>Send a message (streaming SSE).</summary>
    [HttpPost("conversations/{id}/send")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public async Task SendMessage(int id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        await foreach (var chunk in _chatService.StreamResponseAsync(id, UserId ?? "unknown", request.Content))
        {
            if (ct.IsCancellationRequested) break;
            await Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(new { content = chunk })}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
        await Response.WriteAsync("data: [DONE]\n\n", ct);
    }

    /// <summary>Semantic search via embeddings.</summary>
    [HttpGet("search")]
    [RequireAccessFunction(AccessFunctionCodes.Api.ChatUse)]
    public IActionResult Search([FromQuery] string q)
    {
        return Ok(new { query = q, results = Array.Empty<object>(), message = "pgvector semantic search endpoint" });
    }
}

public class CreateConversationRequest
{
    public string Title { get; set; } = "New Conversation";
    public string Source { get; set; } = "General";
}

public class SendMessageRequest
{
    public string Content { get; set; } = default!;
}
