using Eatopia.Application.DTOs.Chat;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Eatopia.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ChatService _chatService;

    public ChatHub(ChatService chatService)
    {
        _chatService = chatService;
    }

    public static string UserGroupName(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdValue, out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinThread(string threadId)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var threadGuid = Guid.Parse(threadId);

        var isMember = await _chatService.IsUserInThreadAsync(threadGuid, userId);
        if (!isMember)
            throw new HubException("FORBIDDEN");

        await Groups.AddToGroupAsync(Context.ConnectionId, threadId);
    }

    public async Task SendMessage(string threadId, SendMessageDto dto)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var threadGuid = Guid.Parse(threadId);

        var saved = await _chatService.SaveMessageAsync(threadGuid, userId, dto);

        await Clients.Group(threadId).SendAsync("MessageReceived", saved);

        var participantIds = await _chatService.GetThreadParticipantUserIdsAsync(threadGuid);
        await Clients.Groups(participantIds.Select(id => UserGroupName(id)).ToList()).SendAsync("ThreadChanged", new { threadId });
    }

    public async Task Typing(string threadId, bool isTyping)
    {
        var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var threadGuid = Guid.Parse(threadId);

        var isMember = await _chatService.IsUserInThreadAsync(threadGuid, userId);
        if (!isMember)
            throw new HubException("FORBIDDEN");

        await Clients.OthersInGroup(threadId).SendAsync("UserTyping", new { threadId, userId, isTyping });
    }
}
