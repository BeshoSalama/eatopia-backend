using Eatopia.Api.Common;
using Eatopia.Application.DTOs.Community;
using Eatopia.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Eatopia.Api.Controllers;

[Route("api/v1/community")]
[Route("api/community")]
[ApiController]
[Authorize]
public class CommunityController : ControllerBase
{
    private readonly CommunityService _communityService;

    public CommunityController(CommunityService communityService)
    {
        _communityService = communityService;
    }

    [HttpPost("presence/heartbeat")]
    public async Task<IActionResult> Heartbeat()
    {
        await _communityService.TouchPresenceAsync(GetUserId());
        return Ok(new { success = true, message = "Presence updated." });
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] int? pageIndex = null)
    {
        var result = await _communityService.GetPostsAsync(GetUserId(), PaginationHelper.ToPageIndex(page, pageIndex), pageSize);
        return Ok(new { success = true, posts = result.Items, data = result.Items, meta = PaginationHelper.ToMeta(result) });
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var post = await _communityService.CreatePostAsync(GetUserId(), dto);
        return Ok(new { success = true, message = "Post created.", post, data = post });
    }

    [HttpPut("posts/{id:guid}")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] CreatePostDto dto)
    {
        var post = await _communityService.UpdatePostAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Post updated.", post, data = post });
    }

    [HttpDelete("posts/{id:guid}")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        await _communityService.DeletePostAsync(GetUserId(), id);
        return Ok(new { success = true, message = "Post deleted." });
    }

    [HttpPost("posts/{id:guid}/hide")]
    public async Task<IActionResult> HidePost(Guid id)
    {
        await _communityService.HidePostAsync(GetUserId(), id);
        return Ok(new { success = true, message = "Post hidden." });
    }

    [HttpPost("posts/{id:guid}/report")]
    public async Task<IActionResult> ReportPost(Guid id, [FromBody] ReportContentDto dto)
    {
        await _communityService.ReportPostAsync(GetUserId(), id, dto.Reason);
        return Ok(new { success = true, message = "Post reported. Thank you for helping keep the community safe." });
    }

    [HttpPost("posts/{id:guid}/share-profile")]
    public async Task<IActionResult> ShareToProfile(Guid id, [FromBody] SharePostToProfileDto dto)
    {
        var post = await _communityService.SharePostToProfileAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Post shared to your profile.", post, data = post });
    }

    [HttpPost("posts/{id:guid}/share-message")]
    public async Task<IActionResult> ShareAsMessage(Guid id, [FromBody] SharePostToMessageDto dto)
    {
        var result = await _communityService.SharePostAsMessageAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Post sent as message.", data = result });
    }

    [HttpPost("posts/{id:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentDto dto)
    {
        var comment = await _communityService.AddCommentAsync(GetUserId(), id, dto);
        return Ok(new { success = true, message = "Comment added.", comment, data = comment });
    }

    [HttpGet("posts/{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, [FromQuery] int? pageIndex = null)
    {
        var result = await _communityService.GetCommentsAsync(id, PaginationHelper.ToPageIndex(page, pageIndex), pageSize);
        return Ok(new { success = true, comments = result.Items, data = result.Items, meta = PaginationHelper.ToMeta(result) });
    }

    [HttpPut("posts/{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(Guid postId, Guid commentId, [FromBody] CreateCommentDto dto)
    {
        var comment = await _communityService.UpdateCommentAsync(GetUserId(), commentId, dto);
        return Ok(new { success = true, message = "Comment updated.", comment, data = comment });
    }

    [HttpDelete("posts/{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId)
    {
        await _communityService.DeleteCommentAsync(GetUserId(), commentId);
        return Ok(new { success = true, message = "Comment deleted." });
    }

    [HttpPost("posts/{postId:guid}/comments/{commentId:guid}/report")]
    public async Task<IActionResult> ReportComment(Guid postId, Guid commentId, [FromBody] ReportContentDto dto)
    {
        await _communityService.ReportCommentAsync(GetUserId(), commentId, dto.Reason);
        return Ok(new { success = true, message = "Comment reported." });
    }

    [HttpPost("posts/{id:guid}/like")]
    public async Task<IActionResult> Like(Guid id) { await _communityService.LikePostAsync(GetUserId(), id); return Ok(new { success = true, message = "Liked" }); }

    [HttpDelete("posts/{id:guid}/like")]
    public async Task<IActionResult> Unlike(Guid id) { await _communityService.UnlikePostAsync(GetUserId(), id); return Ok(new { success = true, message = "Unliked" }); }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile([FromQuery] Guid? userId = null)
    {
        var profile = await _communityService.GetUserCommunityProfileAsync(GetUserId(), userId ?? GetUserId());
        return Ok(new { success = true, profile, data = profile });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] string? search = null, [FromQuery] bool friendsOnly = false)
    {
        var users = await _communityService.SearchUsersAsync(GetUserId(), search, friendsOnly);
        return Ok(new { success = true, users, data = users });
    }

    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers([FromQuery] Guid? userId = null) => Ok(new { success = true, users = await _communityService.GetFollowersAsync(GetUserId(), userId) });

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing([FromQuery] Guid? userId = null) => Ok(new { success = true, users = await _communityService.GetFollowingAsync(GetUserId(), userId) });

    [HttpPost("users/{targetUserId:guid}/follow")]
    public async Task<IActionResult> Follow(Guid targetUserId)
    {
        var profile = await _communityService.FollowUserAsync(GetUserId(), targetUserId);
        return Ok(new { success = true, message = "User followed.", profile, data = profile });
    }

    [HttpDelete("users/{targetUserId:guid}/follow")]
    public async Task<IActionResult> Unfollow(Guid targetUserId)
    {
        var profile = await _communityService.UnfollowUserAsync(GetUserId(), targetUserId);
        return Ok(new { success = true, message = "User unfollowed.", profile, data = profile });
    }

    [HttpPost("users/{targetUserId:guid}/block")]
    public async Task<IActionResult> BlockUser(Guid targetUserId)
    {
        var result = await _communityService.BlockUserAsync(GetUserId(), targetUserId);
        return Ok(new { success = true, message = "User blocked.", data = result });
    }

    [HttpPost("users/{targetUserId:guid}/report")]
    public async Task<IActionResult> ReportUser(Guid targetUserId, [FromBody] ReportContentDto dto)
    {
        await _communityService.ReportUserAsync(GetUserId(), targetUserId, dto.Reason);
        return Ok(new { success = true, message = "User reported. The admin team will review it." });
    }

    [HttpDelete("users/{targetUserId:guid}/block")]
    public async Task<IActionResult> UnblockUser(Guid targetUserId)
    {
        await _communityService.UnblockUserAsync(GetUserId(), targetUserId);
        return Ok(new { success = true, message = "User unblocked." });
    }

    [HttpGet("blocked-users")]
    public async Task<IActionResult> GetBlockedUsers()
    {
        var users = await _communityService.GetBlockedUsersAsync(GetUserId());
        return Ok(new { success = true, users, data = users });
    }

    [HttpPost("messages/{messageId:guid}/report")]
    public async Task<IActionResult> ReportMessage(Guid messageId, [FromBody] ReportContentDto dto)
    {
        await _communityService.ReportMessageAsync(GetUserId(), messageId, dto.Reason);
        return Ok(new { success = true, message = "Message reported." });
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
