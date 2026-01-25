using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Index(nameof(token), IsUnique = true)]
[Table("VerificationToken")]
public class VerificationToken
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string identifier { get; set; } = string.Empty;

    [Required]
    public string token { get; set; } = string.Empty;

    [Required]
    public DateTime expires { get; set; }

    [ForeignKey("identifier")]
    public virtual User? User { get; set; }

}
