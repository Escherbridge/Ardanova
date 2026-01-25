namespace ArdaNova.Application.Services.Implementations;

using AutoMapper;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;

/// <summary>
/// Service for managing attachment database records
/// </summary>
public class AttachmentService : IAttachmentService
{
    private readonly IRepository<Attachment> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AttachmentService(
        IRepository<Attachment> repository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<AttachmentDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var attachment = await _repository.GetByIdAsync(id, ct);
        if (attachment == null)
        {
            return Result<AttachmentDto>.NotFound($"Attachment with id '{id}' not found");
        }

        return Result<AttachmentDto>.Success(_mapper.Map<AttachmentDto>(attachment));
    }

    public async Task<Result<IReadOnlyList<AttachmentDto>>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        var attachments = await _repository.FindAsync(a => a.uploadedById == userId, ct);
        return Result<IReadOnlyList<AttachmentDto>>.Success(
            _mapper.Map<IReadOnlyList<AttachmentDto>>(attachments));
    }

    public async Task<Result<PagedResult<AttachmentDto>>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var pagedResult = await _repository.GetPagedAsync(page, pageSize, null, ct);
        return Result<PagedResult<AttachmentDto>>.Success(
            pagedResult.Map(_mapper.Map<AttachmentDto>));
    }

    public async Task<Result<AttachmentDto>> CreateAsync(CreateAttachmentDto dto, CancellationToken ct = default)
    {
        var attachment = new Attachment
        {
            id = GenerateId(),
            uploadedById = dto.UploadedById,
            bucketPath = dto.BucketPath,
            type = dto.Type,
            createdAt = DateTime.UtcNow,
            lastUsedAt = null
        };

        await _repository.AddAsync(attachment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AttachmentDto>.Success(_mapper.Map<AttachmentDto>(attachment));
    }

    public async Task<Result<AttachmentDto>> UpdateLastUsedAsync(string id, CancellationToken ct = default)
    {
        var attachment = await _repository.GetByIdAsync(id, ct);
        if (attachment == null)
        {
            return Result<AttachmentDto>.NotFound($"Attachment with id '{id}' not found");
        }

        attachment.lastUsedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(attachment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<AttachmentDto>.Success(_mapper.Map<AttachmentDto>(attachment));
    }

    public async Task<Result<bool>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var attachment = await _repository.GetByIdAsync(id, ct);
        if (attachment == null)
        {
            return Result<bool>.NotFound($"Attachment with id '{id}' not found");
        }

        await _repository.DeleteAsync(attachment, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<AttachmentDto>>> GetByTypeAsync(MimeType type, CancellationToken ct = default)
    {
        var attachments = await _repository.FindAsync(a => a.type == type, ct);
        return Result<IReadOnlyList<AttachmentDto>>.Success(
            _mapper.Map<IReadOnlyList<AttachmentDto>>(attachments));
    }

    public async Task<Result<AttachmentDto>> GetByBucketPathAsync(string bucketPath, CancellationToken ct = default)
    {
        var attachment = await _repository.FindOneAsync(a => a.bucketPath == bucketPath, ct);
        if (attachment == null)
        {
            return Result<AttachmentDto>.NotFound($"Attachment with bucket path '{bucketPath}' not found");
        }

        return Result<AttachmentDto>.Success(_mapper.Map<AttachmentDto>(attachment));
    }

    private static string GenerateId()
    {
        // Generate a CUID-like ID (matches Prisma's default ID generation)
        return Guid.NewGuid().ToString("N")[..25];
    }
}
