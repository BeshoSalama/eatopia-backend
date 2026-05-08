using Eatopia.Tests.Support;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Eatopia.Tests.Integration;

public class ChatApiTests
{
    [Fact]
    public async Task User_can_send_voice_message_request_and_receiver_gets_actionable_notification()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var sender = await factory.AddUserAsync("sender@test.com");
        var receiver = await factory.AddUserAsync("receiver@test.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(sender));
        var threadResponse = await client.PostAsJsonAsync("/api/chat/threads", new { otherUserId = receiver.Id });
        threadResponse.EnsureSuccessStatusCode();

        using var threadJson = await JsonDocument.ParseAsync(await threadResponse.Content.ReadAsStreamAsync());
        var threadId = threadJson.RootElement.GetProperty("data").GetProperty("thread_id").GetGuid();

        var sendResponse = await client.PostAsJsonAsync($"/api/chat/threads/{threadId}/messages", new
        {
            messageType = "audio",
            mediaContent = "/uploads/chat-audio/2026/05/voice-note.webm",
            fileName = "voice-note.webm"
        });

        sendResponse.EnsureSuccessStatusCode();

        await factory.WithDbAsync(async db =>
        {
            var message = await db.ChatMessages.SingleAsync(x => x.ThreadId == threadId);
            var notification = await db.Notifications.SingleAsync(x => x.UserId == receiver.Id && x.Type == "message_request");

            Assert.Equal("audio", message.MessageType);
            Assert.Equal("/uploads/chat-audio/2026/05/voice-note.webm", message.MediaContent);
            Assert.Contains(sender.Username!, notification.Title);
            Assert.Equal($"/communityProfile?userId={sender.Id}", notification.ActionUrl);
        });
    }

    [Fact]
    public async Task Chat_media_messages_must_use_uploaded_urls_not_data_uris()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();

        var sender = await factory.AddUserAsync("sender@test.com");
        var receiver = await factory.AddUserAsync("receiver@test.com");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(sender));
        var threadResponse = await client.PostAsJsonAsync("/api/chat/threads", new { otherUserId = receiver.Id });
        threadResponse.EnsureSuccessStatusCode();
        using var threadJson = await JsonDocument.ParseAsync(await threadResponse.Content.ReadAsStreamAsync());
        var threadId = threadJson.RootElement.GetProperty("data").GetProperty("thread_id").GetGuid();

        var sendResponse = await client.PostAsJsonAsync($"/api/chat/threads/{threadId}/messages", new
        {
            messageType = "audio",
            mediaContent = "data:audio/webm;base64,AAAA",
            fileName = "inline.webm"
        });

        Assert.Equal(HttpStatusCode.BadRequest, sendResponse.StatusCode);
    }
}
