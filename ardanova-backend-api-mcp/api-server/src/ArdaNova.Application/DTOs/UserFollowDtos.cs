namespace ArdaNova.Application.DTOs;

public record UserFollowDto
{
    public string Id { get; init; }
    public string FollowerId { get; init; }
    public string FollowingId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserFollowDto
{
    public required string FollowerId { get; init; }
    public required string FollowingId { get; init; }
}

public record UserFollowCountsDto
{
    public int FollowersCount { get; init; }
    public int FollowingCount { get; init; }
}
