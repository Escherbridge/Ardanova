namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class KycService : IKycService
{
    private readonly IRepository<KycSubmission> _submissionRepository;
    private readonly IRepository<KycDocument> _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IKycProviderService _providerService;
    private readonly IUserService _userService;

    public KycService(
        IRepository<KycSubmission> submissionRepository,
        IRepository<KycDocument> documentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IKycProviderService providerService,
        IUserService userService)
    {
        _submissionRepository = submissionRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _providerService = providerService;
        _userService = userService;
    }

    public async Task<Result<KycSubmissionDto>> SubmitAsync(SubmitKycDto dto, CancellationToken ct = default)
    {
        // Check no active submission (PENDING or IN_REVIEW) for this user
        var activeSubmissions = await _submissionRepository.FindAsync(
            s => s.userId == dto.UserId && (s.status == KycStatus.PENDING || s.status == KycStatus.IN_REVIEW), ct);

        if (activeSubmissions.Any())
            return Result<KycSubmissionDto>.ValidationError(
                "An active KYC submission already exists for this user. Please wait for the current submission to be reviewed.");

        // Validate documents via provider
        var validationResult = await _providerService.ValidateDocumentsAsync(dto.Documents, ct);
        if (validationResult.IsFailure)
            return Result<KycSubmissionDto>.ValidationError(validationResult.Error!);

        // Create submission
        var submission = new KycSubmission
        {
            id = Guid.NewGuid().ToString(),
            userId = dto.UserId,
            provider = KycProvider.MANUAL,
            status = KycStatus.PENDING,
            submittedAt = DateTime.UtcNow,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddAsync(submission, ct);

        // Create document records
        var documents = new List<KycDocument>();
        foreach (var docDto in dto.Documents)
        {
            var document = new KycDocument
            {
                id = Guid.NewGuid().ToString(),
                submissionId = submission.id,
                type = docDto.Type,
                fileUrl = docDto.FileUrl,
                fileName = docDto.FileName,
                mimeType = docDto.MimeType,
                fileSizeBytes = docDto.FileSizeBytes,
                metadata = docDto.Metadata,
                createdAt = DateTime.UtcNow
            };
            documents.Add(document);
        }

        await _documentRepository.AddRangeAsync(documents, ct);

        // Call provider to create session
        var documentDtos = documents.Select(d => _mapper.Map<KycDocumentDto>(d)).ToList();
        var sessionResult = await _providerService.CreateSessionAsync(dto.UserId, documentDtos, ct);
        if (sessionResult.IsSuccess && !string.IsNullOrEmpty(sessionResult.Value))
        {
            submission.providerSessionId = sessionResult.Value;
            await _submissionRepository.UpdateAsync(submission, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Map and return
        var submissionDto = _mapper.Map<KycSubmissionDto>(submission);
        // Manually attach document DTOs since the navigation property may not be loaded
        submissionDto = submissionDto with { Documents = documentDtos };
        return Result<KycSubmissionDto>.Success(submissionDto);
    }

    public async Task<Result<KycSubmissionDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, ct);
        if (submission is null)
            return Result<KycSubmissionDto>.NotFound($"KYC submission with id {id} not found");

        var documents = await _documentRepository.FindAsync(d => d.submissionId == id, ct);
        var submissionDto = _mapper.Map<KycSubmissionDto>(submission);
        submissionDto = submissionDto with { Documents = _mapper.Map<List<KycDocumentDto>>(documents) };
        return Result<KycSubmissionDto>.Success(submissionDto);
    }

    public async Task<Result<KycSubmissionDto>> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        // Get most recent submission for user (ordered by submittedAt descending)
        var submissions = await _submissionRepository.FindAsync(s => s.userId == userId, ct);
        var submission = submissions.OrderByDescending(s => s.submittedAt).FirstOrDefault();

        if (submission is null)
            return Result<KycSubmissionDto>.NotFound($"No KYC submission found for user {userId}");

        var documents = await _documentRepository.FindAsync(d => d.submissionId == submission.id, ct);
        var submissionDto = _mapper.Map<KycSubmissionDto>(submission);
        submissionDto = submissionDto with { Documents = _mapper.Map<List<KycDocumentDto>>(documents) };
        return Result<KycSubmissionDto>.Success(submissionDto);
    }

    public async Task<Result<List<KycSubmissionDto>>> GetPendingAsync(CancellationToken ct = default)
    {
        var submissions = await _submissionRepository.FindAsync(
            s => s.status == KycStatus.PENDING || s.status == KycStatus.IN_REVIEW, ct);

        var dtos = new List<KycSubmissionDto>();
        foreach (var submission in submissions)
        {
            var documents = await _documentRepository.FindAsync(d => d.submissionId == submission.id, ct);
            var dto = _mapper.Map<KycSubmissionDto>(submission);
            dto = dto with { Documents = _mapper.Map<List<KycDocumentDto>>(documents) };
            dtos.Add(dto);
        }

        return Result<List<KycSubmissionDto>>.Success(dtos);
    }

    public async Task<Result<KycSubmissionDto>> ApproveAsync(string id, string reviewerId, string? notes, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, ct);
        if (submission is null)
            return Result<KycSubmissionDto>.NotFound($"KYC submission with id {id} not found");

        if (submission.status != KycStatus.PENDING && submission.status != KycStatus.IN_REVIEW)
            return Result<KycSubmissionDto>.ValidationError(
                $"Cannot approve a submission with status {submission.status}. Only PENDING or IN_REVIEW submissions can be approved.");

        submission.status = KycStatus.APPROVED;
        submission.reviewerId = reviewerId;
        submission.reviewNotes = notes;
        submission.reviewedAt = DateTime.UtcNow;
        submission.updatedAt = DateTime.UtcNow;

        await _submissionRepository.UpdateAsync(submission, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Auto-upgrade user to PRO
        await _userService.UpdateVerificationLevelAsync(submission.userId,
            new AdminUpdateVerificationLevelDto { VerificationLevel = VerificationLevel.PRO }, ct);

        var documents = await _documentRepository.FindAsync(d => d.submissionId == id, ct);
        var submissionDto = _mapper.Map<KycSubmissionDto>(submission);
        submissionDto = submissionDto with { Documents = _mapper.Map<List<KycDocumentDto>>(documents) };
        return Result<KycSubmissionDto>.Success(submissionDto);
    }

    public async Task<Result<KycSubmissionDto>> RejectAsync(string id, string reviewerId, string? notes, string? rejectionReason, CancellationToken ct = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, ct);
        if (submission is null)
            return Result<KycSubmissionDto>.NotFound($"KYC submission with id {id} not found");

        if (submission.status != KycStatus.PENDING && submission.status != KycStatus.IN_REVIEW)
            return Result<KycSubmissionDto>.ValidationError(
                $"Cannot reject a submission with status {submission.status}. Only PENDING or IN_REVIEW submissions can be rejected.");

        submission.status = KycStatus.REJECTED;
        submission.reviewerId = reviewerId;
        submission.reviewNotes = notes;
        submission.rejectionReason = rejectionReason;
        submission.reviewedAt = DateTime.UtcNow;
        submission.updatedAt = DateTime.UtcNow;

        await _submissionRepository.UpdateAsync(submission, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var documents = await _documentRepository.FindAsync(d => d.submissionId == id, ct);
        var submissionDto = _mapper.Map<KycSubmissionDto>(submission);
        submissionDto = submissionDto with { Documents = _mapper.Map<List<KycDocumentDto>>(documents) };
        return Result<KycSubmissionDto>.Success(submissionDto);
    }
}
