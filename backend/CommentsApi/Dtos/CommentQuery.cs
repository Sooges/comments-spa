using System.ComponentModel.DataAnnotations;

namespace CommentsApi.Dtos;

public enum CommentSortField
{
    Date,
    UserName,
    Email
}

public enum SortDirection
{
    Asc,
    Desc
}

public class CommentQuery
{
    // by default the newest comments go first (LIFO)
    [EnumDataType(typeof(CommentSortField))]
    public CommentSortField SortBy { get; set; } = CommentSortField.Date;

    [EnumDataType(typeof(SortDirection))]
    public SortDirection SortOrder { get; set; } = SortDirection.Desc;

    [Range(1, 100000)]
    public int Page { get; set; } = 1;
}
