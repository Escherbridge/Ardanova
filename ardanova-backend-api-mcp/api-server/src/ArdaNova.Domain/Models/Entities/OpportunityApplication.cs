using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("OpportunityApplication")]
public class OpportunityApplication
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string opportunityId { get; set; } = string.Empty;

    [Required]
    public string applicantId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string coverLetter { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string? portfolio { get; set; }

    [Column(TypeName = "text")]
    public string? additionalInfo { get; set; }

    [Required]
    public ApplicationStatus status { get; set; }

    [Column(TypeName = "text")]
    public string? reviewNotes { get; set; }

    [Required]
    public DateTime appliedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    [ForeignKey("opportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [ForeignKey("applicantId")]
    public virtual User? Applicant { get; set; }

}
