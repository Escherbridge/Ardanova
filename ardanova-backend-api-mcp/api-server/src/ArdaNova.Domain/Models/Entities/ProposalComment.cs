using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ProposalComment")]
public class ProposalComment
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string proposalId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string content { get; set; } = string.Empty;

    public string? parentId { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("proposalId")]
    public virtual Proposal? Proposal { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("parentId")]
    public virtual ProposalComment? Parent { get; set; }

}
