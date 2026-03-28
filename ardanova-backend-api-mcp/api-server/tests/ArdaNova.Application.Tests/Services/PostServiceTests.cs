namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Domain.Models.Entities;
using AutoMapper;
using FluentAssertions;
using Moq;

public class PostServiceTests
{
    private readonly Mock<IRepository<Post>> _posts = new();
    private readonly Mock<IRepository<PostLike>> _likes = new();
    private readonly Mock<IRepository<PostComment>> _comments = new();
    private readonly Mock<IRepository<PostBookmark>> _bookmarks = new();
    private readonly Mock<IRepository<PostShare>> _shares = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PostService _sut;

    public PostServiceTests()
    {
        _sut = new PostService(
            _posts.Object,
            _likes.Object,
            _comments.Object,
            _bookmarks.Object,
            _shares.Object,
            _unitOfWork.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenPostMissing_ReturnsNotFound()
    {
        _posts.Setup(r => r.GetByIdAsync("nope", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Post?)null);

        var result = await _sut.GetByIdAsync("nope");

        result.IsFailure.Should().BeTrue();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNoPosts_ReturnsEmptyList()
    {
        _posts.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Post, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Post>());

        var result = await _sut.GetByUserIdAsync("user-1");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull().And.BeEmpty();
    }
}
