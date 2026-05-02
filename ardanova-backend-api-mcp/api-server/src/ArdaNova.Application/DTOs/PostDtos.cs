namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record PostDto
{
    public string Id { get; init; } = null!;
    public string AuthorId { get; init; } = null!;
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public string Type { get; init; } = null!;
    public string Visibility { get; init; } = null!;
    public string? Title { get; init; }
    public string Content { get; init; } = null!;
    public string? Metadata { get; init; }
    public int LikesCount { get; init; }
    public int CommentsCount { get; init; }
    public int SharesCount { get; init; }
    public int ViewsCount { get; init; }
    public bool IsPinned { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreatePostDto
{
    public required string AuthorId { get; init; }
    public string? ProjectId { get; init; }
    public string? GuildId { get; init; }
    public PostType Type { get; init; } = PostType.POST;
    public PostVisibility Visibility { get; init; } = PostVisibility.PUBLIC;
    public string? Title { get; init; }
    public required string Content { get; init; }
    public string? Metadata { get; init; }
}

public record UpdatePostDto
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public PostVisibility? Visibility { get; init; }
    public string? Metadata { get; init; }
    public bool? IsPinned { get; init; }
}

public record PostCommentDto
{
    public string Id { get; init; } = null!;
    public string PostId { get; init; } = null!;
    public string AuthorId { get; init; } = null!;
    public string? ParentId { get; init; }
    public string Content { get; init; } = null!;
    public int LikesCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreatePostCommentDto
{
    public required string AuthorId { get; init; }
    public string? ParentId { get; init; }
    public required string Content { get; init; }
}

public record CreatePostShareDto
{
    public string? SharedToProjectId { get; init; }
    public string? SharedToGuildId { get; init; }
    public string? Comment { get; init; }
}
