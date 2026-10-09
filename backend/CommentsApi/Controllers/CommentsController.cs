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

    public CommentsController(ICommentService commentService, IHtmlTextProcessor htmlTextProcessor)
    {
        _commentService = commentService;
        _htmlTextProcessor = htmlTextProcessor;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CommentDto>>> GetComments([FromQuery] CommentQuery query)
    {
        var page = await _commentService.GetPageAsync(query);
        return Ok(page);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(CreateCommentRequest request)
    {
        var text = _htmlTextProcessor.Process(request.Text);
        if (!text.IsValid)
        {
            ModelState.AddModelError(nameof(request.Text), text.Error!);
            return ValidationProblem(ModelState);
        }

        // only the checked and escaped HTML is saved
        request.Text = text.Html;

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers.UserAgent.ToString();

        var comment = await _commentService.CreateAsync(request, ipAddress, userAgent);
        if (comment == null)
        {
            ModelState.AddModelError(nameof(request.ParentId), "Parent comment not found.");
            return ValidationProblem(ModelState);
        }

        return Ok(comment);
    }
}
