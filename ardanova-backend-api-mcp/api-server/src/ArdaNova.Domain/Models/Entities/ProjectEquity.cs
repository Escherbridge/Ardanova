using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProjectEquity")]
public class ProjectEquity
{
    [Key]
    public string id { get; set; }

    [Required]
    public string projectId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public decimal sharePercent { get; set; }

    [Required]
    public decimal investmentAmount { get; set; }

    [Required]
    public DateTime grantedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project Project { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
