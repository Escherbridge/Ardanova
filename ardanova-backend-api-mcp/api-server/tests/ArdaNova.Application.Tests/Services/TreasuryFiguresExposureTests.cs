namespace ArdaNova.Application.Tests.Services;

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using ArdaNova.Application.Common.Interfaces;
using ArdaNova.Application.Common.Results;
using ArdaNova.Application.DTOs;
using ArdaNova.Application.Services.Implementations;
using ArdaNova.Application.Services.Interfaces;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;

/// <summary>
/// What a project's buy-back figures may say, and to whom. Two rules are under test: the response
/// carries amounts and dates but never the node account the funds sit in, and membership branches
/// the PAYLOAD the way the money page branches it (OI-89).
/// </summary>
/// <remarks>
/// The fixture deliberately CARRIES the address and the balance the stranger must not receive -
/// the node read returns a real-looking treasury account and a non-zero balance, and
/// <see cref="Member_ReceivesTheFundsFigureAndStillNoAddress"/> proves the read reaches them. A
/// withholding test that passed over an empty fixture would be a test of the fixture, not the rule.
/// </remarks>
public sealed class TreasuryFiguresExposureTests
{
    private const string ProjectId = "project-1";
    private const string DraftProjectId = "project-draft";
    private const string ConfigId = "config-1";
    private const string OwnerId = "owner-1";
    private const string MemberId = "member-1";
    private const string StrangerId = "stranger-1";

    /// <summary>The one value this whole file exists to keep off the wire. No word of it is a field name.</summary>
    private const string TreasuryAccountAddress = "XKQ4T7ZP2M9V6WBN3H8LRJ5CDYFA1SGE0UQ7TZ2X";

    private const string PlatformAssetId = "arda-asset-1";
    private const long TreasuryFundsMinorUnits = 250_00L;
    private const string ArdaPerWholeProjectUnit = "2.50";
    private const string UsdPerWholeProjectUnit = "0.025";

    private static readonly DateTimeOffset ObservedAt = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>What the node reports, address included. The stranger's answer must not contain it.</summary>
    private static readonly ProjectTreasuryBalance NodeBalance = new(
        ProjectReference: "project-reference-1",
        ChainType: "algorand",
        Address: TreasuryAccountAddress,
        AssetId: PlatformAssetId,
        BalanceBaseUnits: TreasuryFundsMinorUnits,
        ObservedAt: ObservedAt);

    private static readonly DeclaredProjectUnitRate DeclaredRate = new(
        ProjectTokenConfigId: ConfigId,
        ArdaPerWholeProjectUnit: ArdaPerWholeProjectUnit,
        UsdMajorUnitsPerWholeProjectUnit: UsdPerWholeProjectUnit,
        ProjectUnitDecimals: 2,
        Source: "ardanova-declared",
        DeclaredAt: ObservedAt);

    /// <summary>
    /// The item's acceptance test: no field of the record can carry an account or a rail fact, so
    /// no branch of the service can put one there. Asserted on the TYPE, not on one response.
    /// </summary>
    [Fact]
    public void TheFiguresRecord_CarriesNoAddressFieldAndNoRailFact()
    {
        var propertyNames = typeof(ProjectBuyBackFigures)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();

        propertyNames.Should().NotContain(name => name.Contains("Address", StringComparison.OrdinalIgnoreCase));
        propertyNames.Should().NotContain(name => name.Contains("Chain", StringComparison.OrdinalIgnoreCase));

        // The mirror: the figures a sponsor is actually owed are still on the record.
        propertyNames.Should().Contain(nameof(ProjectBuyBackFigures.TreasuryFundsMinorUnits));
        propertyNames.Should().Contain(nameof(ProjectBuyBackFigures.Disclosure));
    }

