using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("MembershipCredential")]
public class MembershipCredential
{

    [Key]
    [Required]
    public string id { get; set; } = string.Empty;

    public string? projectId { get; set; }

    public string? guildId { get; set; }

    [Required]
    public string userId { get; set; } = string.Empty;

    public string? assetId { get; set; }

    [Required]
    public MembershipCredentialStatus status { get; set; }

    [Required]
    public bool isTransferable { get; set; }

    public UserTier? tier { get; set; }

    [Required]
    public MembershipGrantType grantedVia { get; set; }

    public string? grantedByProposalId { get; set; }

    public string? metadataUri { get; set; }

    public string? mintTxHash { get; set; }

    /// <summary>
    /// When the credential ISSUANCE NODE issued this credential to its holder. Its OWN column,
    /// not derived from mintTxHash or assetId: both of those were also written by the removed
    /// address-based mint (which put the asset in the PLATFORM's hands) and fabricated outright
    /// by the simulated provider, so a row predating the node path carries them and proves
    /// nothing. Null on every such row, which is the fail-closed reading. Written in exactly one
    /// place -- MembershipCredentialServices.UpdateMintInfoAsync, and only when the caller states
    /// CredentialProofOrigin.IssuanceNode.
    /// </summary>
    public DateTime? issuedThroughNodeAt { get; set; }

    public string? revokeTxHash { get; set; }

    public DateTime? mintedAt { get; set; }

    public DateTime? revokedAt { get; set; }

    [Required]
    public DateTime createdAt { get; set; }

    [Required]
    public DateTime updatedAt { get; set; }

    [ForeignKey("projectId")]
    public virtual Project? Project { get; set; }

    [ForeignKey("guildId")]
    public virtual Guild? Guild { get; set; }

    [ForeignKey("userId")]
    public virtual User? User { get; set; }

    [ForeignKey("grantedByProposalId")]
    public virtual Proposal? GrantedByProposal { get; set; }

}
