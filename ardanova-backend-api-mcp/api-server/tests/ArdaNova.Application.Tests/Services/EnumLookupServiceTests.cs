namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Results;
using ArdaNova.Application.Services.Implementations;
using FluentAssertions;

public class EnumLookupServiceTests
{
    private readonly EnumLookupService _sut;

    public EnumLookupServiceTests()
    {
        _sut = new EnumLookupService();
    }

    [Fact]
    public void GetAllEnumNames_ReturnsSuccessWithAllEnums()
    {
        // Act
        var result = _sut.GetAllEnumNames();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().Contain("TaskPriority");
        result.Value.Should().Contain("ProjectStatus");
        result.Value.Should().Contain("UserRole");
    }

    [Fact]
    public void GetAllEnumNames_ReturnsSortedList()
    {
        // Act
        var result = _sut.GetAllEnumNames();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeInAscendingOrder();
    }

    [Fact]
    public void GetEnumValues_WithValidEnum_ReturnsValues()
    {
        // Act
        var result = _sut.GetEnumValues("TaskPriority");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("LOW");
        result.Value.Should().Contain("MEDIUM");
        result.Value.Should().Contain("HIGH");
        result.Value.Should().Contain("URGENT");
    }

    [Fact]
    public void GetEnumValues_WithValidEnum_ReturnsAllValues()
    {
        // Act
        var result = _sut.GetEnumValues("ProjectStatus");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(7);
        result.Value.Should().ContainInOrder("DRAFT", "PUBLISHED", "SEEKING_SUPPORT", "FUNDED", "IN_PROGRESS", "COMPLETED", "CANCELLED");
    }

    [Fact]
    public void GetEnumValues_IsCaseInsensitive()
    {
        // Act
        var lowercase = _sut.GetEnumValues("taskpriority");
        var uppercase = _sut.GetEnumValues("TASKPRIORITY");
        var mixedCase = _sut.GetEnumValues("TaskPriority");

        // Assert
        lowercase.IsSuccess.Should().BeTrue();
        uppercase.IsSuccess.Should().BeTrue();
        mixedCase.IsSuccess.Should().BeTrue();
        lowercase.Value.Should().BeEquivalentTo(uppercase.Value);
        lowercase.Value.Should().BeEquivalentTo(mixedCase.Value);
    }

    [Fact]
    public void GetEnumValues_WithInvalidEnum_ReturnsNotFound()
    {
        // Act
        var result = _sut.GetEnumValues("NonExistentEnum");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
        result.Error.Should().Contain("NonExistentEnum");
    }

    [Fact]
    public void GetEnumValues_WithEmptyString_ReturnsNotFound()
    {
        // Act
        var result = _sut.GetEnumValues("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public void GetAllEnumNames_ContainsExpectedCount()
    {
        // Act
        var result = _sut.GetAllEnumNames();

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should have a significant number of enums (currently 66+)
        result.Value.Should().HaveCountGreaterThanOrEqualTo(60);
    }

    [Theory]
    [InlineData("UserRole", new[] { "INDIVIDUAL", "GUILD", "ADMIN" })]
    [InlineData("Priority", new[] { "LOW", "MEDIUM", "HIGH", "CRITICAL" })]
    [InlineData("UserTier", new[] { "BRONZE", "SILVER", "GOLD", "PLATINUM", "DIAMOND" })]
    public void GetEnumValues_ReturnsCorrectValues(string enumName, string[] expectedValues)
    {
        // Act
        var result = _sut.GetEnumValues(enumName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainInOrder(expectedValues);
    }
}
