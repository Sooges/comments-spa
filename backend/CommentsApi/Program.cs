using CommentsApi.Data;
using CommentsApi.Events;
using CommentsApi.Hubs;
using CommentsApi.Queue;
using CommentsApi.Services;
using CommentsApi.Workers;
using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddSingleton<INotificationQueue, NotificationQueue>();
builder.Services.AddHostedService<NotificationWorker>();

builder.Services.AddSignalR();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddSingleton<IHtmlTextProcessor, HtmlTextProcessor>();

builder.Services.AddSingleton<IFileStorage, FileStorage>();
builder.Services.AddSingleton<IAttachmentProcessor, AttachmentProcessor>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();

// In Docker the API works behind the nginx of the web container, and the real address of the client
// comes in the X-Forwarded-For header. The port of the API is not open outside of the Docker network,
// so the header can be trusted.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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

app.UseForwardedHeaders();

app.UseAuthorization();

app.MapControllers();
app.MapHub<CommentsHub>("/hubs/comments");

app.Run();
