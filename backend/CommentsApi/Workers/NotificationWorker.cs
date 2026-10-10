using CommentsApi.Hubs;
using CommentsApi.Queue;
using Microsoft.AspNetCore.SignalR;

namespace CommentsApi.Workers;

// Works in the background: takes the notifications from the queue and sends them to all the browsers.
public class NotificationWorker : BackgroundService
{
    private readonly INotificationQueue _queue;
    private readonly IHubContext<CommentsHub> _hub;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(INotificationQueue queue, IHubContext<CommentsHub> hub, ILogger<NotificationWorker> logger)
    {
        _queue = queue;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _hub.Clients.All.SendAsync(
                    "CommentAdded",
                    new { commentId = notification.CommentId, parentId = notification.ParentId },
                    stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // one failed message must not stop the worker
                _logger.LogError(exception, "Could not send the notification about the comment {CommentId}", notification.CommentId);
            }
        }
    }
}
