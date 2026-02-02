namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

public class OpportunityServiceTests
{
    private readonly Mock<IRepository<Opportunity>> _repositoryMock;
    private readonly Mock<IRepository<OpportunityApplication>> _applicationRepositoryMock;
    private readonly Mock<IRepository<OpportunityUpdate>> _updateRepositoryMock;
    private readonly Mock<IRepository<OpportunityComment>> _commentRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IRepository<Guild>> _guildRepositoryMock;
    private readonly Mock<IRepository<Project>> _projectRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly OpportunityService _sut;

    public OpportunityServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Opportunity>>();
        _applicationRepositoryMock = new Mock<IRepository<OpportunityApplication>>();
        _updateRepositoryMock = new Mock<IRepository<OpportunityUpdate>>();
        _commentRepositoryMock = new Mock<IRepository<OpportunityComment>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _guildRepositoryMock = new Mock<IRepository<Guild>>();
        _projectRepositoryMock = new Mock<IRepository<Project>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new OpportunityService(
            _repositoryMock.Object,
            _applicationRepositoryMock.Object,
            _updateRepositoryMock.Object,
            _commentRepositoryMock.Object,
            _userRepositoryMock.Object,
            _guildRepositoryMock.Object,
            _projectRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOpportunityExists_ReturnsSuccessResult()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var posterId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "A test opportunity",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            posterId = posterId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var poster = new User { id = posterId, name = "Test Poster" };
        var opportunityDto = new OpportunityDto { Id = opportunityId, Title = "Test Opportunity" };
        var posterDto = new OpportunityPosterDto { Id = posterId, Name = "Test Poster" };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(poster);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(opportunity)).Returns(opportunityDto);
        _mapperMock.Setup(m => m.Map<OpportunityPosterDto>(poster)).Returns(posterDto);

        // Act
        var result = await _sut.GetByIdAsync(opportunityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Opportunity");
    }

