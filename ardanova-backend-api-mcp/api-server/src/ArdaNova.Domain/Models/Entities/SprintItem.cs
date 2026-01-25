using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("SprintItem")]
public class SprintItem
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string sprintId { get; set; } = string.Empty;

    [Required]
    public string taskId { get; set; } = string.Empty;

    [Required]
    public int order { get; set; }

    [Required]
    public DateTime addedAt { get; set; }

    [ForeignKey("sprintId")]
    public virtual Sprint? Sprint { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask? Task { get; set; }

}
