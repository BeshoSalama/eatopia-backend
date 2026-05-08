using System.ComponentModel.DataAnnotations;

namespace Eatopia.Application.DTOs.Community;

public class CreateCommentDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = null!;
}
