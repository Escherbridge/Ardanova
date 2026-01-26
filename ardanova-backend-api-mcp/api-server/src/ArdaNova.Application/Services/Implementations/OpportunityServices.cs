namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class OpportunityService : IOpportunityService
{
    private readonly IRepository<Opportunity> _repository;
    private readonly IRepository<OpportunityApplication> _applicationRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public OpportunityService(
        IRepository<Opportunity> repository,
        IRepository<OpportunityApplication> applicationRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _applicationRepository = applicationRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            + "-" + Guid.NewGuid().ToString("N")[..8];
    }

    public async Task<Result<OpportunityDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Result<OpportunityDto>.NotFound($"Opportunity with id {id} not found");

        var dto = await EnrichOpportunityDtoAsync(opportunity, ct);
        return Result<OpportunityDto>.Success(dto);
    }

    public async Task<Result<OpportunityDto>> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var opportunity = await _repository.FindOneAsync(o => o.slug == slug, ct);
        if (opportunity is null)
            return Result<OpportunityDto>.NotFound($"Opportunity with slug {slug} not found");

        var dto = await EnrichOpportunityDtoAsync(opportunity, ct);
        return Result<OpportunityDto>.Success(dto);
    }

    public async Task<Result<IReadOnlyList<OpportunityDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var opportunities = await _repository.GetAllAsync(ct);
        var dtos = await EnrichOpportunityDtosAsync(opportunities, ct);
        return Result<IReadOnlyList<OpportunityDto>>.Success(dtos);
    }

    public async Task<Result<PagedResult<OpportunityDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, null, ct);
        var dtos = await EnrichOpportunityDtosAsync(result.Items, ct);
        return Result<PagedResult<OpportunityDto>>.Success(new PagedResult<OpportunityDto>(dtos.ToList(), result.TotalCount, result.Page, result.PageSize));
    }

    public async Task<Result<PagedResult<OpportunityDto>>> SearchAsync(
        string? searchTerm,
        OpportunityType? type,
        OpportunityStatus? status,
        ExperienceLevel? experienceLevel,
        string? skills,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _repository.Query();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(o => o.title.ToLower().Contains(term) ||
                o.description.ToLower().Contains(term));
        }

        if (type.HasValue)
            query = query.Where(o => o.type == type.Value);

        if (status.HasValue)
            query = query.Where(o => o.status == status.Value);

        if (experienceLevel.HasValue)
            query = query.Where(o => o.experienceLevel == experienceLevel.Value);

        if (!string.IsNullOrWhiteSpace(skills))
        {
            var skillTerm = skills.ToLower();
            query = query.Where(o => o.skills != null && o.skills.ToLower().Contains(skillTerm));
        }

        query = query.OrderByDescending(o => o.createdAt);

        var totalCount = query.Count();
        var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var dtos = await EnrichOpportunityDtosAsync(items, ct);

        return Result<PagedResult<OpportunityDto>>.Success(new PagedResult<OpportunityDto>(dtos.ToList(), totalCount, page, pageSize));
    }

    public async Task<Result<IReadOnlyList<OpportunityDto>>> GetByPosterIdAsync(string posterId, CancellationToken ct = default)
    {
        var opportunities = await _repository.FindAsync(o => o.posterId == posterId, ct);
        var dtos = await EnrichOpportunityDtosAsync(opportunities, ct);
        return Result<IReadOnlyList<OpportunityDto>>.Success(dtos);
    }

    public async Task<Result<OpportunityDto>> CreateAsync(CreateOpportunityDto dto, CancellationToken ct = default)
    {
        var opportunity = new Opportunity
        {
            id = Guid.NewGuid().ToString(),
            title = dto.Title,
            slug = GenerateSlug(dto.Title),
            description = dto.Description,
            type = dto.Type,
            status = OpportunityStatus.DRAFT,
            experienceLevel = dto.ExperienceLevel,
            requirements = dto.Requirements,
            skills = dto.Skills,
            benefits = dto.Benefits,
            location = dto.Location,
            isRemote = dto.IsRemote,
            compensation = dto.Compensation,
            compensationDetails = dto.CompensationDetails,
            deadline = dto.Deadline,
            maxApplications = dto.MaxApplications,
            applicationsCount = 0,
            coverImage = dto.CoverImage,
            posterId = dto.PosterId,
            guildId = dto.GuildId,
            projectId = dto.ProjectId,
            taskId = dto.TaskId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(opportunity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichOpportunityDtoAsync(opportunity, ct);
        return Result<OpportunityDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityDto>> UpdateAsync(string id, UpdateOpportunityDto dto, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Result<OpportunityDto>.NotFound($"Opportunity with id {id} not found");

        if (dto.Title is not null) opportunity.title = dto.Title;
        if (dto.Description is not null) opportunity.description = dto.Description;
        if (dto.Type.HasValue) opportunity.type = dto.Type.Value;
        if (dto.Status.HasValue) opportunity.status = dto.Status.Value;
        if (dto.ExperienceLevel.HasValue) opportunity.experienceLevel = dto.ExperienceLevel.Value;
        if (dto.Requirements is not null) opportunity.requirements = dto.Requirements;
        if (dto.Skills is not null) opportunity.skills = dto.Skills;
        if (dto.Benefits is not null) opportunity.benefits = dto.Benefits;
        if (dto.Location is not null) opportunity.location = dto.Location;
        if (dto.IsRemote.HasValue) opportunity.isRemote = dto.IsRemote.Value;
        if (dto.Compensation.HasValue) opportunity.compensation = dto.Compensation;
        if (dto.CompensationDetails is not null) opportunity.compensationDetails = dto.CompensationDetails;
        if (dto.Deadline.HasValue) opportunity.deadline = dto.Deadline;
        if (dto.MaxApplications.HasValue) opportunity.maxApplications = dto.MaxApplications;
        if (dto.CoverImage is not null) opportunity.coverImage = dto.CoverImage;
        opportunity.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(opportunity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichOpportunityDtoAsync(opportunity, ct);
        return Result<OpportunityDto>.Success(resultDto);
    }

    public async Task<Result<OpportunityDto>> CloseAsync(string id, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Result<OpportunityDto>.NotFound($"Opportunity with id {id} not found");

        opportunity.status = OpportunityStatus.CLOSED;
        opportunity.closedAt = DateTime.UtcNow;
        opportunity.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(opportunity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var resultDto = await EnrichOpportunityDtoAsync(opportunity, ct);
        return Result<OpportunityDto>.Success(resultDto);
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(id, ct);
        if (opportunity is null)
            return Result<bool>.NotFound($"Opportunity with id {id} not found");

        await _repository.DeleteAsync(opportunity, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<OpportunityApplicationDto>> ApplyAsync(string opportunityId, ApplyToOpportunityDto dto, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(opportunityId, ct);
        if (opportunity is null)
            return Result<OpportunityApplicationDto>.NotFound($"Opportunity with id {opportunityId} not found");

        if (opportunity.status != OpportunityStatus.OPEN)
            return Result<OpportunityApplicationDto>.ValidationError("Opportunity is not open for applications");

        var existingApplication = await _applicationRepository.FindOneAsync(
            a => a.opportunityId == opportunityId && a.applicantId == dto.ApplicantId, ct);
        if (existingApplication is not null)
            return Result<OpportunityApplicationDto>.ValidationError("User has already applied to this opportunity");

        if (opportunity.maxApplications.HasValue && opportunity.applicationsCount >= opportunity.maxApplications.Value)
            return Result<OpportunityApplicationDto>.ValidationError("Opportunity has reached maximum applications");

        var application = new OpportunityApplication
        {
            id = Guid.NewGuid().ToString(),
            opportunityId = opportunityId,
            applicantId = dto.ApplicantId,
            coverLetter = dto.CoverLetter,
            portfolio = dto.Portfolio,
            additionalInfo = dto.AdditionalInfo,
            status = ApplicationStatus.PENDING,
            appliedAt = DateTime.UtcNow
        };

        await _applicationRepository.AddAsync(application, ct);

        opportunity.applicationsCount++;
        await _repository.UpdateAsync(opportunity, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var applicationDto = _mapper.Map<OpportunityApplicationDto>(application);
        var applicant = await _userRepository.GetByIdAsync(dto.ApplicantId, ct);
        if (applicant is not null)
            applicationDto = applicationDto with { Applicant = _mapper.Map<OpportunityApplicationApplicantDto>(applicant) };

        return Result<OpportunityApplicationDto>.Success(applicationDto);
    }

    public async Task<Result<IReadOnlyList<OpportunityApplicationDto>>> GetApplicationsAsync(string opportunityId, CancellationToken ct = default)
    {
        var opportunity = await _repository.GetByIdAsync(opportunityId, ct);
        if (opportunity is null)
            return Result<IReadOnlyList<OpportunityApplicationDto>>.NotFound($"Opportunity with id {opportunityId} not found");

        var applications = await _applicationRepository.FindAsync(a => a.opportunityId == opportunityId, ct);
        var dtos = new List<OpportunityApplicationDto>();

        foreach (var application in applications)
        {
            var dto = _mapper.Map<OpportunityApplicationDto>(application);
            var applicant = await _userRepository.GetByIdAsync(application.applicantId, ct);
            if (applicant is not null)
                dto = dto with { Applicant = _mapper.Map<OpportunityApplicationApplicantDto>(applicant) };
            dtos.Add(dto);
        }

        return Result<IReadOnlyList<OpportunityApplicationDto>>.Success(dtos);
    }

    public async Task<Result<OpportunityApplicationDto>> UpdateApplicationStatusAsync(string applicationId, UpdateApplicationStatusDto dto, CancellationToken ct = default)
    {
        var application = await _applicationRepository.GetByIdAsync(applicationId, ct);
        if (application is null)
            return Result<OpportunityApplicationDto>.NotFound($"Application with id {applicationId} not found");

        application.status = dto.Status;
        application.reviewNotes = dto.ReviewNotes;
        application.reviewedAt = DateTime.UtcNow;

        await _applicationRepository.UpdateAsync(application, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var applicationDto = _mapper.Map<OpportunityApplicationDto>(application);
        var applicant = await _userRepository.GetByIdAsync(application.applicantId, ct);
        if (applicant is not null)
            applicationDto = applicationDto with { Applicant = _mapper.Map<OpportunityApplicationApplicantDto>(applicant) };

        return Result<OpportunityApplicationDto>.Success(applicationDto);
    }

    private async Task<OpportunityDto> EnrichOpportunityDtoAsync(Opportunity opportunity, CancellationToken ct)
    {
        var dto = _mapper.Map<OpportunityDto>(opportunity);

        var poster = await _userRepository.GetByIdAsync(opportunity.posterId, ct);
        if (poster is not null)
            dto = dto with { Poster = _mapper.Map<OpportunityPosterDto>(poster) };

        return dto;
    }

    private async Task<IReadOnlyList<OpportunityDto>> EnrichOpportunityDtosAsync(IEnumerable<Opportunity> opportunities, CancellationToken ct)
    {
        var dtos = new List<OpportunityDto>();
        foreach (var opportunity in opportunities)
        {
            dtos.Add(await EnrichOpportunityDtoAsync(opportunity, ct));
        }
        return dtos;
    }
}
