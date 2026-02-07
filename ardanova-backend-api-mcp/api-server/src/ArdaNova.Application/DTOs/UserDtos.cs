namespace ArdaNova.Application.DTOs;

using ArdaNova.Domain.Models.Enums;

public record UserDto
{
    public string Id { get; init; }
    public string Email { get; init; } = null!;
    public DateTime? EmailVerified { get; init; }
    public string? Name { get; init; }
    public string? Image { get; init; }
    public string? Bio { get; init; }
    public string? Location { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? LinkedIn { get; init; }
    public string? Twitter { get; init; }
    public UserRole Role { get; init; }
    public UserType UserType { get; init; }
    public bool IsVerified { get; init; }
    public VerificationLevel VerificationLevel { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CreateUserDto
{
    public required string Email { get; init; }
    public string? Name { get; init; }
    public UserRole Role { get; init; } = UserRole.INDIVIDUAL;
    public UserType UserType { get; init; } = UserType.INNOVATOR;
}

public record UpdateUserDto
{
    public string? Name { get; init; }
    public string? Bio { get; init; }
    public string? Location { get; init; }
    public string? Phone { get; init; }
    public string? Website { get; init; }
    public string? LinkedIn { get; init; }
    public string? Twitter { get; init; }
    public string? Image { get; init; }
}

public record AdminUpdateUserRoleDto
{
    public required UserRole Role { get; init; }
}

public record AdminUpdateUserTypeDto
{
    public required UserType UserType { get; init; }
}

public record AdminUpdateVerificationLevelDto
{
    public required VerificationLevel VerificationLevel { get; init; }
}

public record UserSkillDto
{
    public string Id { get; init; }
    public string UserId { get; init; }
    public string Skill { get; init; } = null!;
    public int Level { get; init; }
}

public record CreateUserSkillDto
{
    public required string UserId { get; init; }
    public required string Skill { get; init; }
    public int Level { get; init; } = 1;
}

public record UpdateUserSkillDto
{
    public int? Level { get; init; }
}

public record UserExperienceDto
{
    public string Id { get; init; }
    public string UserId { get; init; }
    public string Title { get; init; } = null!;
    public string Company { get; init; } = null!;
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsCurrent { get; init; }
}

public record CreateUserExperienceDto
{
    public required string UserId { get; init; }
    public required string Title { get; init; }
    public required string Company { get; init; }
    public string? Description { get; init; }
    public required DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool IsCurrent { get; init; }
}

public record UpdateUserExperienceDto
{
    public string? Title { get; init; }
    public string? Company { get; init; }
    public string? Description { get; init; }
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public bool? IsCurrent { get; init; }
}

public record AccountDto
{
    public string Id { get; init; }
    public string UserId { get; init; }
    public string Type { get; init; } = null!;
    public string Provider { get; init; } = null!;
    public string ProviderAccountId { get; init; } = null!;
}

public record SessionDto
{
    public string Id { get; init; }
    public string SessionToken { get; init; } = null!;
    public string UserId { get; init; }
    public DateTime Expires { get; init; }
}

public record VerificationTokenDto
{
    public string Identifier { get; init; } = null!;
    public string Token { get; init; } = null!;
    public DateTime Expires { get; init; }
}
