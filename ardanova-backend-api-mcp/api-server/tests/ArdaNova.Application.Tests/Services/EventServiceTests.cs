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

public class EventServiceTests
{
    private readonly Mock<IRepository<Event>> _repositoryMock;
    private readonly Mock<IRepository<EventAttendee>> _attendeeRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly EventService _sut;

    public EventServiceTests()
    {
        _repositoryMock = new Mock<IRepository<Event>>();
        _attendeeRepositoryMock = new Mock<IRepository<EventAttendee>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _sut = new EventService(
            _repositoryMock.Object,
            _attendeeRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventExists_ReturnsSuccessResult()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var organizerId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            description = "A test event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 0,
            organizerId = organizerId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var organizer = new User { id = organizerId, name = "Test Organizer" };
        var eventDto = new EventDto { Id = eventId, Title = "Test Event" };
        var organizerDto = new EventOrganizerDto { Id = organizerId, Name = "Test Organizer" };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);
        _mapperMock.Setup(m => m.Map<EventDto>(evt)).Returns(eventDto);
        _mapperMock.Setup(m => m.Map<EventOrganizerDto>(organizer)).Returns(organizerDto);

        // Act
        var result = await _sut.GetByIdAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Test Event");
    }

    [Fact]
    public async Task GetByIdAsync_WhenEventNotExists_ReturnsNotFound()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event?)null);

        // Act
        var result = await _sut.GetByIdAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task GetBySlugAsync_WhenEventExists_ReturnsEvent()
    {
        // Arrange
        var slug = "test-event";
        var organizerId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = Guid.NewGuid().ToString(),
            title = "Test Event",
            slug = slug,
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 0,
            organizerId = organizerId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var eventDto = new EventDto { Title = "Test Event", Slug = slug };

        _repositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<EventDto>(evt)).Returns(eventDto);

        // Act
        var result = await _sut.GetBySlugAsync(slug);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEvents()
    {
        // Arrange
        var organizerId = Guid.NewGuid().ToString();
        var events = new List<Event>
        {
            new Event { id = Guid.NewGuid().ToString(), title = "Event 1", slug = "event-1", type = EventType.WEBINAR, visibility = EventVisibility.PUBLIC, status = EventStatus.SCHEDULED, isOnline = true, timezone = "UTC", startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(1).AddHours(2), attendeesCount = 0, organizerId = organizerId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow },
            new Event { id = Guid.NewGuid().ToString(), title = "Event 2", slug = "event-2", type = EventType.MEETING, visibility = EventVisibility.PUBLIC, status = EventStatus.SCHEDULED, isOnline = false, timezone = "UTC", startDate = DateTime.UtcNow.AddDays(2), endDate = DateTime.UtcNow.AddDays(2).AddHours(3), attendeesCount = 0, organizerId = organizerId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.id, Title = e.title });

        // Act
        var result = await _sut.GetAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidDto_ReturnsCreatedEvent()
    {
        // Arrange
        var organizerId = Guid.NewGuid().ToString();
        var dto = new CreateEventDto
        {
            OrganizerId = organizerId,
            Title = "New Event",
            Description = "A new event",
            Type = EventType.WORKSHOP,
            Visibility = EventVisibility.PUBLIC,
            IsOnline = true,
            Timezone = "UTC",
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(7).AddHours(4)
        };
        var eventDto = new EventDto { Title = "New Event" };

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Event e, CancellationToken _) => e);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>())).Returns(eventDto);

        // Act
        var result = await _sut.CreateAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("New Event");
    }

    [Fact]
    public async Task DeleteAsync_WhenEventExists_ReturnsSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 0,
            organizerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _repositoryMock.Setup(r => r.DeleteAsync(evt, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.DeleteAsync(eventId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_ReturnsAttendee()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 0,
            maxAttendees = 100,
            organizerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new RegisterEventDto { UserId = userId };
        var user = new User { id = userId, name = "Test User" };
        var attendeeDto = new EventAttendeeDto { EventId = eventId, UserId = userId };
        var userDto = new EventAttendeeUserDto { Id = userId, Name = "Test User" };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _attendeeRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventAttendee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAttendee?)null);
        _attendeeRepositoryMock.Setup(r => r.AddAsync(It.IsAny<EventAttendee>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAttendee a, CancellationToken _) => a);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mapperMock.Setup(m => m.Map<EventAttendeeDto>(It.IsAny<EventAttendee>())).Returns(attendeeDto);
        _mapperMock.Setup(m => m.Map<EventAttendeeUserDto>(user)).Returns(userDto);

        // Act
        var result = await _sut.RegisterAsync(eventId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        evt.attendeesCount.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAsync_WhenAlreadyRegistered_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 1,
            organizerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var existingAttendee = new EventAttendee { id = Guid.NewGuid().ToString(), eventId = eventId, userId = userId };
        var dto = new RegisterEventDto { UserId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _attendeeRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventAttendee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAttendee);

        // Act
        var result = await _sut.RegisterAsync(eventId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task RegisterAsync_WhenMaxAttendeesReached_ReturnsValidationError()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 100,
            maxAttendees = 100,
            organizerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var dto = new RegisterEventDto { UserId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _attendeeRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventAttendee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAttendee?)null);

        // Act
        var result = await _sut.RegisterAsync(eventId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.ValidationError);
    }

    [Fact]
    public async Task UnregisterAsync_WhenRegistered_ReturnsSuccess()
    {
        // Arrange
        var eventId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var evt = new Event
        {
            id = eventId,
            title = "Test Event",
            slug = "test-event",
            type = EventType.WEBINAR,
            visibility = EventVisibility.PUBLIC,
            status = EventStatus.SCHEDULED,
            isOnline = true,
            timezone = "UTC",
            startDate = DateTime.UtcNow.AddDays(1),
            endDate = DateTime.UtcNow.AddDays(1).AddHours(2),
            attendeesCount = 1,
            organizerId = Guid.NewGuid().ToString(),
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        var attendee = new EventAttendee { id = Guid.NewGuid().ToString(), eventId = eventId, userId = userId };

        _repositoryMock.Setup(r => r.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(evt);
        _attendeeRepositoryMock.Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<EventAttendee, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(attendee);
        _attendeeRepositoryMock.Setup(r => r.DeleteAsync(attendee, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.UnregisterAsync(eventId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        evt.attendeesCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByOrganizerIdAsync_ReturnsOrganizerEvents()
    {
        // Arrange
        var organizerId = Guid.NewGuid().ToString();
        var events = new List<Event>
        {
            new Event { id = Guid.NewGuid().ToString(), title = "Organizer Event", slug = "organizer-event", type = EventType.WEBINAR, visibility = EventVisibility.PUBLIC, status = EventStatus.SCHEDULED, isOnline = true, timezone = "UTC", startDate = DateTime.UtcNow.AddDays(1), endDate = DateTime.UtcNow.AddDays(1).AddHours(2), attendeesCount = 0, organizerId = organizerId, createdAt = DateTime.UtcNow, updatedAt = DateTime.UtcNow }
        };

        _repositoryMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Event, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(organizerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _mapperMock.Setup(m => m.Map<EventDto>(It.IsAny<Event>()))
            .Returns((Event e) => new EventDto { Id = e.id, Title = e.title });

        // Act
        var result = await _sut.GetByOrganizerIdAsync(organizerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }
}
