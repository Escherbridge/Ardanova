namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class RoadmapService : IRoadmapService
{
    private readonly IRepository<Roadmap> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoadmapService(IRepository<Roadmap> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<RoadmapDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var roadmap = await _repository.GetByIdAsync(id, ct);
        if (roadmap is null)
            return Result<RoadmapDto>.NotFound($"Roadmap with id {id} not found");
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }

    public async Task<Result<RoadmapDto>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
    {
        var roadmap = await _repository.FindOneAsync(r => r.projectId == projectId, ct);
        if (roadmap is null)
            return Result<RoadmapDto>.NotFound($"Roadmap for project {projectId} not found");
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }

    public async Task<Result<RoadmapDto>> CreateAsync(CreateRoadmapDto dto, CancellationToken ct = default)
    {
        var roadmap = new Roadmap
        {
            id = Guid.NewGuid().ToString(),
            projectId = dto.ProjectId,
            title = dto.Title,
            vision = dto.Vision,
            assigneeId = dto.AssigneeId,
            status = RoadmapStatus.DRAFT,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(roadmap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }

    public async Task<Result<RoadmapDto>> UpdateAsync(string id, UpdateRoadmapDto dto, CancellationToken ct = default)
    {
        var roadmap = await _repository.GetByIdAsync(id, ct);
        if (roadmap is null)
            return Result<RoadmapDto>.NotFound($"Roadmap with id {id} not found");

        if (dto.Title is not null) roadmap.title = dto.Title;
        if (dto.Vision is not null) roadmap.vision = dto.Vision;
        if (dto.Status.HasValue) roadmap.status = dto.Status.Value;
        if (dto.AssigneeId is not null) roadmap.assigneeId = dto.AssigneeId;
        roadmap.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(roadmap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var roadmap = await _repository.GetByIdAsync(id, ct);
        if (roadmap is null)
            return Result<bool>.NotFound($"Roadmap with id {id} not found");

        await _repository.DeleteAsync(roadmap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<RoadmapDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var roadmap = await _repository.GetByIdAsync(id, ct);
        if (roadmap is null)
            return Result<RoadmapDto>.NotFound($"Roadmap with id {id} not found");

        roadmap.assigneeId = userId;
        roadmap.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(roadmap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }

    public async Task<Result<RoadmapDto>> UpdateStatusAsync(string id, RoadmapStatus status, CancellationToken ct = default)
    {
        var roadmap = await _repository.GetByIdAsync(id, ct);
        if (roadmap is null)
            return Result<RoadmapDto>.NotFound($"Roadmap with id {id} not found");

        roadmap.status = status;
        roadmap.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(roadmap, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapDto>.Success(_mapper.Map<RoadmapDto>(roadmap));
    }
}

public class RoadmapPhaseService : IRoadmapPhaseService
{
    private readonly IRepository<RoadmapPhase> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RoadmapPhaseService(IRepository<RoadmapPhase> repository, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<RoadmapPhaseDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var phase = await _repository.GetByIdAsync(id, ct);
        if (phase is null)
            return Result<RoadmapPhaseDto>.NotFound($"Phase with id {id} not found");
        return Result<RoadmapPhaseDto>.Success(_mapper.Map<RoadmapPhaseDto>(phase));
    }

    public async Task<Result<IReadOnlyList<RoadmapPhaseDto>>> GetByRoadmapIdAsync(string roadmapId, CancellationToken ct = default)
    {
        var phases = await _repository.FindAsync(p => p.roadmapId == roadmapId, ct);
        var orderedPhases = phases.OrderBy(p => p.order).ToList();
        return Result<IReadOnlyList<RoadmapPhaseDto>>.Success(_mapper.Map<IReadOnlyList<RoadmapPhaseDto>>(orderedPhases));
    }

    public async Task<Result<RoadmapPhaseDto>> CreateAsync(CreateRoadmapPhaseDto dto, CancellationToken ct = default)
    {
        var phase = new RoadmapPhase
        {
            id = Guid.NewGuid().ToString(),
            roadmapId = dto.RoadmapId,
            name = dto.Name,
            description = dto.Description,
            order = dto.Order,
            startDate = dto.StartDate,
            endDate = dto.EndDate,
            status = PhaseStatus.PLANNED,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(phase, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapPhaseDto>.Success(_mapper.Map<RoadmapPhaseDto>(phase));
    }

    public async Task<Result<RoadmapPhaseDto>> UpdateAsync(string id, UpdateRoadmapPhaseDto dto, CancellationToken ct = default)
    {
        var phase = await _repository.GetByIdAsync(id, ct);
        if (phase is null)
            return Result<RoadmapPhaseDto>.NotFound($"Phase with id {id} not found");

        if (dto.Name is not null) phase.name = dto.Name;
        if (dto.Description is not null) phase.description = dto.Description;
        if (dto.Status.HasValue) phase.status = dto.Status.Value;
        if (dto.Order.HasValue) phase.order = dto.Order.Value;
        if (dto.StartDate.HasValue) phase.startDate = dto.StartDate;
        if (dto.EndDate.HasValue) phase.endDate = dto.EndDate;
        phase.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(phase, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapPhaseDto>.Success(_mapper.Map<RoadmapPhaseDto>(phase));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var phase = await _repository.GetByIdAsync(id, ct);
        if (phase is null)
            return Result<bool>.NotFound($"Phase with id {id} not found");

        await _repository.DeleteAsync(phase, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<RoadmapPhaseDto>> AssignAsync(string id, string? userId, CancellationToken ct = default)
    {
        var phase = await _repository.GetByIdAsync(id, ct);
        if (phase is null)
            return Result<RoadmapPhaseDto>.NotFound($"Phase with id {id} not found");

        phase.assigneeId = userId;
        phase.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(phase, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapPhaseDto>.Success(_mapper.Map<RoadmapPhaseDto>(phase));
    }

    public async Task<Result<RoadmapPhaseDto>> UpdateStatusAsync(string id, PhaseStatus status, CancellationToken ct = default)
    {
        var phase = await _repository.GetByIdAsync(id, ct);
        if (phase is null)
            return Result<RoadmapPhaseDto>.NotFound($"Phase with id {id} not found");

        phase.status = status;
        phase.updatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(phase, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<RoadmapPhaseDto>.Success(_mapper.Map<RoadmapPhaseDto>(phase));
    }

    public async Task<Result<bool>> ReorderAsync(string roadmapId, IReadOnlyList<string> phaseIds, CancellationToken ct = default)
    {
        var phases = await _repository.FindAsync(p => p.roadmapId == roadmapId, ct);
        var phaseDict = phases.ToDictionary(p => p.id);

        for (int i = 0; i < phaseIds.Count; i++)
        {
            if (phaseDict.TryGetValue(phaseIds[i], out var phase))
            {
                phase.order = i;
                phase.updatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(phase, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
