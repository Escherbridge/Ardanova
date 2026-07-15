using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("StripeWebhookEvent")]
public class StripeWebhookEvent
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string eventType { get; set; } = string.Empty;

    [Required]
    public StripeWebhookEventStatus status { get; set; }

    [Required]
    public int attemptCount { get; set; }

    [Required]
    public DateTime receivedAt { get; set; }

    [Required]
    public DateTime processingLeaseExpiresAt { get; set; }

    public DateTime? completedAt { get; set; }

    public DateTime? lastFailedAt { get; set; }

}