    /// <summary>A non-member is quoted the terms, and is sent no read of the project's position.</summary>
    [Fact]
    public async Task NonMember_ReceivesTheTermsWithNoFundsFigureAndNoAddress()
    {
        // The fixture really holds what is being withheld; otherwise this asserts nothing.
        NodeBalance.Address.Should().Be(TreasuryAccountAddress);
        NodeBalance.BalanceBaseUnits.Should().Be(TreasuryFundsMinorUnits);

        var service = BuildService();

        var result = await service.GetBuyBackFiguresAsync(
            ProjectId, new ProjectViewer(StrangerId, IsAdmin: false));

        result.IsSuccess.Should().BeTrue();
        var figures = result.Value!;

        figures.TreasuryFundsVisible.Should().BeFalse();
        figures.TreasuryFundsMinorUnits.Should().BeNull();
        figures.MaxWholeUnitsFillableNow.Should().BeNull();
        figures.ObservedAt.Should().BeNull();
        figures.FundsHiddenNote.Should().Be(BuyBackDisclosures.FundsHiddenFromNonMembers);

        // The terms themselves are public: a sponsor still learns the price and the promise.
        figures.ArdaPerWholeProjectUnit.Should().Be(ArdaPerWholeProjectUnit);
        figures.UsdMajorUnitsPerWholeProjectUnit.Should().Be(UsdPerWholeProjectUnit);
        figures.Disclosure.Should().Be(BuyBackDisclosures.FilledFromProjectFunds);

        AssertCarriesNoAccountOrRailFact(figures);
    }

    /// <summary>
    /// The mirror that keeps the test above honest: a member DOES receive the funds figure, so the
    /// stranger's blank line is a decision about the reader, not an empty node read. The address is
    /// still absent - it is withheld from everyone, not merely from strangers.
    /// </summary>
    [Fact]
    public async Task Member_ReceivesTheFundsFigureAndStillNoAddress()
    {
        var service = BuildService();

        var result = await service.GetBuyBackFiguresAsync(
            ProjectId, new ProjectViewer(MemberId, IsAdmin: false));

        result.IsSuccess.Should().BeTrue();
        var figures = result.Value!;

        figures.TreasuryFundsVisible.Should().BeTrue();
        figures.FundsHiddenNote.Should().BeNull();
        figures.TreasuryFundsMinorUnits.Should().Be(TreasuryFundsMinorUnits);
        figures.ObservedAt.Should().Be(ObservedAt);

        // 250.00 of funds at 2.50 per whole unit fills 100 whole units.
        figures.MaxWholeUnitsFillableNow.Should().Be(100L);

        AssertCarriesNoAccountOrRailFact(figures);
    }

