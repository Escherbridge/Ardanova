namespace ArdaNova.Application.Services.Implementations;

using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;

public class ChatService : IChatService
{
    private readonly IRepository<Conversation> _conversationRepository;
    private readonly IRepository<ConversationMember> _memberRepository;
    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ChatService(
        IRepository<Conversation> conversationRepository,
        IRepository<ConversationMember> memberRepository,
        IRepository<ChatMessage> messageRepository,
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _conversationRepository = conversationRepository;
        _memberRepository = memberRepository;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    // ========== Conversation Operations ==========

    public async Task<Result<ConversationDto>> GetConversationByIdAsync(
        string conversationId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        // Check if user is a member of the conversation
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == requestingUserId, ct);

        if (membership is null)
            return Result<ConversationDto>.Forbidden("You are not a member of this conversation");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return Result<ConversationDto>.NotFound($"Conversation with id {conversationId} not found");

        // Load members separately
        var members = await _memberRepository.FindAsync(m => m.conversationId == conversationId, ct);

        // Load users for members
        var memberUserIds = members.Select(m => m.userId).ToList();
        var users = await _userRepository.FindAsync(u => memberUserIds.Contains(u.id), ct);
        var usersById = users.ToDictionary(u => u.id);

        // Load last message
        var messages = await _messageRepository.FindAsync(
            m => m.conversationId == conversationId && !m.isDeleted, ct);
        var lastMessage = messages.OrderByDescending(m => m.sentAt).FirstOrDefault();

        // Load last message sender if exists
        User? lastMessageSender = null;
        if (lastMessage is not null)
        {
            lastMessageSender = usersById.GetValueOrDefault(lastMessage.userFromId)
                ?? await _userRepository.GetByIdAsync(lastMessage.userFromId, ct);
        }

        var dto = MapConversationToDto(conversation, members, usersById, lastMessage, lastMessageSender, requestingUserId);
        return Result<ConversationDto>.Success(dto);
    }

    public async Task<Result<ConversationListDto>> GetUserConversationsAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        // Get conversation IDs where user is a member
        var userMemberships = await _memberRepository.FindAsync(m => m.userId == userId, ct);
        var conversationIds = userMemberships.Select(m => m.conversationId).ToList();

        var totalCount = conversationIds.Count;

        // Get conversations
        var allConversations = await _conversationRepository.FindAsync(
            c => conversationIds.Contains(c.id), ct);

