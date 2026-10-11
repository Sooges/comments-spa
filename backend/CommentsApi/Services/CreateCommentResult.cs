using CommentsApi.Dtos;

namespace CommentsApi.Services;

public enum CreateCommentError
{
    ParentNotFound,
    TooDeep
}

public class CreateCommentResult
{
    public CommentDto? Comment { get; private init; }
    public CreateCommentError? Error { get; private init; }

    public static CreateCommentResult Created(CommentDto comment)
    {
        return new CreateCommentResult { Comment = comment };
    }

    public static CreateCommentResult Failed(CreateCommentError error)
    {
        return new CreateCommentResult { Error = error };
    }
}