    /// <summary>
    /// The withholding is the ABSENCE of the read, not a field blanked after one: the node
    /// treasury is never asked about this project when a non-member is the reader.
    /// </summary>
    [Fact]
    public async Task NonMember_NeverCausesTheProjectTreasuryToBeRead()
    {
        var nodeTreasury = NodeTreasuryReadModel();
        var service = BuildService(
            ProjectVisibility().Object, HierarchyAuthorization().Object, nodeTreasury.Object);

        var result = await service.GetBuyBackFiguresAsync(
            ProjectId, new ProjectViewer(StrangerId, IsAdmin: false));

        result.IsSuccess.Should().BeTrue();
        nodeTreasury.Verify(
            model => model.GetBalanceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // The mirror: the same read model IS reached for a member, so Times.Never above is a
        // statement about the branch and not about an unreachable dependency.
        var memberNodeTreasury = NodeTreasuryReadModel();
        var memberService = BuildService(
            ProjectVisibility().Object, HierarchyAuthorization().Object, memberNodeTreasury.Object);

        await memberService.GetBuyBackFiguresAsync(ProjectId, new ProjectViewer(MemberId, IsAdmin: false));

        memberNodeTreasury.Verify(
            model => model.GetBalanceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>A project this reader may not see answers NotFound: its existence is private too.</summary>
    [Fact]
    public async Task NonMemberOnADraftProject_IsAnsweredNotFoundAndNoFigures()
    {
        var service = BuildService();

        var result = await service.GetBuyBackFiguresAsync(
            DraftProjectId, new ProjectViewer(StrangerId, IsAdmin: false));

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.NotFound);
        result.Value.Should().BeNull();
    }

    /// <summary>The draft's own owner still reads it, so the refusal above is scoping, not breakage.</summary>
    [Fact]
    public async Task OwnerOfADraftProject_StillReadsItsFigures()
    {
        var service = BuildService();

        var result = await service.GetBuyBackFiguresAsync(
            DraftProjectId, new ProjectViewer(OwnerId, IsAdmin: false));

        result.IsSuccess.Should().BeTrue();
        result.Value!.TreasuryFundsVisible.Should().BeTrue();
    }

    /// <summary>Nobody unnamed reads a project's figures, whatever the transport let through.</summary>
    [Fact]
    public async Task AnonymousViewer_IsRefused()
    {
        var service = BuildService();

        var result = await service.GetBuyBackFiguresAsync(ProjectId, ProjectViewer.Anonymous);

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Unauthorized);
        result.Value.Should().BeNull();
    }

    /// <summary>
    /// With the ports that decide the audience absent, the read REFUSES. A deployment that
    /// forgot to wire the membership check must not fall through to the wider answer.
    /// </summary>
    [Fact]
    public async Task WithoutTheScopingPorts_TheReadRefusesRatherThanAnsweringWidely()
    {
        var service = BuildService(projectVisibility: null, hierarchyAuthorization: null);

        var result = await service.GetBuyBackFiguresAsync(
            ProjectId, new ProjectViewer(MemberId, IsAdmin: false));

        result.IsSuccess.Should().BeFalse();
        result.Type.Should().Be(ResultType.Forbidden);
        result.Error.Should().Be(BuyBackDisclosures.ScopeUndecidable);
        result.Value.Should().BeNull();
    }

    /// <summary>The sibling project-status route strips the account too; OI-89 named both.</summary>
    [Fact]
    public async Task ProjectStatus_ReportsTheBalanceWithoutTheAccountItWasReadFrom()
    {
        var service = BuildService();

        var result = await service.GetProjectStatusAsync(ProjectId);

        result.IsSuccess.Should().BeTrue();
        var status = result.Value!;

        status.IsChainBacked.Should().BeTrue();
        status.ChainBalanceMinorUnits.Should().Be(TreasuryFundsMinorUnits);
        status.TreasuryAddress.Should().BeNull();

        JsonSerializer.Serialize(status).Should().NotContain(TreasuryAccountAddress);
    }

    /// <summary>
    /// Anchors the mocked visibility above to the rule the service actually composes, so the
    /// draft test is not merely agreeing with its own stub.
    /// </summary>
    [Fact]
    public void TheVisibilityRuleTheGateComposes_HidesADraftFromAStranger()
    {
        var strangerSees = ProjectQueryFilters
            .VisibleTo(new ProjectViewer(StrangerId, IsAdmin: false))
            .Compile();

        strangerSees(ProjectRow(DraftProjectId, ProjectStatus.DRAFT)).Should().BeFalse();
        strangerSees(ProjectRow(ProjectId, ProjectStatus.SEEKING_SUPPORT)).Should().BeTrue();

        var ownerSees = ProjectQueryFilters
            .VisibleTo(new ProjectViewer(OwnerId, IsAdmin: false))
            .Compile();

        ownerSees(ProjectRow(DraftProjectId, ProjectStatus.DRAFT)).Should().BeTrue();
    }

    /// <summary>Asserted on the SERIALIZED payload, because "the page does not render it" is not the control.</summary>
    private static void AssertCarriesNoAccountOrRailFact(ProjectBuyBackFigures figures)
    {
        var payload = JsonSerializer.Serialize(figures);

        payload.Should().NotContain(TreasuryAccountAddress);
        payload.Should().NotContain("Address");
        payload.Should().NotContain("address");
        payload.Should().NotContain("ChainBacked");
        payload.Should().NotContain("chainBacked");
    }

    private static TreasuryService BuildService() =>
        BuildService(ProjectVisibility().Object, HierarchyAuthorization().Object);

    private static TreasuryService BuildService(
        IProjectVisibility? projectVisibility,
        IHierarchyAuthorizationService? hierarchyAuthorization,
        IAzoaProjectTreasuryReadModel? nodeTreasuryReadModel = null) =>
        new(
            TreasuryRepository().Object,
            new Mock<IRepository<PlatformTreasuryTransaction>>().Object,
            new Mock<IUnitOfWork>().Object,
            Mapper().Object,
            nodeTreasuryReadModel ?? NodeTreasuryReadModel().Object,
            ConfigRepository().Object,
            RateSource().Object,
            projectVisibility,
            hierarchyAuthorization);

    /// <summary>Both projects are funded the same way, so only the viewer changes between tests.</summary>
    private static Mock<IRepository<ProjectTokenConfig>> ConfigRepository() =>
        RepositoryOf(
            Config(ConfigId, ProjectId),
            Config("config-draft", DraftProjectId));

    private static ProjectTokenConfig Config(string configId, string projectId) => new()
    {
        id = configId,
        projectId = projectId,
        unitName = "Unit",
        treasuryChain = "algorand",
        treasuryReference = "project-reference-1",
        treasuryAddress = TreasuryAccountAddress,
    };

    private static Mock<IRepository<PlatformTreasury>> TreasuryRepository()
    {
        var repository = new Mock<IRepository<PlatformTreasury>>();
        repository
            .Setup(store => store.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<PlatformTreasury>)new List<PlatformTreasury>
            {
                new()
                {
                    id = "treasury-1",
                    ardaAssetId = PlatformAssetId,
                    indexFundAllocationPct = 0.60,
                    liquidReserveAllocationPct = 0.30,
                    operationsAllocationPct = 0.10,
                },
            });

        return repository;
    }

