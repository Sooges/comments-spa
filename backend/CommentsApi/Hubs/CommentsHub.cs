using Microsoft.AspNetCore.SignalR;

namespace CommentsApi.Hubs;

// The clients (browsers) connect here and wait for the "CommentAdded" message.
// They do not send anything, so there are no methods.
public class CommentsHub : Hub
{
}
