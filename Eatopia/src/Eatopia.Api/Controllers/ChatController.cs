using Eatopia.Api.Common;
using Eatopia.Api.Hubs;
using Eatopia.Application.DTOs.Chat;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/chat")]
[Route("api/chat")]
[ApiController]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatController(ChatService chatService, IHubContext<ChatHub> hubContext)
    {
        _chatService = chatService;
        _hubContext = hubContext;
    }

    [HttpPost("threads")]
    public async Task<IActionResult> CreateThread(CreateThreadDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var threadId = await _chatService.CreateOrGetThreadAsync(userId, dto);

        return Ok(new { data = new { threadId } });
    }

    [HttpGet("threads")]
    public async Task<IActionResult> GetThreads()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var threads = await _chatService.GetUserThreadsAsync(userId);

        return Ok(new { data = threads });
    }

    [HttpPost("threads/{threadId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid threadId, [FromBody] SendMessageDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.SaveMessageAsync(threadId, userId, dto);
        await _hubContext.Clients.Group(threadId.ToString()).SendAsync("MessageReceived", result);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = result, data = result });
    }

    [HttpPut("threads/{threadId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> UpdateMessage(Guid threadId, Guid messageId, [FromBody] UpdateMessageDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.UpdateMessageAsync(threadId, messageId, userId, dto);
        await _hubContext.Clients.Group(threadId.ToString()).SendAsync("MessageUpdated", result);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = result, data = result });
    }

    [HttpDelete("threads/{threadId:guid}/messages/{messageId:guid}")]
    public async Task<IActionResult> DeleteMessage(Guid threadId, Guid messageId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.DeleteMessageAsync(threadId, messageId, userId);
        await _hubContext.Clients.Group(threadId.ToString()).SendAsync("MessageDeleted", result);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = "Message deleted.", data = result });
    }

    [HttpPut("threads/{threadId:guid}/read")]
    public async Task<IActionResult> MarkThreadRead(Guid threadId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _chatService.MarkThreadReadAsync(threadId, userId);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Group(threadId.ToString()).SendAsync("MessagesSeen", new { threadId, seenByUserId = userId });
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = "Thread marked as read." });
    }

    [HttpPost("threads/{threadId:guid}/accept")]
    public async Task<IActionResult> AcceptMessageRequest(Guid threadId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.AcceptMessageRequestAsync(threadId, userId);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = "Message request accepted.", data = result });
    }

    [HttpDelete("threads/{threadId:guid}/request")]
    public async Task<IActionResult> DeleteMessageRequest(Guid threadId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.DeleteMessageRequestAsync(threadId, userId);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = "Message request deleted.", data = result });
    }

    [HttpPost("threads/{threadId:guid}/block")]
    public async Task<IActionResult> BlockThreadUser(Guid threadId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _chatService.BlockThreadUserAsync(threadId, userId);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadId);
        await _hubContext.Clients.Groups(participantIds.Select(id => ChatHub.UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });

        return Ok(new { success = true, message = "User blocked.", data = result });
    }

    [HttpGet("threads/{threadId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessages(
        Guid threadId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? pageIndex = null)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var result = await _chatService.GetMessagesAsync(threadId, userId, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);

        return Ok(new
        {
            data = result.Items,
            meta = PaginationHelper.ToMeta(result)
        });
    }
}