    private static Mock<IAzoaProjectTreasuryReadModel> NodeTreasuryReadModel()
    {
        var readModel = new Mock<IAzoaProjectTreasuryReadModel>();
        readModel
            .Setup(model => model.GetBalanceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ProjectTreasuryBalance>.Success(NodeBalance));

        return readModel;
    }

    private static Mock<IProjectUnitRateSource> RateSource()
    {
        var rateSource = new Mock<IProjectUnitRateSource>();
        rateSource
            .Setup(source => source.GetDeclaredRateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<DeclaredProjectUnitRate>.Success(DeclaredRate));

        return rateSource;
    }

    /// <summary>Mirrors <see cref="ProjectQueryFilters.VisibleTo"/>, which the sibling test pins.</summary>
    private static Mock<IProjectVisibility> ProjectVisibility()
    {
        var visibility = new Mock<IProjectVisibility>();
        visibility
            .Setup(reads => reads.GetVisibleByIdAsync(
                It.IsAny<ProjectViewer>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectViewer viewer, string projectId, CancellationToken token) =>
            {
                var row = ProjectRow(
                    projectId,
                    projectId == DraftProjectId ? ProjectStatus.DRAFT : ProjectStatus.SEEKING_SUPPORT);

                return ProjectQueryFilters.VisibleTo(viewer).Compile().Invoke(row)
                    ? Result<ProjectDto>.Success(new ProjectDto { Id = projectId, CreatedById = OwnerId })
                    : Result<ProjectDto>.NotFound($"Project with id {projectId} not found");
            });

        return visibility;
    }

    private static Mock<IHierarchyAuthorizationService> HierarchyAuthorization()
    {
        var authorization = new Mock<IHierarchyAuthorizationService>();
        authorization
            .Setup(service => service.IsProjectMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, string projectId, CancellationToken token) =>
                userId == OwnerId || (userId == MemberId && projectId == ProjectId));

        return authorization;
    }

    private static Mock<IMapper> Mapper()
    {
        var mapper = new Mock<IMapper>();
        mapper
            .Setup(map => map.Map<TreasuryStatusDto>(It.IsAny<object>()))
            .Returns(() => new TreasuryStatusDto
            {
                Id = "treasury-1",
                IndexFundAllocationPct = 0.60,
                LiquidReserveAllocationPct = 0.30,
                OperationsAllocationPct = 0.10,
            });

        return mapper;
    }

    private static Project ProjectRow(string projectId, ProjectStatus status) => new()
    {
        id = projectId,
        title = "A project",
        slug = projectId,
        status = status,
        createdById = OwnerId,
    };

    private static Mock<IRepository<T>> RepositoryOf<T>(params T[] rows)
        where T : class
    {
        var repository = new Mock<IRepository<T>>();

        repository
            .Setup(store => store.FindAsync(
                It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken token) =>
                (IReadOnlyList<T>)rows.Where(predicate.Compile()).ToList());

        repository
            .Setup(store => store.FindOneAsync(
                It.IsAny<Expression<Func<T, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<T, bool>> predicate, CancellationToken token) =>
                rows.FirstOrDefault(predicate.Compile()));

        return repository;
    }
}
