// Generated from database-architecture.dbml by generate:csharp - do not edit; hand-written members go in ConversationMember.Custom.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ConversationMember")]
public partial class ConversationMember
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string conversationId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public ConversationRole role { get; set; }

    public DateTime? lastReadAt { get; set; }

    [Required]
    public DateTime joinedAt { get; set; }

    public DateTime? lastActiveAt { get; set; }

    [ForeignKey("conversationId")]
    public virtual Conversation? Conversation { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
