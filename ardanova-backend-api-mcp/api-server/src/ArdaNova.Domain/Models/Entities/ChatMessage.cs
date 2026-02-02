using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ChatMessage")]
public class ChatMessage
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userToId { get; set; } = string.Empty;

    [Required]
    public string userFromId { get; set; } = string.Empty;

    public string? message { get; set; }

    [Required]
    public MessageStatus status { get; set; }

    public string? ChatReaction { get; set; }

    public string? chatAttachmentId { get; set; }

    [Required]
    public DateTime sentAt { get; set; }

    public DateTime? seenAt { get; set; }

    public string? conversationId { get; set; }

    public string? replyToId { get; set; }

    public DateTime? deliveredAt { get; set; }

    [Required]
    public bool isDeleted { get; set; }

    public DateTime? editedAt { get; set; }

    [ForeignKey("userToId")]
    [InverseProperty("ChatMessagesAsUserTo")]
    public virtual User? UserTo { get; set; }

    [ForeignKey("userFromId")]
    [InverseProperty("ChatMessagesAsUserFrom")]
    public virtual User? UserFrom { get; set; }

    [ForeignKey("chatAttachmentId")]
    public virtual Attachment? ChatAttachment { get; set; }

    [ForeignKey("conversationId")]
    public virtual Conversation? Conversation { get; set; }

    [ForeignKey("replyToId")]
    public virtual ChatMessage? ReplyTo { get; set; }

}
