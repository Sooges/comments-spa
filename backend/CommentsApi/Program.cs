using CommentsApi.Data;
using CommentsApi.Events;
using CommentsApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<ICommentEvents, CommentEvents>();
builder.Services.AddSingleton<ICommentCache, CommentCache>();
builder.Services.AddSingleton<CommentEventSubscriber>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<IHtmlTextProcessor, HtmlTextProcessor>();

builder.Services.AddSingleton<IFileStorage, FileStorage>();
builder.Services.AddSingleton<IAttachmentProcessor, AttachmentProcessor>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();

var app = builder.Build();

// The subscriber starts to listen to the comment events as soon as it is created
app.Services.GetRequiredService<CommentEventSubscriber>();

// Create the database and apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
