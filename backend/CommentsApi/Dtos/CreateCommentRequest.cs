using System.ComponentModel.DataAnnotations;

namespace CommentsApi.Dtos;

public class CreateCommentRequest
{
    [Required(ErrorMessage = "Укажите имя пользователя.")]
    [StringLength(50, ErrorMessage = "Имя не должно быть длиннее 50 символов.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "Имя может содержать только латинские буквы и цифры.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите e-mail.")]
    [StringLength(254, ErrorMessage = "E-mail не должен быть длиннее 254 символов.")]
    [EmailAddress(ErrorMessage = "Некорректный e-mail.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(2048, ErrorMessage = "Адрес сайта слишком длинный.")]
    [Url(ErrorMessage = "Некорректный адрес сайта.")]
    [RegularExpression(@"^https?://\S+$", ErrorMessage = "Адрес сайта должен начинаться с http:// или https://.")]
    public string? HomePage { get; set; }

    [Required(ErrorMessage = "Введите текст комментария.")]
    [StringLength(5000, ErrorMessage = "Текст не должен быть длиннее 5000 символов.")]
    public string Text { get; set; } = string.Empty;

    // id of the comment we reply to, null for a new top-level comment
    public int? ParentId { get; set; }

    [Required(ErrorMessage = "Не удалось получить CAPTCHA, обновите картинку.")]
    public string CaptchaId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите код с картинки.")]
    [StringLength(10, ErrorMessage = "Код слишком длинный.")]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "Код может содержать только латинские буквы и цифры.")]
    public string CaptchaCode { get; set; } = string.Empty;
}
