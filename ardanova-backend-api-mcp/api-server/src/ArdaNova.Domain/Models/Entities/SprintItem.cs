using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("SprintItem")]
public class SprintItem
{
    [Key]
    public string id { get; set; }

    [Required]
    public string sprintId { get; set; }

    [Required]
    public string taskId { get; set; }

    [Required]
    public int order { get; set; }

    [Required]
    public DateTime addedAt { get; set; }

    [ForeignKey("sprintId")]
    public virtual Sprint Sprint { get; set; }

    [ForeignKey("taskId")]
    public virtual ProjectTask Task { get; set; }

}
