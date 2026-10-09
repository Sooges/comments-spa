using System.ComponentModel.DataAnnotations;

namespace CommentsApi.Dtos;

public class PreviewRequest
{
    [Required(ErrorMessage = "Введите текст комментария.")]
    [StringLength(5000, ErrorMessage = "Текст не должен быть длиннее 5000 символов.")]
    public string Text { get; set; } = string.Empty;
}
