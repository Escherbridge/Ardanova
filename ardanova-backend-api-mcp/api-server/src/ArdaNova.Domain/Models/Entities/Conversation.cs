using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Conversation")]
public class Conversation
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public ConversationType type { get; set; }

    public string? name { get; set; }

    public string? avatarUrl { get; set; }

    public string? createdById { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    public DateTime? lastMessageAt { get; set; }

    [ForeignKey("createdById")]
    public virtual User? CreatedBy { get; set; }

    public virtual ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

}
