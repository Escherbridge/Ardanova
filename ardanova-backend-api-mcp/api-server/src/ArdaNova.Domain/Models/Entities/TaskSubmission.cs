using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TaskSubmission")]
public class TaskSubmission
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public string submittedById { get; set; } = string.Empty;

    [Required]
    public string content { get; set; } = string.Empty;

    public string? attachments { get; set; }

    [Required]
    public SubmissionStatus status { get; set; }

    public string? reviewedById { get; set; }

    public string? feedback { get; set; }

    [Required]
    public DateTime submittedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

    [ForeignKey("submittedById")]
    public virtual User? SubmittedBy { get; set; }

    [ForeignKey("reviewedById")]
    public virtual User? ReviewedBy { get; set; }

}
