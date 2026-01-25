using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("Session")]
public class Session
{
    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string sessionToken { get; set; } = string.Empty;

    [Required]
    public string userId { get; set; } = string.Empty;

    [Required]
    public DateTime expires { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

}
