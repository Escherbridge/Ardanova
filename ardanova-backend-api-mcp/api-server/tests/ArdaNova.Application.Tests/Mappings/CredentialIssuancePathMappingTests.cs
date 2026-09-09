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
/// The flag shipped with ZERO producers: the member-facing card read it, nothing published
/// it, so the card's proof state was hard-off rather than gated — a control with no input.
/// It is now derived here, from <c>mintTxHash</c>, which is written in exactly one place
/// (<c>MembershipCredentialServices.UpdateMintInfoAsync</c>) whose only production caller is
/// <c>CredentialUtilityService.IssueThroughNodeAsync</c>, where it carries the issuance
/// node's own <c>IssuanceReference</c>.
///
/// Never from <c>assetId</c>: the shipped simulated provider fabricated one outright.
/// </summary>
public sealed class CredentialIssuancePathMappingTests
{
    [Fact]
    public void An_issuance_reference_is_what_makes_a_credential_independently_verifiable()
    {
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            mintTxHash = "issuance-reference-1",
            assetId = "proof-reference-1",
        });

        dto.IsIndependentlyVerifiable.Should().BeTrue();
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
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse(
            "the presence of a stored identifier is not evidence that anything was issued");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_absent_or_blank_issuance_reference_reads_as_not_verifiable(string? reference)
    {
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            mintTxHash = reference,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse();
    }

    [Fact]
    public void A_regrant_of_a_withdrawn_credential_goes_back_to_not_verifiable()
    {
        // MembershipCredentialServices clears both columns when a REVOKED credential is
        // re-granted, so the flag must fall back to false until the node issues again.
        var dto = Map(new MembershipCredential
        {
            id = "cred-1",
            userId = "user-1",
            status = MembershipCredentialStatus.ACTIVE,
            grantedVia = MembershipGrantType.FOUNDER,
            assetId = null,
            mintTxHash = null,
        });

        dto.IsIndependentlyVerifiable.Should().BeFalse();
    }

    private static MembershipCredentialDto Map(MembershipCredential credential)
    {
        var configuration = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return configuration.CreateMapper().Map<MembershipCredentialDto>(credential);
    }
}
