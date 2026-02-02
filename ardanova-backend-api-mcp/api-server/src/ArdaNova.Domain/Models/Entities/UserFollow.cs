using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("UserFollow")]
public class UserFollow
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    [Required]
    public string followerId { get; set; } = string.Empty;

    [Required]
    public string followingId { get; set; } = string.Empty;

    [Required]
    public DateTime createdAt { get; set; }

    [ForeignKey("followerId")]
    [InverseProperty("UserFollowsAsFollower")]
    public virtual User? Follower { get; set; }

    [ForeignKey("followingId")]
    [InverseProperty("UserFollowsAsFollowing")]
    public virtual User? Following { get; set; }

}
