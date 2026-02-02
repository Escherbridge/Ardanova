using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(taskId), IsUnique = true)]
[Table("TaskCompensation")]
public class TaskCompensation
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public CompensationModel compensationModel { get; set; }

    [Precision(18, 8)]
    public decimal? shareAmount { get; set; }

    [Precision(18, 8)]
    public decimal? hourlyRate { get; set; }

    [Precision(18, 8)]
    public decimal? equityPercent { get; set; }

    [Precision(18, 8)]
    public decimal? stableCoinAmount { get; set; }

    public int? vestingMonths { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

}
