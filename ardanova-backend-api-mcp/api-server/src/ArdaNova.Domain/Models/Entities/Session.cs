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
    public string id { get; set; }

    [Required]
    public string sessionToken { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public DateTime expires { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
