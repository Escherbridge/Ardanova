namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Enums;
using FluentAssertions;

public class ManualKycProviderServiceTests
{
    private readonly ManualKycProviderService _sut;

    public ManualKycProviderServiceTests()
    {
        _sut = new ManualKycProviderService();
    }

    #region ValidateDocumentsAsync

    [Fact]
    public async Task ValidateDocumentsAsync_WithValidDocuments_ReturnsSuccess()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "https://s3.example.com/doc1.jpg",
                FileName = "id-front.jpg",
                MimeType = "image/jpeg",
                FileSizeBytes = 1024 * 500 // 500 KB
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithEmptyList_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>();

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("At least one document is required");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithNullList_ReturnsValidationError()
    {
        // Act
        var result = await _sut.ValidateDocumentsAsync(null!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithUnsupportedMimeType_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "https://s3.example.com/doc1.exe",
                FileName = "doc1.exe",
                MimeType = "application/x-msdownload",
                FileSizeBytes = 1024
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("unsupported file type");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithFileTooLarge_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "https://s3.example.com/bigfile.jpg",
                FileName = "bigfile.jpg",
                MimeType = "image/jpeg",
                FileSizeBytes = 11 * 1024 * 1024 // 11 MB (over 10 MB limit)
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("maximum file size");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithZeroFileSize_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "https://s3.example.com/empty.jpg",
                FileName = "empty.jpg",
                MimeType = "image/jpeg",
                FileSizeBytes = 0
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("invalid file size");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithMissingFileUrl_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "",
                FileName = "doc.jpg",
                MimeType = "image/jpeg"
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("missing a file URL");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithMissingFileName_ReturnsValidationError()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.GOVERNMENT_ID,
                FileUrl = "https://s3.example.com/doc.jpg",
                FileName = "",
                MimeType = "image/jpeg"
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
        result.Error.Should().Contain("file name");
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithPdfMimeType_ReturnsSuccess()
    {
        // Arrange
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.PROOF_OF_ADDRESS,
                FileUrl = "https://s3.example.com/bill.pdf",
                FileName = "utility-bill.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 1024 * 200
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateDocumentsAsync_WithNoMimeType_ReturnsSuccess()
    {
        // Arrange - when mime type is not provided, validation should pass
        var documents = new List<SubmitKycDocumentDto>
        {
            new SubmitKycDocumentDto
            {
                Type = KycDocumentType.SELFIE,
                FileUrl = "https://s3.example.com/selfie.jpg",
                FileName = "selfie.jpg",
                MimeType = null,
                FileSizeBytes = 1024 * 100
            }
        };

        // Act
        var result = await _sut.ValidateDocumentsAsync(documents);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region CreateSessionAsync

    [Fact]
    public async Task CreateSessionAsync_ReturnsUserId()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var documents = new List<KycDocumentDto>();

        // Act
        var result = await _sut.CreateSessionAsync(userId, documents);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(userId);
    }

    #endregion

    #region GetSessionStatusAsync

    [Fact]
    public async Task GetSessionStatusAsync_ReturnsPending()
    {
        // Act
        var result = await _sut.GetSessionStatusAsync("any-session-id");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(KycStatus.PENDING);
    }

    #endregion

    #region HandleWebhookAsync

    [Fact]
    public async Task HandleWebhookAsync_ReturnsSuccess()
    {
        // Act
        var result = await _sut.HandleWebhookAsync("any-payload");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(KycStatus.PENDING);
    }

    #endregion
}
