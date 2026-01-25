using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TaskCompensation")]
public class TaskCompensation
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public CompensationModel model { get; set; }

    public decimal? tokenAmount { get; set; }

    public decimal? hourlyRate { get; set; }

    public decimal? equityPercent { get; set; }

    public decimal? stableCoinAmount { get; set; }

    public int? vestingMonths { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

}