        var conversations = allConversations
            .OrderByDescending(c => c.lastMessageAt ?? c.createdAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Load all members for these conversations
        var convIds = conversations.Select(c => c.id).ToList();
        var allMembers = await _memberRepository.FindAsync(m => convIds.Contains(m.conversationId), ct);

        // Load all users for members
        var memberUserIds = allMembers.Select(m => m.userId).Distinct().ToList();
        var users = await _userRepository.FindAsync(u => memberUserIds.Contains(u.id), ct);
        var usersById = users.ToDictionary(u => u.id);

        // Load last messages
        var allMessages = await _messageRepository.FindAsync(
            m => convIds.Contains(m.conversationId ?? "") && !m.isDeleted, ct);
        var lastMessageByConv = allMessages
            .GroupBy(m => m.conversationId)
            .ToDictionary(g => g.Key!, g => g.OrderByDescending(m => m.sentAt).First());

        var items = conversations.Select(c =>
        {
            var members = allMembers.Where(m => m.conversationId == c.id).ToList();
            var lastMessage = lastMessageByConv.GetValueOrDefault(c.id);
            User? lastMessageSender = lastMessage is not null
                ? usersById.GetValueOrDefault(lastMessage.userFromId)
                : null;
            return MapConversationToDto(c, members, usersById, lastMessage, lastMessageSender, userId);
        }).ToList();

        return Result<ConversationListDto>.Success(new ConversationListDto
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = (page * pageSize) < totalCount
        });
    }

    public async Task<Result<ConversationDto>> GetOrCreateDirectConversationAsync(
        string userId,
        CreateDirectConversationDto dto,
        CancellationToken ct = default)
    {
        // Validate the participant exists
        var participant = await _userRepository.GetByIdAsync(dto.ParticipantUserId, ct);
        if (participant is null)
            return Result<ConversationDto>.NotFound($"User with id {dto.ParticipantUserId} not found");

        // Check if a direct conversation already exists between these two users
        var userMemberships = await _memberRepository.FindAsync(m => m.userId == userId, ct);
        var userConversationIds = userMemberships.Select(m => m.conversationId).ToList();

        // Get direct conversations where user is a member
        var directConversations = await _conversationRepository.FindAsync(
            c => c.type == ConversationType.DIRECT && userConversationIds.Contains(c.id), ct);

        var isSelfConversation = userId == dto.ParticipantUserId;

        foreach (var conv in directConversations)
        {
            var convMembers = await _memberRepository.FindAsync(m => m.conversationId == conv.id, ct);

            if (isSelfConversation)
            {
                // Self-conversation: look for a direct conversation with only this user
                if (convMembers.Count == 1 && convMembers[0].userId == userId)
                {
                    return await GetConversationByIdAsync(conv.id, userId, ct);
                }
            }
            else
            {
                if (convMembers.Any(m => m.userId == dto.ParticipantUserId))
                {
                    // Found existing conversation
                    return await GetConversationByIdAsync(conv.id, userId, ct);
                }
            }
        }

        // Create new direct conversation
        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            id = Guid.NewGuid().ToString(),
            type = ConversationType.DIRECT,
            createdById = userId,
            createdAt = now,
            updatedAt = now
        };

        await _conversationRepository.AddAsync(conversation, ct);

        // Add members
        var member1 = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversation.id,
            userId = userId,
            role = ConversationRole.MEMBER,
            joinedAt = now
        };

        if (isSelfConversation)
        {
            // Self-conversation: only one member entry
            await _memberRepository.AddAsync(member1, ct);
        }
        else
        {
            var member2 = new ConversationMember
            {
                id = Guid.NewGuid().ToString(),
                conversationId = conversation.id,
                userId = dto.ParticipantUserId,
                role = ConversationRole.MEMBER,
                joinedAt = now
            };
            await _memberRepository.AddRangeAsync([member1, member2], ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.id, userId, ct);
    }

    public async Task<Result<ConversationDto>> CreateGroupConversationAsync(
        string creatorUserId,
        CreateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result<ConversationDto>.ValidationError("Group name is required");

        if (dto.MemberUserIds.Count < 1)
            return Result<ConversationDto>.ValidationError("At least one member is required for a group conversation");

        // Validate all member user IDs exist
        var memberUserIds = dto.MemberUserIds.Distinct().ToList();
        if (!memberUserIds.Contains(creatorUserId))
            memberUserIds.Add(creatorUserId);

        var existingUsers = await _userRepository.FindAsync(
            u => memberUserIds.Contains(u.id), ct);

        if (existingUsers.Count != memberUserIds.Count)
        {
            var existingIds = existingUsers.Select(u => u.id).ToHashSet();
            var missingIds = memberUserIds.Where(id => !existingIds.Contains(id)).ToList();
            return Result<ConversationDto>.ValidationError(
                $"Users not found: {string.Join(", ", missingIds)}");
        }

        var now = DateTime.UtcNow;
        var conversation = new Conversation
        {
            id = Guid.NewGuid().ToString(),
            type = ConversationType.GROUP,
            name = dto.Name,
            avatarUrl = dto.AvatarUrl,
            createdById = creatorUserId,
            createdAt = now,
            updatedAt = now
        };

        await _conversationRepository.AddAsync(conversation, ct);

        // Create members with creator as OWNER
        var members = memberUserIds.Select(uid => new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversation.id,
            userId = uid,
            role = uid == creatorUserId ? ConversationRole.OWNER : ConversationRole.MEMBER,
            joinedAt = now
        }).ToList();

        await _memberRepository.AddRangeAsync(members, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversation.id, creatorUserId, ct);
    }

    public async Task<Result<ConversationDto>> UpdateGroupConversationAsync(
        string conversationId,
        string requestingUserId,
        UpdateGroupConversationDto dto,
        CancellationToken ct = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return Result<ConversationDto>.NotFound($"Conversation with id {conversationId} not found");

        if (conversation.type != ConversationType.GROUP)
            return Result<ConversationDto>.ValidationError("Only group conversations can be updated");

        // Check if user is admin or owner
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == requestingUserId, ct);

        if (membership is null)
            return Result<ConversationDto>.Forbidden("You are not a member of this conversation");

        if (membership.role != ConversationRole.ADMIN && membership.role != ConversationRole.OWNER)
            return Result<ConversationDto>.Forbidden("Only admins and owners can update the conversation");

        if (dto.Name is not null)
            conversation.name = dto.Name;

        if (dto.AvatarUrl is not null)
            conversation.avatarUrl = dto.AvatarUrl;

        conversation.updatedAt = DateTime.UtcNow;

        await _conversationRepository.UpdateAsync(conversation, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await GetConversationByIdAsync(conversationId, requestingUserId, ct);
    }

    public async Task<Result<ConversationMemberDto>> AddMemberAsync(
        string conversationId,
        string requestingUserId,
        AddConversationMemberDto dto,
        CancellationToken ct = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return Result<ConversationMemberDto>.NotFound($"Conversation with id {conversationId} not found");

        if (conversation.type != ConversationType.GROUP)
            return Result<ConversationMemberDto>.ValidationError("Can only add members to group conversations");

        // Check if requester is admin/owner
        var requesterMembership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == requestingUserId, ct);

        if (requesterMembership is null)
            return Result<ConversationMemberDto>.Forbidden("You are not a member of this conversation");

        if (requesterMembership.role != ConversationRole.ADMIN && requesterMembership.role != ConversationRole.OWNER)
            return Result<ConversationMemberDto>.Forbidden("Only admins and owners can add members");

        // Check if user to add exists
        var userToAdd = await _userRepository.GetByIdAsync(dto.UserId, ct);
        if (userToAdd is null)
            return Result<ConversationMemberDto>.NotFound($"User with id {dto.UserId} not found");

        // Check if already a member
        var existingMembership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == dto.UserId, ct);

        if (existingMembership is not null)
            return Result<ConversationMemberDto>.ValidationError("User is already a member of this conversation");

        var now = DateTime.UtcNow;
        var newMember = new ConversationMember
        {
            id = Guid.NewGuid().ToString(),
            conversationId = conversationId,
            userId = dto.UserId,
            role = dto.Role,
            joinedAt = now
        };

        await _memberRepository.AddAsync(newMember, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<ConversationMemberDto>.Success(new ConversationMemberDto
        {
            Id = newMember.id,
            UserId = userToAdd.id,
            UserName = userToAdd.name ?? userToAdd.email,
            UserImage = userToAdd.image,
            Role = newMember.role,
            LastReadAt = newMember.lastReadAt,
            LastActiveAt = newMember.lastActiveAt,
            JoinedAt = newMember.joinedAt,
            IsOnline = false
        });
    }

    public async Task<Result<bool>> RemoveMemberAsync(
        string conversationId,
        string memberUserId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return Result<bool>.NotFound($"Conversation with id {conversationId} not found");

        if (conversation.type != ConversationType.GROUP)
            return Result<bool>.ValidationError("Can only remove members from group conversations");

        // Check if requester is admin/owner
        var requesterMembership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == requestingUserId, ct);

        if (requesterMembership is null)
            return Result<bool>.Forbidden("You are not a member of this conversation");

        if (requesterMembership.role != ConversationRole.ADMIN && requesterMembership.role != ConversationRole.OWNER)
            return Result<bool>.Forbidden("Only admins and owners can remove members");

        // Cannot remove the owner
        var memberToRemove = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == memberUserId, ct);

        if (memberToRemove is null)
            return Result<bool>.NotFound("User is not a member of this conversation");

        if (memberToRemove.role == ConversationRole.OWNER)
            return Result<bool>.Forbidden("Cannot remove the owner of the conversation");

        await _memberRepository.DeleteAsync(memberToRemove, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> LeaveConversationAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default)
    {
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<bool>.NotFound("You are not a member of this conversation");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, ct);
        if (conversation is null)
            return Result<bool>.NotFound($"Conversation with id {conversationId} not found");

        // If leaving a group and user is owner, need to transfer ownership
        if (conversation.type == ConversationType.GROUP && membership.role == ConversationRole.OWNER)
        {
            var otherMembers = await _memberRepository.FindAsync(
                m => m.conversationId == conversationId && m.userId != userId, ct);

            if (otherMembers.Any())
            {
                // Transfer to first admin, or first member
                var newOwner = otherMembers.FirstOrDefault(m => m.role == ConversationRole.ADMIN)
                    ?? otherMembers.First();
                newOwner.role = ConversationRole.OWNER;
                await _memberRepository.UpdateAsync(newOwner, ct);
            }
        }

        await _memberRepository.DeleteAsync(membership, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    // ========== Message Operations ==========

    public async Task<Result<MessageListDto>> GetMessagesAsync(
        string conversationId,
        string requestingUserId,
        int limit = 50,
        string? beforeCursor = null,
        CancellationToken ct = default)
    {
        // Validate membership
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == requestingUserId, ct);

        if (membership is null)
            return Result<MessageListDto>.Forbidden("You are not a member of this conversation");

        // Get all messages for this conversation
        var allMessages = await _messageRepository.FindAsync(
            m => m.conversationId == conversationId && !m.isDeleted, ct);

        var query = allMessages.AsEnumerable();

        // Apply cursor-based pagination
        if (!string.IsNullOrEmpty(beforeCursor))
        {
            var cursorMessage = allMessages.FirstOrDefault(m => m.id == beforeCursor);
            if (cursorMessage is not null)
            {
                query = query.Where(m => m.sentAt < cursorMessage.sentAt ||
                    (m.sentAt == cursorMessage.sentAt && string.Compare(m.id, beforeCursor) < 0));
            }
        }

        var totalCount = allMessages.Count;

        var messages = query
            .OrderByDescending(m => m.sentAt)
            .ThenByDescending(m => m.id)
            .Take(limit + 1)
            .ToList();

        var hasMore = messages.Count > limit;
        var resultMessages = messages.Take(limit).ToList();

        // Load users for messages
        var userIds = resultMessages.Select(m => m.userFromId).Distinct().ToList();
        var users = await _userRepository.FindAsync(u => userIds.Contains(u.id), ct);
        var usersById = users.ToDictionary(u => u.id);

        // Load reply-to messages if any
        var replyToIds = resultMessages
            .Where(m => !string.IsNullOrEmpty(m.replyToId))
            .Select(m => m.replyToId!)
            .Distinct()
            .ToList();
        var replyToMessages = replyToIds.Any()
            ? await _messageRepository.FindAsync(m => replyToIds.Contains(m.id), ct)
            : new List<ChatMessage>();
        var replyToById = replyToMessages.ToDictionary(m => m.id);

        // Load users for reply-to messages
        var replyUserIds = replyToMessages.Select(m => m.userFromId).Distinct().Except(userIds).ToList();
        if (replyUserIds.Any())
        {
            var replyUsers = await _userRepository.FindAsync(u => replyUserIds.Contains(u.id), ct);
            foreach (var u in replyUsers)
                usersById.TryAdd(u.id, u);
        }

        var items = resultMessages.Select(m => MapMessageToDto(m, usersById, replyToById)).ToList();
        var nextCursor = hasMore && items.Any() ? items.Last().Id : null;

        return Result<MessageListDto>.Success(new MessageListDto
        {
            Items = items,
            TotalCount = totalCount,
            HasMore = hasMore,
            NextCursor = nextCursor
        });
    }

    public async Task<Result<ChatMessageDto>> SendMessageAsync(
        string userId,
        SendMessageDto dto,
        CancellationToken ct = default)
    {
        // Validate membership
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == dto.ConversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<ChatMessageDto>.Forbidden("You are not a member of this conversation");

        // Validate message length
        const int MaxMessageLength = 10000;
        if (dto.Message.Length > MaxMessageLength)
        {
            return Result<ChatMessageDto>.ValidationError($"Message cannot exceed {MaxMessageLength} characters");
        }

        // Validate reply target if specified
        if (!string.IsNullOrEmpty(dto.ReplyToId))
        {
            var replyTarget = await _messageRepository.FindOneAsync(
                m => m.id == dto.ReplyToId && m.conversationId == dto.ConversationId, ct);

            if (replyTarget is null)
                return Result<ChatMessageDto>.NotFound("Reply target message not found in this conversation");
        }

        // Get conversation to find recipient (for userToId - legacy field)
        var conversation = await _conversationRepository.GetByIdAsync(dto.ConversationId, ct);
        if (conversation is null)
            return Result<ChatMessageDto>.NotFound("Conversation not found");

        var members = await _memberRepository.FindAsync(m => m.conversationId == dto.ConversationId, ct);

        // For userToId, use first other member (legacy support)
        var recipientId = members.FirstOrDefault(m => m.userId != userId)?.userId ?? userId;

        var now = DateTime.UtcNow;
        var message = new ChatMessage
        {
            id = Guid.NewGuid().ToString(),
            conversationId = dto.ConversationId,
            userFromId = userId,
            userToId = recipientId,
            message = dto.Message,
            status = MessageStatus.SENT,
            sentAt = now,
            replyToId = dto.ReplyToId
        };

        await _messageRepository.AddAsync(message, ct);

        // Update conversation's lastMessageAt
        conversation.lastMessageAt = now;
        conversation.updatedAt = now;
        await _conversationRepository.UpdateAsync(conversation, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        // Load user info for response
        var user = await _userRepository.GetByIdAsync(userId, ct);

        ChatMessage? replyTo = null;
        User? replyToUser = null;
        if (!string.IsNullOrEmpty(dto.ReplyToId))
        {
            replyTo = await _messageRepository.GetByIdAsync(dto.ReplyToId, ct);
            if (replyTo is not null)
                replyToUser = await _userRepository.GetByIdAsync(replyTo.userFromId, ct);
        }

        return Result<ChatMessageDto>.Success(new ChatMessageDto
        {
            Id = message.id,
            ConversationId = message.conversationId!,
            UserFromId = message.userFromId,
            UserFromName = user?.name ?? user?.email ?? "Unknown",
            UserFromImage = user?.image,
            Message = message.message,
            Status = message.status,
            ReplyToId = message.replyToId,
            ReplyTo = replyTo is not null ? new ChatMessageDto
            {
                Id = replyTo.id,
                ConversationId = replyTo.conversationId ?? string.Empty,
                UserFromId = replyTo.userFromId,
                UserFromName = replyToUser?.name ?? replyToUser?.email ?? "Unknown",
                UserFromImage = replyToUser?.image,
                Message = replyTo.isDeleted ? null : replyTo.message,
                Status = replyTo.status,
                ReplyToId = replyTo.replyToId,
                ReplyTo = null,
                SentAt = replyTo.sentAt,
                DeliveredAt = replyTo.deliveredAt,
                SeenAt = replyTo.seenAt,
                EditedAt = replyTo.editedAt,
                IsDeleted = replyTo.isDeleted
            } : null,
            SentAt = message.sentAt,
            DeliveredAt = message.deliveredAt,
            SeenAt = message.seenAt,
            EditedAt = message.editedAt,
            IsDeleted = message.isDeleted
        });
    }

    public async Task<Result<ChatMessageDto>> UpdateMessageAsync(
        string messageId,
        string requestingUserId,
        UpdateMessageDto dto,
        CancellationToken ct = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, ct);
        if (message is null)
            return Result<ChatMessageDto>.NotFound($"Message with id {messageId} not found");

        if (message.userFromId != requestingUserId)
            return Result<ChatMessageDto>.Forbidden("You can only edit your own messages");

        if (message.isDeleted)
            return Result<ChatMessageDto>.ValidationError("Cannot edit a deleted message");

        message.message = dto.Message;
        message.editedAt = DateTime.UtcNow;

        await _messageRepository.UpdateAsync(message, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var user = await _userRepository.GetByIdAsync(message.userFromId, ct);
        var usersById = user is not null ? new Dictionary<string, User> { { user.id, user } } : new Dictionary<string, User>();

        return Result<ChatMessageDto>.Success(MapMessageToDto(message, usersById, new Dictionary<string, ChatMessage>()));
    }

    public async Task<Result<bool>> DeleteMessageAsync(
        string messageId,
        string requestingUserId,
        CancellationToken ct = default)
    {
        var message = await _messageRepository.GetByIdAsync(messageId, ct);
        if (message is null)
            return Result<bool>.NotFound($"Message with id {messageId} not found");

        if (message.userFromId != requestingUserId)
            return Result<bool>.Forbidden("You can only delete your own messages");

        // Soft delete
        message.isDeleted = true;
        message.message = null;

        await _messageRepository.UpdateAsync(message, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> MarkMessagesDeliveredAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default)
    {
        // Validate membership
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<bool>.Forbidden("You are not a member of this conversation");

        var now = DateTime.UtcNow;

        // Update all undelivered messages not from this user
        var undeliveredMessages = await _messageRepository.FindAsync(
            m => m.conversationId == conversationId &&
                 m.userFromId != userId &&
                 m.deliveredAt == null &&
                 !m.isDeleted, ct);

        foreach (var message in undeliveredMessages)
        {
            message.deliveredAt = now;
            if (message.status == MessageStatus.SENT)
                message.status = MessageStatus.DELIVERED;
            await _messageRepository.UpdateAsync(message, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> MarkMessagesReadAsync(
        string userId,
        MarkMessagesReadDto dto,
        CancellationToken ct = default)
    {
        // Validate membership
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == dto.ConversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<bool>.Forbidden("You are not a member of this conversation");

        var now = DateTime.UtcNow;

        // Update lastReadAt for the member
        membership.lastReadAt = dto.ReadUpTo;
        membership.lastActiveAt = now;
        await _memberRepository.UpdateAsync(membership, ct);

        // Update message statuses to READ
        var unreadMessages = await _messageRepository.FindAsync(
            m => m.conversationId == dto.ConversationId &&
                 m.userFromId != userId &&
                 m.sentAt <= dto.ReadUpTo &&
                 m.status != MessageStatus.READ &&
                 !m.isDeleted, ct);

        foreach (var message in unreadMessages)
        {
            message.seenAt = now;
            message.status = MessageStatus.READ;
            await _messageRepository.UpdateAsync(message, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    // ========== Presence Operations ==========

    public async Task<Result<bool>> UpdatePresenceAsync(
        string conversationId,
        string userId,
        CancellationToken ct = default)
    {
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == conversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<bool>.Forbidden("You are not a member of this conversation");

        membership.lastActiveAt = DateTime.UtcNow;
        await _memberRepository.UpdateAsync(membership, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> SendTypingIndicatorAsync(
        string userId,
        TypingIndicatorDto dto,
        CancellationToken ct = default)
    {
        // Validate membership
        var membership = await _memberRepository.FindOneAsync(
            m => m.conversationId == dto.ConversationId && m.userId == userId, ct);

        if (membership is null)
            return Result<bool>.Forbidden("You are not a member of this conversation");

        // Note: The actual typing indicator broadcast happens via SignalR hub
        // This method validates the user can send typing indicators
        return Result<bool>.Success(true);
    }

    // ========== Private Helper Methods ==========

    private static ConversationDto MapConversationToDto(
        Conversation conversation,
        IReadOnlyList<ConversationMember> members,
        Dictionary<string, User> usersById,
        ChatMessage? lastMessage,
        User? lastMessageSender,
        string requestingUserId)
    {
        var memberDtos = members.Select(m =>
        {
            var user = usersById.GetValueOrDefault(m.userId);
            return new ConversationMemberDto
            {
                Id = m.id,
                UserId = m.userId,
                UserName = user?.name ?? user?.email ?? "Unknown",
                UserImage = user?.image,
                Role = m.role,
                LastReadAt = m.lastReadAt,
                LastActiveAt = m.lastActiveAt,
                JoinedAt = m.joinedAt,
                IsOnline = false // Would need presence service for real-time status
            };
        }).ToList();

        ChatMessageDto? lastMessageDto = lastMessage is not null
            ? new ChatMessageDto
            {
                Id = lastMessage.id,
                ConversationId = lastMessage.conversationId ?? string.Empty,
                UserFromId = lastMessage.userFromId,
                UserFromName = lastMessageSender?.name ?? lastMessageSender?.email ?? "Unknown",
                UserFromImage = lastMessageSender?.image,
                Message = lastMessage.isDeleted ? null : lastMessage.message,
                Status = lastMessage.status,
                ReplyToId = lastMessage.replyToId,
                ReplyTo = null,
                SentAt = lastMessage.sentAt,
                DeliveredAt = lastMessage.deliveredAt,
                SeenAt = lastMessage.seenAt,
                EditedAt = lastMessage.editedAt,
                IsDeleted = lastMessage.isDeleted
            }
            : null;

        // Calculate unread count for requesting user
        var userMembership = members.FirstOrDefault(m => m.userId == requestingUserId);
        var unreadCount = 0;
        // Note: Unread count would need message data which we don't have here
        // In a production app, this would be calculated via a separate query or cached

        return new ConversationDto
        {
            Id = conversation.id,
            Type = conversation.type,
            Name = conversation.type == ConversationType.DIRECT
                ? GetDirectConversationName(members, usersById, requestingUserId)
                : conversation.name,
            AvatarUrl = conversation.type == ConversationType.DIRECT
                ? GetDirectConversationAvatar(members, usersById, requestingUserId)
                : conversation.avatarUrl,
            CreatedAt = conversation.createdAt,
            LastMessageAt = conversation.lastMessageAt,
            Members = memberDtos,
            LastMessage = lastMessageDto,
            UnreadCount = unreadCount
        };
    }

    private static string GetDirectConversationName(
        IReadOnlyList<ConversationMember> members,
        Dictionary<string, User> usersById,
        string requestingUserId)
    {
        var otherMember = members.FirstOrDefault(m => m.userId != requestingUserId);
        if (otherMember is null)
        {
            // Self-conversation - show own name
            var selfUser = usersById.GetValueOrDefault(requestingUserId);
            return selfUser?.name ?? selfUser?.email ?? "Notes";
        }
        var user = usersById.GetValueOrDefault(otherMember.userId);
        return user?.name ?? user?.email ?? "Unknown User";
    }

    private static string? GetDirectConversationAvatar(
        IReadOnlyList<ConversationMember> members,
        Dictionary<string, User> usersById,
        string requestingUserId)
    {
        var otherMember = members.FirstOrDefault(m => m.userId != requestingUserId);
        if (otherMember is null)
        {
            // Self-conversation - show own avatar
            var selfUser = usersById.GetValueOrDefault(requestingUserId);
            return selfUser?.image;
        }
        var user = usersById.GetValueOrDefault(otherMember.userId);
        return user?.image;
    }

    private static ChatMessageDto MapMessageToDto(
        ChatMessage message,
        Dictionary<string, User> usersById,
        Dictionary<string, ChatMessage> replyToById)
    {
        var user = usersById.GetValueOrDefault(message.userFromId);

        ChatMessageDto? replyToDto = null;
        if (!string.IsNullOrEmpty(message.replyToId) && replyToById.TryGetValue(message.replyToId, out var replyTo))
        {
            var replyToUser = usersById.GetValueOrDefault(replyTo.userFromId);
            replyToDto = new ChatMessageDto
            {
                Id = replyTo.id,
                ConversationId = replyTo.conversationId ?? string.Empty,
                UserFromId = replyTo.userFromId,
                UserFromName = replyToUser?.name ?? replyToUser?.email ?? "Unknown",
                UserFromImage = replyToUser?.image,
                Message = replyTo.isDeleted ? null : replyTo.message,
                Status = replyTo.status,
                ReplyToId = replyTo.replyToId,
                ReplyTo = null,
                SentAt = replyTo.sentAt,
                DeliveredAt = replyTo.deliveredAt,
                SeenAt = replyTo.seenAt,
                EditedAt = replyTo.editedAt,
                IsDeleted = replyTo.isDeleted
            };
        }

        return new ChatMessageDto
        {
            Id = message.id,
            ConversationId = message.conversationId ?? string.Empty,
            UserFromId = message.userFromId,
            UserFromName = user?.name ?? user?.email ?? "Unknown",
            UserFromImage = user?.image,
            Message = message.isDeleted ? null : message.message,
            Status = message.status,
            ReplyToId = message.replyToId,
            ReplyTo = replyToDto,
            SentAt = message.sentAt,
            DeliveredAt = message.deliveredAt,
            SeenAt = message.seenAt,
            EditedAt = message.editedAt,
            IsDeleted = message.isDeleted
        };
    }
}
