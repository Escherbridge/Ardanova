using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(slug), IsUnique = true)]
[Table("Event")]
public class Event
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string title { get; set; } = string.Empty;

    [Required]
    public string slug { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? description { get; set; }

    [Required]
    public EventType type { get; set; }

    [Required]
    public EventVisibility visibility { get; set; }

    [Required]
    public EventStatus status { get; set; }

    public string? location { get; set; }

    public string? locationUrl { get; set; }

    [Required]
    public bool isOnline { get; set; }

    public string? meetingUrl { get; set; }

    [Required]
    public string timezone { get; set; } = string.Empty;

    [Required]
    public DateTime startDate { get; set; }

    [Required]
    public DateTime endDate { get; set; }

    public int? maxAttendees { get; set; }

    [Required]
    public int attendeesCount { get; set; }

    public string? coverImage { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [Required]
    public string organizerId { get; set; } = string.Empty;

    public string? projectId { get; set; }

    public string? guildId { get; set; }

    [ForeignKey("organizerId")]
    public virtual User? Organizer { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    public virtual ICollection<EventAttendee> EventAttendees { get; set; } = new List<EventAttendee>();

    public virtual ICollection<EventCoHost> EventCoHosts { get; set; } = new List<EventCoHost>();

    public virtual ICollection<EventReminder> EventReminders { get; set; } = new List<EventReminder>();

}
