using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Notification")]
public class Notification
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public NotificationType type { get; set; }

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string message { get; set; } = string.Empty;

    public string? data { get; set; }

    [Required]
    public bool isRead { get; set; }

    public DateTime? readAt { get; set; }

    public string? actionUrl { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
