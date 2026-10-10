using CommentsApi.Dtos;
using CommentsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IHtmlTextProcessor _htmlTextProcessor;
    private readonly ICaptchaService _captchaService;
    private readonly IAttachmentProcessor _attachmentProcessor;

    public CommentsController(
        ICommentService commentService,
        IHtmlTextProcessor htmlTextProcessor,
        ICaptchaService captchaService,
        IAttachmentProcessor attachmentProcessor)
    {
        _commentService = commentService;
        _htmlTextProcessor = htmlTextProcessor;
        _captchaService = captchaService;
        _attachmentProcessor = attachmentProcessor;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments([FromQuery] CommentQuery query)
    {
        var page = await _commentService.GetPageAsync(query);
        return Ok(page);
    }

    // shows how the text will look like, nothing is saved
    [HttpPost("preview")]
    public ActionResult<PreviewResponse> Preview(PreviewRequest request)
    {
        var text = _htmlTextProcessor.Process(request.Text);
        if (!text.IsValid)
        {
            ModelState.AddModelError(nameof(request.Text), text.Error!);
            return ValidationProblem(ModelState);
        }

        return Ok(new PreviewResponse { Html = text.Html });
    }

    // the data is sent as a form, because it can contain a file
    [HttpPost]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<CommentDto>> Create([FromForm] CreateCommentRequest request)
    {
        var text = _htmlTextProcessor.Process(request.Text);
        if (!text.IsValid)
        {
            ModelState.AddModelError(nameof(request.Text), text.Error!);
            return ValidationProblem(ModelState);
        }

        PreparedAttachment? attachment = null;
        if (request.File != null)
        {
            var file = await _attachmentProcessor.ProcessAsync(request.File);
            if (!file.IsValid)
            {
                ModelState.AddModelError(nameof(request.File), file.Error!);
                return ValidationProblem(ModelState);
            }

            attachment = file.Attachment;
        }

        // the captcha is checked last, because every captcha can be used only once
        if (!_captchaService.Validate(request.CaptchaId, request.CaptchaCode))
        {
            ModelState.AddModelError(nameof(request.CaptchaCode), "Неверный код с картинки.");
            return ValidationProblem(ModelState);
        }

        // only the checked and escaped HTML is saved
        request.Text = text.Html;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var comment = await _commentService.CreateAsync(request, attachment, ipAddress, userAgent);
        if (comment == null)
        {
            ModelState.AddModelError(nameof(request.ParentId), "Parent comment not found.");
            return ValidationProblem(ModelState);
        }

        return Ok(comment);
    }
}
