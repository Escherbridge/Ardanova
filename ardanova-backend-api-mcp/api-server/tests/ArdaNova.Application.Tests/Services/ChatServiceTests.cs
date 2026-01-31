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

public class ChatServiceTests
{
    private readonly Mock<IRepository<Conversation>> _conversationRepositoryMock;
    private readonly Mock<IRepository<ConversationMember>> _memberRepositoryMock;
    private readonly Mock<IRepository<ChatMessage>> _messageRepositoryMock;
    private readonly Mock<IRepository<User>> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _conversationRepositoryMock = new Mock<IRepository<Conversation>>();
        _memberRepositoryMock = new Mock<IRepository<ConversationMember>>();
        _messageRepositoryMock = new Mock<IRepository<ChatMessage>>();
        _userRepositoryMock = new Mock<IRepository<User>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();

        _sut = new ChatService(
            _conversationRepositoryMock.Object,
            _memberRepositoryMock.Object,
            _messageRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object);
    }

    #region GetConversationByIdAsync Tests

    [Fact]
    public async Task GetConversationByIdAsync_WhenMember_ReturnsConversation()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var membership = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = userId,
            role = ConversationRole.MEMBER,
            joinedAt = now
        };

        var conversation = new Conversation
        {
            id = conversationId,
            type = ConversationType.DIRECT,
            createdById = userId,
            createdAt = now,
            updatedAt = now
        };

        var members = new List<ConversationMember>
        {
            membership,
            new ConversationMember
            {
                id = Guid.NewGuid().ToString(),
                conversationId = conversationId,
                userId = otherUserId,
                role = ConversationRole.MEMBER,
                joinedAt = now
            }
        };

        var users = new List<User>
        {
            new User { id = userId, email = "user@test.com", name = "Test User", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now },
            new User { id = otherUserId, email = "other@test.com", name = "Other User", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now }
        };

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        _conversationRepositoryMock.Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _memberRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        _userRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _messageRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        // Act
        var result = await _sut.GetConversationByIdAsync(conversationId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(conversationId);
        result.Value.Type.Should().Be(ConversationType.DIRECT);
        result.Value.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetConversationByIdAsync_WhenNotMember_ReturnsUnauthorized()
    {
        // Arrange
        var conversationId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid().ToString();

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationMember?)null);

        // Act
        var result = await _sut.GetConversationByIdAsync(conversationId, userId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("not a member");
    }

    #endregion

    #region GetOrCreateDirectConversationAsync Tests

    [Fact]
    public async Task GetOrCreateDirectConversationAsync_WhenExists_ReturnsExisting()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var participantId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var dto = new CreateDirectConversationDto { ParticipantUserId = participantId };

        var participant = new User
        {
            id = participantId,
            email = "participant@test.com",
            name = "Participant",
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            createdAt = now,
            updatedAt = now
        };

        var existingConversation = new Conversation
        {
            id = conversationId,
            type = ConversationType.DIRECT,
            createdById = userId,
            createdAt = now,
            updatedAt = now
        };

        var userMembership = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = userId,
            role = ConversationRole.MEMBER,
            joinedAt = now
        };

        var participantMembership = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = participantId,
            role = ConversationRole.MEMBER,
            joinedAt = now
        };

        var allMembers = new List<ConversationMember> { userMembership, participantMembership };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        // FindAsync for user's memberships
        _memberRepositoryMock.SetupSequence(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationMember> { userMembership })  // User's memberships
            .ReturnsAsync(allMembers)  // Conversation members check
            .ReturnsAsync(allMembers); // GetConversationByIdAsync members load

        _conversationRepositoryMock.SetupSequence(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Conversation, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Conversation> { existingConversation });

        _conversationRepositoryMock.Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConversation);

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(userMembership);

        var users = new List<User>
        {
            new User { id = userId, email = "user@test.com", name = "Test User", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now },
            participant
        };

        _userRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        _messageRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        // Act
        var result = await _sut.GetOrCreateDirectConversationAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(conversationId);
        result.Value.Type.Should().Be(ConversationType.DIRECT);

        // Verify no new conversation was created
        _conversationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateDirectConversationAsync_WhenNotExists_CreatesNew()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var participantId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var dto = new CreateDirectConversationDto { ParticipantUserId = participantId };

        var participant = new User
        {
            id = participantId,
            email = "participant@test.com",
            name = "Participant",
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            createdAt = now,
            updatedAt = now
        };

        var currentUser = new User
        {
            id = userId,
            email = "user@test.com",
            name = "Test User",
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            createdAt = now,
            updatedAt = now
        };

        _userRepositoryMock.Setup(r => r.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        // No existing memberships
        _memberRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConversationMember>());

        // No existing direct conversations
        _conversationRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<Conversation, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Conversation>());

        Conversation? capturedConversation = null;
        _conversationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
            .Callback<Conversation, CancellationToken>((c, _) => capturedConversation = c)
            .ReturnsAsync((Conversation c, CancellationToken _) => c);

        _memberRepositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ConversationMember>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<ConversationMember> members, CancellationToken _) => members);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Setup for GetConversationByIdAsync call after creation
        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ConversationMember, bool>> expr, CancellationToken ct) =>
            {
                return new ConversationMember
                {
                    id = Guid.NewGuid().ToString(),
                    conversationId = capturedConversation?.id ?? "",
                    userId = userId,
                    role = ConversationRole.MEMBER,
                    joinedAt = now
                };
            });

        _conversationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken ct) => capturedConversation);

        _userRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { currentUser, participant });

        _messageRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        // Act
        var result = await _sut.GetOrCreateDirectConversationAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(ConversationType.DIRECT);

        // Verify conversation was created
        _conversationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Once);
        _memberRepositoryMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ConversationMember>>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateGroupConversationAsync Tests

    [Fact]
    public async Task CreateGroupConversationAsync_WithValidData_CreatesGroup()
    {
        // Arrange
        var creatorUserId = Guid.NewGuid().ToString();
        var memberUserId1 = Guid.NewGuid().ToString();
        var memberUserId2 = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var dto = new CreateGroupConversationDto
        {
            Name = "Test Group",
            AvatarUrl = "https://example.com/avatar.png",
            MemberUserIds = new List<string> { memberUserId1, memberUserId2 }
        };

        var users = new List<User>
        {
            new User { id = creatorUserId, email = "creator@test.com", name = "Creator", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now },
            new User { id = memberUserId1, email = "member1@test.com", name = "Member 1", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now },
            new User { id = memberUserId2, email = "member2@test.com", name = "Member 2", role = UserRole.INDIVIDUAL, userType = UserType.INNOVATOR, createdAt = now, updatedAt = now }
        };

        _userRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        Conversation? capturedConversation = null;
        _conversationRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
            .Callback<Conversation, CancellationToken>((c, _) => capturedConversation = c)
            .ReturnsAsync((Conversation c, CancellationToken _) => c);

        List<ConversationMember>? capturedMembers = null;
        _memberRepositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ConversationMember>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ConversationMember>, CancellationToken>((m, _) => capturedMembers = m.ToList())
            .ReturnsAsync((IEnumerable<ConversationMember> m, CancellationToken _) => m);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Setup for GetConversationByIdAsync call
        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ConversationMember, bool>> expr, CancellationToken ct) =>
            {
                return new ConversationMember
                {
                    id = Guid.NewGuid().ToString(),
                    conversationId = capturedConversation?.id ?? "",
                    userId = creatorUserId,
                    role = ConversationRole.OWNER,
                    joinedAt = now
                };
            });

        _conversationRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken ct) => capturedConversation);

        _memberRepositoryMock.SetupSequence(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => capturedMembers ?? new List<ConversationMember>());

        _messageRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ChatMessage>());

        // Act
        var result = await _sut.CreateGroupConversationAsync(creatorUserId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Type.Should().Be(ConversationType.GROUP);
        result.Value.Name.Should().Be("Test Group");

        // Verify conversation was created with correct properties
        capturedConversation.Should().NotBeNull();
        capturedConversation!.type.Should().Be(ConversationType.GROUP);
        capturedConversation.name.Should().Be("Test Group");
        capturedConversation.avatarUrl.Should().Be("https://example.com/avatar.png");
        capturedConversation.createdById.Should().Be(creatorUserId);

        // Verify members were created - creator should be OWNER
        capturedMembers.Should().NotBeNull();
        var membersList = capturedMembers!;
        membersList.Should().HaveCount(3);
        membersList.Should().Contain(m => m.userId == creatorUserId && m.role == ConversationRole.OWNER);
        membersList.Where(m => m.userId != creatorUserId).Should().OnlyContain(m => m.role == ConversationRole.MEMBER);
    }

    #endregion

    #region SendMessageAsync Tests

    [Fact]
    public async Task SendMessageAsync_WithValidData_SendsMessage()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var dto = new SendMessageDto
        {
            ConversationId = conversationId,
            Message = "Hello, World!"
        };

        var membership = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = userId,
            role = ConversationRole.MEMBER,
            joinedAt = now
        };

        var conversation = new Conversation
        {
            id = conversationId,
            type = ConversationType.DIRECT,
            createdById = userId,
            createdAt = now,
            updatedAt = now
        };

        var members = new List<ConversationMember>
        {
            membership,
            new ConversationMember
            {
                id = Guid.NewGuid().ToString(),
                conversationId = conversationId,
                userId = otherUserId,
                role = ConversationRole.MEMBER,
                joinedAt = now
            }
        };

        var user = new User
        {
            id = userId,
            email = "user@test.com",
            name = "Test User",
            role = UserRole.INDIVIDUAL,
            userType = UserType.INNOVATOR,
            createdAt = now,
            updatedAt = now
        };

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        _conversationRepositoryMock.Setup(r => r.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

        _memberRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(members);

        ChatMessage? capturedMessage = null;
        _messageRepositoryMock.Setup(r => r.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Callback<ChatMessage, CancellationToken>((m, _) => capturedMessage = m)
            .ReturnsAsync((ChatMessage m, CancellationToken _) => m);

        _conversationRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.SendMessageAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Message.Should().Be("Hello, World!");
        result.Value.UserFromId.Should().Be(userId);
        result.Value.UserFromName.Should().Be("Test User");
        result.Value.ConversationId.Should().Be(conversationId);
        result.Value.Status.Should().Be(MessageStatus.SENT);

        // Verify message was saved
        capturedMessage.Should().NotBeNull();
        capturedMessage!.message.Should().Be("Hello, World!");
        capturedMessage.userFromId.Should().Be(userId);
        capturedMessage.conversationId.Should().Be(conversationId);

        // Verify conversation was updated
        _conversationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Conversation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendMessageAsync_WhenNotMember_ReturnsUnauthorized()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();

        var dto = new SendMessageDto
        {
            ConversationId = conversationId,
            Message = "Hello, World!"
        };

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationMember?)null);

        // Act
        var result = await _sut.SendMessageAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Contain("not a member");

        // Verify no message was created
        _messageRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region MarkMessagesReadAsync Tests

    [Fact]
    public async Task MarkMessagesReadAsync_UpdatesLastReadAt()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;
        var readUpTo = now.AddMinutes(-5);

        var dto = new MarkMessagesReadDto
        {
            ConversationId = conversationId,
            ReadUpTo = readUpTo
        };

        var membership = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = userId,
            role = ConversationRole.MEMBER,
            joinedAt = now.AddDays(-1),
            lastReadAt = null
        };

        var unreadMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                id = Guid.NewGuid().ToString(),
                conversationId = conversationId,
                userFromId = otherUserId,
                userToId = userId,
                message = "Message 1",
                status = MessageStatus.DELIVERED,
                sentAt = readUpTo.AddMinutes(-2),
                isDeleted = false
            },
            new ChatMessage
            {
                id = Guid.NewGuid().ToString(),
                conversationId = conversationId,
                userFromId = otherUserId,
                userToId = userId,
                message = "Message 2",
                status = MessageStatus.SENT,
                sentAt = readUpTo.AddMinutes(-1),
                isDeleted = false
            }
        };

        _memberRepositoryMock.Setup(r => r.FindOneAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ConversationMember, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(membership);

        _memberRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ConversationMember>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _messageRepositoryMock.Setup(r => r.FindAsync(
            It.IsAny<System.Linq.Expressions.Expression<Func<ChatMessage, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(unreadMessages);

        _messageRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.MarkMessagesReadAsync(userId, dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // Verify membership was updated with lastReadAt
        membership.lastReadAt.Should().Be(readUpTo);
        membership.lastActiveAt.Should().NotBeNull();

        // Verify messages were updated to READ status
        unreadMessages.Should().OnlyContain(m => m.status == MessageStatus.READ);
        unreadMessages.Should().OnlyContain(m => m.seenAt != null);

        // Verify updates were persisted
        _memberRepositoryMock.Verify(r => r.UpdateAsync(membership, It.IsAny<CancellationToken>()), Times.Once);
        _messageRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ChatMessage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
