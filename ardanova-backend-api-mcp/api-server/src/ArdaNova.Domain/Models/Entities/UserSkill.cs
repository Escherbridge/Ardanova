using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserSkill")]
public class UserSkill
{
    [Key]
    public string id { get; set; }

    [Required]
    public string userId { get; set; }

    [Required]
    public string skill { get; set; }

    [Required]
    public int level { get; set; }

    [ForeignKey("userId")]
    public virtual User User { get; set; }

}
