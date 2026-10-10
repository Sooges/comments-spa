using CommentsApi.Data;
using CommentsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommentsApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AttachmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IFileStorage _fileStorage;

    public AttachmentsController(AppDbContext db, IFileStorage fileStorage)
    {
        _db = db;
        _fileStorage = fileStorage;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var attachment = await _db.Attachments
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
        if (attachment == null)
        {
            return NotFound();
        }

        var stream = _fileStorage.Open(attachment.StoredFileName);
        if (stream == null)
        {
            return NotFound();
        }

        // a file never changes, so the browser can keep it
        Response.Headers.CacheControl = "public, max-age=86400";

        // the browser must not guess the type of the file and nothing from the file can run on the page
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";

        return File(stream, attachment.ContentType);
    }
}
