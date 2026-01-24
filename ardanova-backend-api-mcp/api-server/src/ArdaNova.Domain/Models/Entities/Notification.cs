using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Notification")]
public class Notification
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public NotificationType type { get; set; }

    [Required]
    public string title { get; set; }

    [Required]
    public string message { get; set; }

    public string? data { get; set; }

    [Required]
    public bool isRead { get; set; }

    public DateTime? readAt { get; set; }

    public string? actionUrl { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
