using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("KycDocument")]
public class KycDocument
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string submissionId { get; set; } = string.Empty;

    [Required]
    public KycDocumentType type { get; set; }

    [Required]
    public string fileUrl { get; set; } = string.Empty;

    [Required]
    public string fileName { get; set; } = string.Empty;

    public string? mimeType { get; set; }

    public int? fileSizeBytes { get; set; }

    [Column(TypeName = "text")]
    public string? metadata { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("submissionId")]
    public virtual KycSubmission? Submission { get; set; }

}
