namespace ArdaNova.Application.Tests.Policies;

using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using FluentAssertions;

public class TokenLiquidityPolicyTests
{
    [Theory]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.ACTIVE, true)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.CONTRIBUTOR, ProjectGateStatus.FAILED, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.ACTIVE, false)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.INVESTOR, ProjectGateStatus.FAILED, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.FUNDING, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.ACTIVE, false)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.SUCCEEDED, true)]
    [InlineData(TokenHolderClass.FOUNDER, ProjectGateStatus.FAILED, false)]
    public void IsLiquid_UsesTheCanonicalHolderClassAndGateMatrix(
        TokenHolderClass holderClass,
        ProjectGateStatus gateStatus,
        bool expected)
    {
        TokenLiquidityPolicy.IsLiquid(holderClass, gateStatus).Should().Be(expected);
    }
}