    [Fact]
    public async Task GetByIdAsync_WhenOpportunityNotExists_ReturnsNotFound()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Opportunity?)null);

        // Act
        var result = await _sut.GetByIdAsync(opportunityId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_WhenOpportunityExists_ReturnsOpportunity()
    {
        // Arrange
        var slug = "test-opportunity";
        var posterId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = Guid.NewGuid().ToString(),
            title = "Test Opportunity",
            slug = slug,
            description = "A test opportunity",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            posterId = posterId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var opportunityDto = new OpportunityDto { Title = "Test Opportunity", Slug = slug };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Opportunity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(opportunity)).Returns(opportunityDto);

        // Act
        var result = await _sut.GetBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllOpportunities()
    {
        // Arrange
        var posterId = Guid.NewGuid().ToString();
        var opportunities = new List<Opportunity>
        {
            new Opportunity { id = Guid.NewGuid().ToString(), title = "Opportunity 1", slug = "opportunity-1", description = "Desc 1", type = OpportunityType.PROJECT_ROLE, status = OpportunityStatus.OPEN, experienceLevel = ExperienceLevel.JUNIOR, isRemote = true, applicationsCount = 0, posterId = posterId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Opportunity { id = Guid.NewGuid().ToString(), title = "Opportunity 2", slug = "opportunity-2", description = "Desc 2", type = OpportunityType.FREELANCE, status = OpportunityStatus.OPEN, experienceLevel = ExperienceLevel.SENIOR, isRemote = false, applicationsCount = 0, posterId = posterId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunities);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(It.IsAny<Opportunity>()))
            .Returns((Opportunity o) => new OpportunityDto { Id = o.id, Title = o.title });

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedOpportunity()
    {
        // Arrange
        var posterId = Guid.NewGuid().ToString();
        var dto = new CreateOpportunityDto
        {
            PosterId = posterId,
            Title = "New Opportunity",
            Description = "A new opportunity",
            Type = OpportunityType.TASK_BOUNTY,
            ExperienceLevel = ExperienceLevel.MID,
            IsRemote = true
        };
        var opportunityDto = new OpportunityDto { Title = "New Opportunity" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Opportunity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Opportunity o, CancellationToken _) => o);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(It.IsAny<Opportunity>())).Returns(opportunityDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Opportunity");
    }

    [Fact]
    public async Task DeleteAsync_WhenOpportunityExists_ReturnsSuccess()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            posterId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _repositoryMock.Setup(r => r.DeleteAsync(opportunity, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(opportunityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CloseAsync_WhenOpportunityExists_ClosesOpportunity()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var posterId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            posterId = posterId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var opportunityDto = new OpportunityDto { Id = opportunityId, Title = "Test Opportunity", Status = OpportunityStatus.CLOSED };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Opportunity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(It.IsAny<Opportunity>())).Returns(opportunityDto);

        // Act
        var result = await _sut.CloseAsync(opportunityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        opportunity.status.Should().Be(OpportunityStatus.CLOSED);
        opportunity.closedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ApplyAsync_WithValidRequest_ReturnsApplication()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var applicantId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            maxApplications = 100,
            posterId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new ApplyToOpportunityDto
        {
            ApplicantId = applicantId,
            CoverLetter = "I am interested in this opportunity"
        };
        var applicant = new User { id = applicantId, name = "Test Applicant", email = "applicant@test.com" };
        var applicationDto = new OpportunityApplicationDto { OpportunityId = opportunityId, ApplicantId = applicantId };
        var applicantDto = new OpportunityApplicationApplicantDto { Id = applicantId, Name = "Test Applicant" };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _applicationRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpportunityApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpportunityApplication?)null);
        _applicationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<OpportunityApplication>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpportunityApplication a, CancellationToken _) => a);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Opportunity>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);
        _mapperMock.Setup(m => m.Map<OpportunityApplicationDto>(It.IsAny<OpportunityApplication>())).Returns(applicationDto);
        _mapperMock.Setup(m => m.Map<OpportunityApplicationApplicantDto>(applicant)).Returns(applicantDto);

        // Act
        var result = await _sut.ApplyAsync(opportunityId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        opportunity.applicationsCount.Should().Be(1);
    }

    [Fact]
    public async Task ApplyAsync_WhenOpportunityNotOpen_ReturnsValidationError()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var applicantId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.CLOSED,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 0,
            posterId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new ApplyToOpportunityDto
        {
            ApplicantId = applicantId,
            CoverLetter = "I am interested"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);

        // Act
        var result = await _sut.ApplyAsync(opportunityId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ApplyAsync_WhenAlreadyApplied_ReturnsValidationError()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var applicantId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 1,
            posterId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var existingApplication = new OpportunityApplication { id = Guid.NewGuid().ToString(), opportunityId = opportunityId, applicantId = applicantId };
        var dto = new ApplyToOpportunityDto
        {
            ApplicantId = applicantId,
            CoverLetter = "I am interested"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _applicationRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpportunityApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingApplication);

        // Act
        var result = await _sut.ApplyAsync(opportunityId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ApplyAsync_WhenMaxApplicationsReached_ReturnsValidationError()
    {
        // Arrange
        var opportunityId = Guid.NewGuid().ToString();
        var applicantId = Guid.NewGuid().ToString();
        var opportunity = new Opportunity
        {
            id = opportunityId,
            title = "Test Opportunity",
            slug = "test-opportunity",
            description = "Desc",
            type = OpportunityType.PROJECT_ROLE,
            status = OpportunityStatus.OPEN,
            experienceLevel = ExperienceLevel.MID,
            isRemote = true,
            applicationsCount = 10,
            maxApplications = 10,
            posterId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new ApplyToOpportunityDto
        {
            ApplicantId = applicantId,
            CoverLetter = "I am interested"
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(opportunityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunity);
        _applicationRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpportunityApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpportunityApplication?)null);

        // Act
        var result = await _sut.ApplyAsync(opportunityId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task GetByPosterIdAsync_ReturnsPosterOpportunities()
    {
        // Arrange
        var posterId = Guid.NewGuid().ToString();
        var opportunities = new List<Opportunity>
        {
            new Opportunity { id = Guid.NewGuid().ToString(), title = "Poster Opportunity", slug = "poster-opportunity", description = "Desc", type = OpportunityType.PROJECT_ROLE, status = OpportunityStatus.OPEN, experienceLevel = ExperienceLevel.MID, isRemote = true, applicationsCount = 0, posterId = posterId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Opportunity, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(opportunities);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(posterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<OpportunityDto>(It.IsAny<Opportunity>()))
            .Returns((Opportunity o) => new OpportunityDto { Id = o.id, Title = o.title });

        // Act
        var result = await _sut.GetByPosterIdAsync(posterId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateApplicationStatusAsync_WhenApplicationExists_UpdatesStatus()
    {
        // Arrange
        var applicationId = Guid.NewGuid().ToString();
        var applicantId = Guid.NewGuid().ToString();
        var application = new OpportunityApplication
        {
            id = applicationId,
            opportunityId = Guid.NewGuid().ToString(),
            applicantId = applicantId,
            coverLetter = "Test",
            status = ApplicationStatus.PENDING,
            appliedAt = DateTime.UtcNow
        };
        var dto = new UpdateApplicationStatusDto
        {
            Status = ApplicationStatus.ACCEPTED,
            ReviewNotes = "Great candidate"
        };
        var applicant = new User { id = applicantId, name = "Test Applicant" };
        var applicationDto = new OpportunityApplicationDto { Id = applicationId, Status = ApplicationStatus.ACCEPTED };
        var applicantDto = new OpportunityApplicationApplicantDto { Id = applicantId, Name = "Test Applicant" };

        _applicationRepositoryMock.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _applicationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<OpportunityApplication>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(applicantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(applicant);
        _mapperMock.Setup(m => m.Map<OpportunityApplicationDto>(It.IsAny<OpportunityApplication>())).Returns(applicationDto);
        _mapperMock.Setup(m => m.Map<OpportunityApplicationApplicantDto>(applicant)).Returns(applicantDto);

        // Act
        var result = await _sut.UpdateApplicationStatusAsync(applicationId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        application.status.Should().Be(ApplicationStatus.ACCEPTED);
        application.reviewNotes.Should().Be("Great candidate");
        application.reviewedAt.Should().NotBeNull();
    }
}
