using CommentsApi.Dtos;
using CommentsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CaptchaController : ControllerBase
{
    private readonly ICaptchaService _captchaService;

    public CaptchaController(ICaptchaService captchaService)
    {
        _captchaService = captchaService;
    }

    [HttpGet]
    public ActionResult<CaptchaResponse> Get()
    {
        // the picture must not be cached, otherwise the user gets the same code again
        Response.Headers.CacheControl = "no-store";
        return Ok(_captchaService.Create());
    }
}
