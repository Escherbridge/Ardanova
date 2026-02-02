using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("ShareHolder")]
public class ShareHolder
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string shareId { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    [Precision(18, 8)]
    public decimal balance { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal stakedAmount { get; set; }

    [Required]
    [Precision(18, 8)]
    public decimal lockedAmount { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("shareId")]
    public virtual ProjectShare? Share { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    public virtual ICollection<ShareVesting> ShareVestings { get; set; } = new List<ShareVesting>();

}
