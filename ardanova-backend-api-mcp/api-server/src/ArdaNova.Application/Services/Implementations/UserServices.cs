namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using System.Text.RegularExpressions;

public class UserService : IUserService
{
    private readonly IRepository<User> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IRepository<User> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _repository.GetAllAsync(ct);
        return Result<IReadOnlyList<UserDto>>.Success(_mapper.Map<IReadOnlyList<UserDto>>(users));
    }

    public async Task<Result<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<UserDto>>.Success(result.Map(_mapper.Map<UserDto>));
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken ct = default)
    {
        var exists = await _repository.ExistsAsync(u => u.email == dto.Email, ct);
        if (exists)
            return Result<UserDto>.ValidationError($"User with email {dto.Email} already exists");

        var user = new User
        {
            id = Guid.NewGuid().ToString(),
            email = dto.Email,
            name = dto.Name,
            role = dto.Role,
            userType = dto.UserType,
            isVerified = false,
            totalXP = 0,
            level = 1,
            tier = UserTier.BRONZE,
            trustScore = 0,
            verificationLevel = VerificationLevel.ANONYMOUS,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var invalid = UserProfileRules.ValidateProfileUpdate(dto);
        if (invalid is not null)
            return Result<UserDto>.ValidationError(invalid);

        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        if (dto.Name is not null) user.name = dto.Name.Trim();
        if (dto.Bio is not null) user.bio = dto.Bio;
        if (dto.Location is not null) user.location = dto.Location;
        if (dto.Phone is not null) user.phone = dto.Phone;
        if (dto.Website is not null) user.website = dto.Website.Trim();
        if (dto.LinkedIn is not null) user.linkedIn = dto.LinkedIn.Trim();
        if (dto.Twitter is not null) user.twitter = dto.Twitter.Trim();
        if (dto.Image is not null) user.image = dto.Image.Trim();
        user.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<bool>.NotFound($"User with id {id} not found");

        await _repository.DeleteAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _repository.FindOneAsync(u => u.email == email, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with email {email} not found");
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<PagedResult<UserDto>>> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default)
    {
        // Email is an exact-match lookup only, so a substring query cannot enumerate addresses.
        var lowerQuery = query.Trim().ToLower();
        var result = await _repository.GetPagedAsync(page, pageSize,
            u => u.name != null && u.name.ToLower().Contains(lowerQuery) ||
                 u.email.ToLower() == lowerQuery,
            ct);
        return Result<PagedResult<UserDto>>.Success(result.Map(_mapper.Map<UserDto>));
    }

    public async Task<Result<UserDto>> UpdateRoleAsync(string id, AdminUpdateUserRoleDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        user.role = dto.Role;
        user.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> UpdateUserTypeAsync(string id, AdminUpdateUserTypeDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        user.userType = dto.UserType;
        user.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> UpdateVerificationLevelAsync(string id, AdminUpdateVerificationLevelDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        user.verificationLevel = dto.VerificationLevel;
        user.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }
}

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AccountService(IRepository<Account> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AccountDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(id, ct);
        if (account is null)
            return Result<AccountDto>.NotFound($"Account with id {id} not found");
        return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
    }

    public async Task<Result<IReadOnlyList<AccountDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var accounts = await _repository.FindAsync(a => a.userId == userId, ct);
        return Result<IReadOnlyList<AccountDto>>.Success(_mapper.Map<IReadOnlyList<AccountDto>>(accounts));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(id, ct);
        if (account is null)
            return Result<bool>.NotFound($"Account with id {id} not found");

        await _repository.DeleteAsync(account, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class SessionService : ISessionService
{
    private readonly IRepository<Session> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SessionService(IRepository<Session> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<SessionDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var session = await _repository.GetByIdAsync(id, ct);
        if (session is null)
            return Result<SessionDto>.NotFound($"Session with id {id} not found");
        return Result<SessionDto>.Success(_mapper.Map<SessionDto>(session));
    }

    public async Task<Result<SessionDto>> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var session = await _repository.FindOneAsync(s => s.sessionToken == token, ct);
        if (session is null)
            return Result<SessionDto>.NotFound($"Session with token not found");
        return Result<SessionDto>.Success(_mapper.Map<SessionDto>(session));
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var sessions = await _repository.FindAsync(s => s.userId == userId, ct);
        return Result<IReadOnlyList<SessionDto>>.Success(_mapper.Map<IReadOnlyList<SessionDto>>(sessions));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var session = await _repository.GetByIdAsync(id, ct);
        if (session is null)
            return Result<bool>.NotFound($"Session with id {id} not found");

        await _repository.DeleteAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteExpiredAsync(CancellationToken ct = default)
    {
        var expired = await _repository.FindAsync(s => s.expires < DateTime.UtcNow, ct);
        await _repository.DeleteRangeAsync(expired, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class UserSkillService : IUserSkillService
{
    private readonly IRepository<UserSkill> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserSkillService(IRepository<UserSkill> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<UserSkillDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null)
            return Result<UserSkillDto>.NotFound($"Skill with id {id} not found");
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<IReadOnlyList<UserSkillDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var skills = await _repository.FindAsync(s => s.userId == userId, ct);
        return Result<IReadOnlyList<UserSkillDto>>.Success(_mapper.Map<IReadOnlyList<UserSkillDto>>(skills));
    }

    public async Task<Result<UserSkillDto>> CreateAsync(CreateUserSkillDto dto, CancellationToken ct = default)
    {
        var name = dto.Skill?.Trim() ?? string.Empty;
        var invalid = UserProfileRules.ValidateSkill(name, dto.Level);
        if (invalid is not null)
            return Result<UserSkillDto>.ValidationError(invalid);

        var existing = await _repository.FindAsync(s => s.userId == dto.UserId, ct);
        if (existing.Any(s => string.Equals(s.skill.Trim(), name, StringComparison.OrdinalIgnoreCase)))
            return Result<UserSkillDto>.ValidationError($"You already list {name} as a skill.");

        var skill = new UserSkill
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            skill = name,
            level = dto.Level
        };
        await _repository.AddAsync(skill, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<UserSkillDto>> UpdateAsync(string userId, string id, UpdateUserSkillDto dto, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null || skill.userId != userId)
            return Result<UserSkillDto>.NotFound($"Skill with id {id} not found");

        if (dto.Level.HasValue && !UserProfileRules.IsSkillLevel(dto.Level.Value))
            return Result<UserSkillDto>.ValidationError(UserProfileRules.SkillLevelMessage);

        if (dto.Level.HasValue) skill.level = dto.Level.Value;

        await _repository.UpdateAsync(skill, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<bool>> DeleteAsync(string userId, string id, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null || skill.userId != userId)
            return Result<bool>.NotFound($"Skill with id {id} not found");

        await _repository.DeleteAsync(skill, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class UserExperienceService : IUserExperienceService
{
    private readonly IRepository<UserExperience> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserExperienceService(IRepository<UserExperience> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<UserExperienceDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null)
            return Result<UserExperienceDto>.NotFound($"Experience with id {id} not found");
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<IReadOnlyList<UserExperienceDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var experiences = await _repository.FindAsync(e => e.userId == userId, ct);
        return Result<IReadOnlyList<UserExperienceDto>>.Success(_mapper.Map<IReadOnlyList<UserExperienceDto>>(experiences));
    }

    public async Task<Result<UserExperienceDto>> CreateAsync(CreateUserExperienceDto dto, CancellationToken ct = default)
    {
        var invalid = UserProfileRules.ValidateExperience(
            dto.Title, dto.Company, dto.Description, dto.StartDate, dto.EndDate, dto.IsCurrent);
        if (invalid is not null)
            return Result<UserExperienceDto>.ValidationError(invalid);

        var exp = new UserExperience
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            title = dto.Title.Trim(),
            company = dto.Company.Trim(),
            description = dto.Description,
            startDate = dto.StartDate,
            endDate = dto.EndDate,
            isCurrent = dto.IsCurrent
        };
        await _repository.AddAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<UserExperienceDto>> UpdateAsync(string userId, string id, UpdateUserExperienceDto dto, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null || exp.userId != userId)
            return Result<UserExperienceDto>.NotFound($"Experience with id {id} not found");

        var invalid = UserProfileRules.ValidateExperience(
            dto.Title ?? exp.title,
            dto.Company ?? exp.company,
            dto.Description ?? exp.description,
            dto.StartDate ?? exp.startDate,
            dto.EndDate ?? (dto.IsCurrent == true ? null : exp.endDate),
            dto.IsCurrent ?? exp.isCurrent);
        if (invalid is not null)
            return Result<UserExperienceDto>.ValidationError(invalid);

        if (dto.IsCurrent == true) exp.endDate = null;
        if (dto.Title is not null) exp.title = dto.Title;
        if (dto.Company is not null) exp.company = dto.Company;
        if (dto.Description is not null) exp.description = dto.Description;
        if (dto.StartDate.HasValue) exp.startDate = dto.StartDate.Value;
        if (dto.EndDate.HasValue) exp.endDate = dto.EndDate;
        if (dto.IsCurrent.HasValue) exp.isCurrent = dto.IsCurrent.Value;

        await _repository.UpdateAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<bool>> DeleteAsync(string userId, string id, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null || exp.userId != userId)
            return Result<bool>.NotFound($"Experience with id {id} not found");

        await _repository.DeleteAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

/// <summary>Profile field rules every caller (BFF, SDK, MCP) gets; see Services/AGENTS.md §Profile rules.</summary>
public static class UserProfileRules
{
    public const string SkillLevelMessage = "Skill level must be between 1 and 10.";

    private static readonly Regex SchemePrefix = new("^[A-Za-z][A-Za-z0-9+.-]*:", RegexOptions.Compiled);

    /// <summary>True when the level is on the 1-10 scale.</summary>
    public static bool IsSkillLevel(int level) => level is >= 1 and <= 10;

    /// <summary>Null when the skill is valid, else the reason.</summary>
    public static string? ValidateSkill(string trimmedName, int level)
    {
        if (trimmedName.Length == 0) return "Skill name is required.";
        if (trimmedName.Length > 100) return "Skill name must be 100 characters or fewer.";
        return IsSkillLevel(level) ? null : SkillLevelMessage;
    }

    /// <summary>Null when the experience entry is valid, else the reason.</summary>
    public static string? ValidateExperience(
        string? title, string? company, string? description, DateTime startDate, DateTime? endDate, bool isCurrent)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Role title is required.";
        if (title.Trim().Length > 200) return "Role title must be 200 characters or fewer.";
        if (string.IsNullOrWhiteSpace(company)) return "Organization is required.";
        if (company.Trim().Length > 200) return "Organization must be 200 characters or fewer.";
        if (description is { Length: > 2000 }) return "Description must be 2000 characters or fewer.";
        if (isCurrent && endDate.HasValue) return "A role you still hold has no end date.";
        if (endDate.HasValue && endDate.Value < startDate) return "The end date is before the start date.";
        return null;
    }

    /// <summary>Null when every supplied profile field is within limits and every link is http(s), else the reason.</summary>
    public static string? ValidateProfileUpdate(UpdateUserDto dto)
    {
        if (dto.Name is not null && dto.Name.Trim().Length == 0) return "Display name cannot be empty.";
        return TooLong(dto.Name, 100, "Display name")
            ?? TooLong(dto.Bio, 2000, "Bio")
            ?? TooLong(dto.Location, 200, "Location")
            ?? TooLong(dto.Phone, 30, "Phone")
            ?? TooLong(dto.Website, 500, "Website")
            ?? TooLong(dto.LinkedIn, 500, "LinkedIn")
            ?? TooLong(dto.Twitter, 100, "Twitter / X")
            ?? TooLong(dto.Image, 2048, "Profile image")
            ?? UnsafeLink(dto.Website, "Website", requireAbsolute: true)
            ?? UnsafeLink(dto.LinkedIn, "LinkedIn", requireAbsolute: false)
            ?? UnsafeLink(dto.Twitter, "Twitter / X", requireAbsolute: false)
            ?? UnsafeLink(dto.Image, "Profile image", requireAbsolute: true);
    }

    private static string? TooLong(string? value, int max, string label)
        => value is not null && value.Trim().Length > max ? $"{label} must be {max} characters or fewer." : null;

    // Scheme allowlist: anything that names a scheme must be http(s) with a host; a bare handle passes when requireAbsolute is false.
    private static string? UnsafeLink(string? value, string label, bool requireAbsolute)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Any(char.IsControl)) return $"{label} contains characters that are not allowed.";
        if (!requireAbsolute && !SchemePrefix.IsMatch(trimmed) && !trimmed.StartsWith("//", StringComparison.Ordinal))
            return null;

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
            && !string.IsNullOrEmpty(uri.Host)
                ? null
                : $"{label} must be an http:// or https:// link.";
    }
}
