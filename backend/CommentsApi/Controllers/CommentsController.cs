using CommentsApi.Dtos;
using CommentsApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CommentsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentsController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> Create(CreateCommentRequest request)
    {
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
