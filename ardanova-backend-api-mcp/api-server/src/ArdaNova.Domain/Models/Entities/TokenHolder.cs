using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("TokenHolder")]
public class TokenHolder
{
    [Key]
    public string id { get; set; }

    [Required]
    public string tokenId { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public decimal balance { get; set; }

    [Required]
    public decimal stakedAmount { get; set; }

    [Required]
    public decimal lockedAmount { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("tokenId")]
    public virtual ProjectToken Token { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

    public virtual ICollection<TokenVesting> TokenVestings { get; set; } = new List<TokenVesting>();

}
