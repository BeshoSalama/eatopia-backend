using Eatopia.Tests.Support;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Eatopia.Tests.Integration;

public class UploadsApiTests
{
    [Fact]
    public async Task Upload_rejects_unsupported_file_types()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var user = await factory.AddUserAsync("uploader@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(user));

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent([1, 2, 3]), "file", "malware.exe");
        form.Add(new StringContent("profile"), "purpose");

        var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_accepts_chat_audio_and_returns_portable_upload_url()
    {
        using var factory = new EatopiaApiFactory();
        await factory.ResetDatabaseAsync();
        using var client = factory.CreateClient();
        var user = await factory.AddUserAsync("voice@test.com");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.CreateToken(user));

        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent([0x1A, 0x45, 0xDF, 0xA3]);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
        form.Add(content, "file", "voice-note.webm");
        form.Add(new StringContent("chat-audio"), "purpose");

        var response = await client.PostAsync("/api/uploads", form);

        response.EnsureSuccessStatusCode();
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var relativeUrl = json.RootElement.GetProperty("relative_url").GetString();
        var absoluteUrl = json.RootElement.GetProperty("url").GetString();
        Assert.StartsWith("/uploads/chat-audio/", relativeUrl);
        Assert.EndsWith(".webm", relativeUrl);
        Assert.StartsWith("http://localhost:3001/uploads/chat-audio/", absoluteUrl);

        var staticResponse = await client.GetAsync(relativeUrl);
        staticResponse.EnsureSuccessStatusCode();
    }
}
