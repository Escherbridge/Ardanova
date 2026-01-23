namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;

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

    public async Task<Result<UserDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
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
        var exists = await _repository.ExistsAsync(u => u.Email == dto.Email, ct);
        if (exists)
            return Result<UserDto>.ValidationError($"User with email {dto.Email} already exists");

        var user = User.Create(dto.Email, dto.Role, dto.UserType, dto.Name);
        await _repository.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserDto dto, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        user.UpdateProfile(dto.Name, dto.Bio, dto.Location, dto.Phone, dto.Website);
        user.UpdateSocialLinks(dto.LinkedIn, dto.Twitter);
        if (dto.Image is not null) user.SetImage(dto.Image);

        await _repository.UpdateAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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
        var user = await _repository.FindOneAsync(u => u.Email == email, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with email {email} not found");
        return Result<UserDto>.Success(_mapper.Map<UserDto>(user));
    }

    public async Task<Result<UserDto>> VerifyAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _repository.GetByIdAsync(id, ct);
        if (user is null)
            return Result<UserDto>.NotFound($"User with id {id} not found");

        user.Verify();
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

    public async Task<Result<AccountDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _repository.GetByIdAsync(id, ct);
        if (account is null)
            return Result<AccountDto>.NotFound($"Account with id {id} not found");
        return Result<AccountDto>.Success(_mapper.Map<AccountDto>(account));
    }

    public async Task<Result<IReadOnlyList<AccountDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var accounts = await _repository.FindAsync(a => a.UserId == userId, ct);
        return Result<IReadOnlyList<AccountDto>>.Success(_mapper.Map<IReadOnlyList<AccountDto>>(accounts));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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

    public async Task<Result<SessionDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var session = await _repository.GetByIdAsync(id, ct);
        if (session is null)
            return Result<SessionDto>.NotFound($"Session with id {id} not found");
        return Result<SessionDto>.Success(_mapper.Map<SessionDto>(session));
    }

    public async Task<Result<SessionDto>> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var session = await _repository.FindOneAsync(s => s.SessionToken == token, ct);
        if (session is null)
            return Result<SessionDto>.NotFound($"Session with token not found");
        return Result<SessionDto>.Success(_mapper.Map<SessionDto>(session));
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _repository.FindAsync(s => s.UserId == userId, ct);
        return Result<IReadOnlyList<SessionDto>>.Success(_mapper.Map<IReadOnlyList<SessionDto>>(sessions));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
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
        var expired = await _repository.FindAsync(s => s.Expires < DateTime.UtcNow, ct);
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

    public async Task<Result<UserSkillDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null)
            return Result<UserSkillDto>.NotFound($"Skill with id {id} not found");
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<IReadOnlyList<UserSkillDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var skills = await _repository.FindAsync(s => s.UserId == userId, ct);
        return Result<IReadOnlyList<UserSkillDto>>.Success(_mapper.Map<IReadOnlyList<UserSkillDto>>(skills));
    }

    public async Task<Result<UserSkillDto>> CreateAsync(CreateUserSkillDto dto, CancellationToken ct = default)
    {
        var skill = UserSkill.Create(dto.UserId, dto.Skill, dto.Level);
        await _repository.AddAsync(skill, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<UserSkillDto>> UpdateAsync(Guid id, UpdateUserSkillDto dto, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null)
            return Result<UserSkillDto>.NotFound($"Skill with id {id} not found");

        skill.UpdateLevel(dto.Level);
        await _repository.UpdateAsync(skill, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserSkillDto>.Success(_mapper.Map<UserSkillDto>(skill));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var skill = await _repository.GetByIdAsync(id, ct);
        if (skill is null)
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

    public async Task<Result<UserExperienceDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null)
            return Result<UserExperienceDto>.NotFound($"Experience with id {id} not found");
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<IReadOnlyList<UserExperienceDto>>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var experiences = await _repository.FindAsync(e => e.UserId == userId, ct);
        return Result<IReadOnlyList<UserExperienceDto>>.Success(_mapper.Map<IReadOnlyList<UserExperienceDto>>(experiences));
    }

    public async Task<Result<UserExperienceDto>> CreateAsync(CreateUserExperienceDto dto, CancellationToken ct = default)
    {
        var exp = UserExperience.Create(dto.UserId, dto.Title, dto.Company, dto.StartDate, dto.Description, dto.EndDate, dto.IsCurrent);
        await _repository.AddAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<UserExperienceDto>> UpdateAsync(Guid id, UpdateUserExperienceDto dto, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null)
            return Result<UserExperienceDto>.NotFound($"Experience with id {id} not found");

        exp.Update(
            dto.Title ?? exp.Title,
            dto.Company ?? exp.Company,
            dto.Description,
            dto.StartDate ?? exp.StartDate,
            dto.EndDate,
            dto.IsCurrent ?? exp.IsCurrent
        );
        await _repository.UpdateAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<UserExperienceDto>.Success(_mapper.Map<UserExperienceDto>(exp));
    }

    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var exp = await _repository.GetByIdAsync(id, ct);
        if (exp is null)
            return Result<bool>.NotFound($"Experience with id {id} not found");

        await _repository.DeleteAsync(exp, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
