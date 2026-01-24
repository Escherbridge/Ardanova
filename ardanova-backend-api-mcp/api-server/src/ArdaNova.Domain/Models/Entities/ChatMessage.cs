using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ChatMessage")]
public class ChatMessage
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userToId { get; set; }

    [Required]
    public string userFromId { get; set; }

    public string? message { get; set; }

    [Required]
    public MessageStatus status { get; set; }

    public string? ChatReaction { get; set; }

    public string? chatAttachmentId { get; set; }

    [Required]
    public DateTime sentAt { get; set; }

    public DateTime? seenAt { get; set; }

    [ForeignKey("userToId")]
    public virtual User UserTo { get; set; }

    [ForeignKey("userFromId")]
    public virtual User UserFrom { get; set; }

    [ForeignKey("chatAttachmentId")]
    public virtual Attachment ChatAttachment { get; set; }

}
