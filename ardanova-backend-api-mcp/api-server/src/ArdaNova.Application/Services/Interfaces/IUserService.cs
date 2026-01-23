namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Result<UserDto>> VerifyAsync(Guid id, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<Result<AccountDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AccountDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface ISessionService
{
    Task<Result<SessionDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<SessionDto>> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SessionDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Result<bool>> DeleteExpiredAsync(CancellationToken ct = default);
}

public interface IUserSkillService
{
    Task<Result<UserSkillDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserSkillDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserSkillDto>> CreateAsync(CreateUserSkillDto dto, CancellationToken ct = default);
    Task<Result<UserSkillDto>> UpdateAsync(Guid id, UpdateUserSkillDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IUserExperienceService
{
    Task<Result<UserExperienceDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserExperienceDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserExperienceDto>> CreateAsync(CreateUserExperienceDto dto, CancellationToken ct = default);
    Task<Result<UserExperienceDto>> UpdateAsync(Guid id, UpdateUserExperienceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
}
