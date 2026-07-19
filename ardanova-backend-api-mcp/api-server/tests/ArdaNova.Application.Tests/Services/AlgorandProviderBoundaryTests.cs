namespace ArdaNova.Application.Tests.Services;

using ArdaNova.Application.DTOs;
using ArdaNova.Infrastructure.Algorand;
using FluentAssertions;

public class AlgorandProviderBoundaryTests
{
    [Theory]
    [InlineData("Legacy", AlgorandProvider.Legacy)]
    [InlineData("AZOA", AlgorandProvider.Azoa)]
    [InlineData("simulated", AlgorandProvider.Simulated)]
    public void Parse_AllowListsKnownProviders(string value, AlgorandProvider expected)
        => AlgorandProviderSelection.Parse(value).Should().Be(expected);

    [Fact]
    public void Parse_RejectsUnknownProvider()
        => FluentActions.Invoking(() => AlgorandProviderSelection.Parse("typo"))
            .Should().Throw<InvalidOperationException>().WithMessage("*Unknown Algorand:Provider*");

    [Fact]
    public async Task SimulatedProvider_IsDeterministicAndDoesNotRequireChainConfiguration()
    {
        var service = new SimulatedAlgorandService();
        var metadata = new CredentialMetadataInput
        {
            CredentialId = "credential-1",
            Scope = "PROJECT",
            ScopeId = "project-1",
            ScopeName = "Project One",
            UserId = "user-1",
            GrantedVia = "FOUNDER",
            GrantedAt = new DateTime(2026, 7, 18, 0, 0, 0, DateTimeKind.Utc),
        };

        var first = await service.MintSoulboundASAAsync("address-1", metadata);
        var replay = await service.MintSoulboundASAAsync("address-1", metadata);

        first.IsSuccess.Should().BeTrue();
        replay.Value.Should().Be(first.Value);
        first.Value!.AssetId.Should().StartWith("sim:credential-asset:");
        (await service.VerifyOwnershipAsync(first.Value.AssetId, "address-1")).Value.Should().BeTrue();
    }
}
