namespace ArdaNova.Application.Tests.Mappings;

using ArdaNova.Application.DTOs;
using ArdaNova.Application.Mappings;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;

/// <summary>
/// The producer of <c>IsIndependentlyVerifiable</c>.
///
/// The flag shipped with ZERO producers: the member-facing card read it, nothing published it,
/// so the card's proof state was hard-off rather than gated — a control with no input. Wave L0
/// gave it a producer that read <c>mintTxHash</c>. That was exclusive of the CODE and wrong about
/// the DATA: the removed address-based path wrote the same column from
/// <c>MintSoulboundASAAsync(_platformAddress, …)</c>, and the simulated provider fabricated
/// <c>assetId</c>, so every row predating the node path mapped to "Verified" over a credential
/// that was never issued to anybody.
///
/// It is now read off <c>issuedThroughNodeAt</c>, a column of its own, written in exactly one
/// place (<c>MembershipCredentialServices.UpdateMintInfoAsync</c>) and only when the caller
/// states <c>CredentialProofOrigin.IssuanceNode</c> — whose sole production caller is
/// <c>CredentialUtilityService.IssueThroughNodeAsync</c>. The legacy shape below is the case
/// that suite previously had no test for.
/// </summary>
public sealed class CredentialIssuancePathMappingTests
{
    [Fact]
    public void A_node_issuance_is_what_makes_a_credential_independently_verifiable()
    {
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            issuedThroughNodeAt = new DateTime(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc),
            mintTxHash = "issuance-reference-1",
            assetId = "proof-reference-1",
        });

        dto.IsIndependentlyVerifiable.Should().BeTrue();
    }

    [Fact]
    public void The_legacy_platform_held_mint_shape_does_not_read_as_verifiable()
    {
        // A row written by the removed address-based mint: a real transaction hash AND a real
        // asset id, for an asset that sits at the PLATFORM's address. It is indistinguishable
        // from a node issuance by identifier presence alone, which is why the flag no longer
        // looks at identifiers. Without this case the suite was green over the exact rows the
        // producer got wrong.
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            mintTxHash = "PLATFORMHELDTXHASHAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            assetId = "1234567",
            mintedAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            issuedThroughNodeAt = null,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse(
            "an asset minted into the platform's own hands proves nothing about the member, "
            + "however real its transaction hash is");
    }

    [Fact]
    public void A_fabricated_identifier_alone_does_not_make_a_credential_verifiable()
    {
        // Exactly what the simulated provider left behind: an asset id and nothing the
        // issuance node ever returned.
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            assetId = "fabricated-12345",
            mintTxHash = null,
            issuedThroughNodeAt = null,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse(
            "the presence of a stored identifier is not evidence that anything was issued");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_issuance_reference_without_a_node_stamp_reads_as_not_verifiable(string? reference)
    {
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            mintTxHash = reference,
            issuedThroughNodeAt = null,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse();
    }

    [Fact]
    public void A_regrant_of_a_withdrawn_credential_goes_back_to_not_verifiable()
    {
        // MembershipCredentialServices clears the issuance stamp along with both identifier
        // columns when a REVOKED credential is re-granted, so the flag must fall back to false
        // until the node issues again.
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            assetId = null,
            mintTxHash = null,
            issuedThroughNodeAt = null,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse();
    }

    private static MembershipCredentialDto Map(MembershipCredential credential)
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return configuration.CreateMapper().Map<MembershipCredentialDto>(credential);
    }
}
