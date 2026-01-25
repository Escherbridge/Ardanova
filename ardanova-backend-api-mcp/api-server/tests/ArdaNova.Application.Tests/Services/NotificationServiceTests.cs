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

public class NotificationServiceTests
{
    private readonly Mock<IRepository<Notification>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Notification>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new NotificationService(_repositoryMock.Object, _unitOfWorkMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationExists_ReturnsSuccessResult()
    {
        // Arrange
        var notificationId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var notification = new Notification
        {
            id = notificationId,
            userId = userId,
            type = NotificationType.PROJECT_UPDATE,
            title = "Title",
            message = "Test Message",
            isRead = false,
            createdAt = DateTime.UtcNow
        };
        var notificationDto = new NotificationDto { Id = notificationId, UserId = userId, Message = "Test Message" };

        _repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);
        _mapperMock.Setup(m => m.Map<NotificationDto>(notification)).Returns(notificationDto);

        // Act
        var result = await _sut.GetByIdAsync(notificationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Message.Should().Be("Test Message");
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationNotExists_ReturnsNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        // Act
        var result = await _sut.GetByIdAsync(notificationId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsNotificationsForUser()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var notifications = new List<Notification>
        {
            new Notification { id = Guid.NewGuid().ToString(), userId = userId, type = NotificationType.TASK_ASSIGNED, title = "Task", message = "Task assigned", isRead = false, createdAt = DateTime.UtcNow },
            new Notification { id = Guid.NewGuid().ToString(), userId = userId, type = NotificationType.COMMENT_REPLY, title = "Reply", message = "New reply", isRead = false, createdAt = DateTime.UtcNow }
        };
        var notificationDtos = new List<NotificationDto>
        {
            new NotificationDto { UserId = userId, Message = "Task assigned" },
            new NotificationDto { UserId = userId, Message = "New reply" }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(notifications);
        _mapperMock.Setup(m => m.Map<IReadOnlyList<NotificationDto>>(It.IsAny<IEnumerable<Notification>>())).Returns(notificationDtos);

        // Act
        var result = await _sut.GetByUserIdAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedNotification()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var dto = new CreateNotificationDto
        {
            UserId = userId,
            Type = NotificationType.PROJECT_UPDATE,
            Title = "New Title",
            Message = "New notification"
        };
        var notificationDto = new NotificationDto { UserId = userId, Message = "New notification" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification n, CancellationToken _) => n);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(notificationDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Message.Should().Be("New notification");
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_MarksAsRead()
    {
        // Arrange
        var notificationId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var notification = new Notification
        {
            id = notificationId,
            userId = userId,
            type = NotificationType.PROJECT_UPDATE,
            title = "Title",
            message = "Test",
            isRead = false,
            createdAt = DateTime.UtcNow
        };
        var notificationDto = new NotificationDto { Id = notificationId, IsRead = true };

        _repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock.Setup(m => m.Map<NotificationDto>(It.IsAny<Notification>())).Returns(notificationDto);

        // Act
        var result = await _sut.MarkAsReadAsync(notificationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationExists_ReturnsSuccess()
    {
        // Arrange
        var notificationId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var notification = new Notification
        {
            id = notificationId,
            userId = userId,
            type = NotificationType.PROJECT_UPDATE,
            title = "Title",
            message = "Test",
            isRead = false,
            createdAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        _repositoryMock.Setup(r => r.DeleteAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(notificationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
