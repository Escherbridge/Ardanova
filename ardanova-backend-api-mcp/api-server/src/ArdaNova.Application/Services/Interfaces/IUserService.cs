namespace ArdaNova.Application.Services.Interfaces;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;

public interface IUserService
{
    Task<Result<UserDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserDto>>> GetAllAsync(CancellationToken ct = default);
    Task<Result<PagedResult<UserDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<Result<UserDto>> CreateAsync(CreateUserDto dto, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateAsync(string id, UpdateUserDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Result<UserDto>> VerifyAsync(string id, CancellationToken ct = default);
    Task<Result<PagedResult<UserDto>>> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateRoleAsync(string id, AdminUpdateUserRoleDto dto, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateUserTypeAsync(string id, AdminUpdateUserTypeDto dto, CancellationToken ct = default);
    Task<Result<UserDto>> UpdateVerificationLevelAsync(string id, AdminUpdateVerificationLevelDto dto, CancellationToken ct = default);
}

public interface IAccountService
{
    Task<Result<AccountDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<AccountDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface ISessionService
{
    Task<Result<SessionDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<SessionDto>> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SessionDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
    Task<Result<bool>> DeleteExpiredAsync(CancellationToken ct = default);
}

public interface IUserSkillService
{
    Task<Result<UserSkillDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserSkillDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<UserSkillDto>> CreateAsync(CreateUserSkillDto dto, CancellationToken ct = default);
    Task<Result<UserSkillDto>> UpdateAsync(string id, UpdateUserSkillDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IUserExperienceService
{
    Task<Result<UserExperienceDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<UserExperienceDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<Result<UserExperienceDto>> CreateAsync(CreateUserExperienceDto dto, CancellationToken ct = default);
    Task<Result<UserExperienceDto>> UpdateAsync(string id, UpdateUserExperienceDto dto, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default);
}
