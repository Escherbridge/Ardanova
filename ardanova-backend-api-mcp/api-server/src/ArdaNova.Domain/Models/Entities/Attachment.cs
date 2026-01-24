using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Attachment")]
public class Attachment
{
    [Key]
    public string id { get; set; }

    [Required]
    public string uploadedById { get; set; }

    public string? bucketPath { get; set; }

    [Required]
    public MimeType type { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    public DateTime? lastUsedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    [ForeignKey("uploadedById")]
    public virtual User UploadedBy { get; set; }

}
