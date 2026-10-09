using System.ComponentModel.DataAnnotations;

namespace CommentsApi.Dtos;

public class CreateCommentRequest
{
    [Required]
    [StringLength(50)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? HomePage { get; set; }

    [Required]
    [StringLength(5000)]
    public string Text { get; set; } = string.Empty;

    // id of the comment we reply to, null for a new top-level comment
    public int? ParentId { get; set; }
}
