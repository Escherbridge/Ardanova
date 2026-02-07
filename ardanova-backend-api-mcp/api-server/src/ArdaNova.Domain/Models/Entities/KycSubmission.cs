using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("KycSubmission")]
public class KycSubmission
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public KycProvider provider { get; set; }

    [Required]
    public KycStatus status { get; set; }

    public string? reviewerId { get; set; }

    [Column(TypeName = "text")]
    public string? reviewNotes { get; set; }

    public string? rejectionReason { get; set; }

    public string? providerSessionId { get; set; }

    [Column(TypeName = "text")]
    public string? providerResult { get; set; }

    [Required]
    public DateTime submittedAt { get; set; }

    public DateTime? reviewedAt { get; set; }

    public DateTime? expiresAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("userId")]
    [InverseProperty("KycSubmissionsAsUser")]
    public virtual User? User { get; set; }

    [ForeignKey("reviewerId")]
    [InverseProperty("KycSubmissionsAsReviewer")]
    public virtual User? Reviewer { get; set; }

    public virtual ICollection<KycDocument> KycDocuments { get; set; } = new List<KycDocument>();

}
