using CommentsApi.Dtos;

namespace CommentsApi.Services;

public interface ICaptchaService
{
    /// <summary>
    /// Creates a new captcha picture and remembers its code for a few minutes.
    /// </summary>
    CaptchaResponse Create();

    /// <summary>
    /// Checks the code entered by the user. Every captcha can be checked only once.
    /// </summary>
    bool Validate(string id, string code);
}
