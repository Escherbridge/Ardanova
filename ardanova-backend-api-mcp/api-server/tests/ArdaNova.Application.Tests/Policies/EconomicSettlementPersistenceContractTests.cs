namespace ArdaNova.Application.Tests.Policies;

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ArdaNova.Domain.Models.Entities;
using ArdaNova.Domain.Models.Enums;
using ArdaNova.Domain.Policies;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

public class EconomicSettlementPersistenceContractTests
{
    [Fact]
    public void Settlement_UsesFixedScaleAmountAndUniqueEconomicKeys()
    {
        var type = typeof(EconomicSettlement);

        type.GetProperty(nameof(EconomicSettlement.amount))!.PropertyType.Should().Be(typeof(decimal));
        type.GetProperty(nameof(EconomicSettlement.amount))!
            .GetCustomAttributes(typeof(PrecisionAttribute), inherit: false)
            .Cast<PrecisionAttribute>()
            .Single()
            .Precision.Should().Be(38);
        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(EconomicSettlement.idempotencyKey),
                nameof(EconomicSettlement.externalEventId),
            });
    }

    [Fact]
    public void Outbox_AllowsOnlyOneDispatchClaimPerSettlement()
    {
        typeof(EconomicOutbox)
            .GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Single(index => index.PropertyNames.Contains(nameof(EconomicOutbox.settlementId)))
            .IsUnique.Should().BeTrue();
    }

    [Fact]
    public void ProjectInvestment_RequiresOneInvestmentPerStripePaymentIntent()
    {
        typeof(ProjectInvestment)
            .GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Single(index => index.PropertyNames.Contains(nameof(ProjectInvestment.stripePaymentIntentId)))
            .IsUnique.Should().BeTrue();
    }

    [Fact]
    public void StripeWebhookEvent_UsesTheProviderEventIdAsItsPrimaryKey()
    {
        typeof(StripeWebhookEvent)
            .GetProperty(nameof(StripeWebhookEvent.id))!
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), inherit: false)
            .Should().ContainSingle();
    }

    /// <summary>
    /// The sponsorship's two legs and its currency reference are EXACT INTEGER base units, so no
    /// float or decimal-scale drift can change what was agreed after the fact; the declared rate
    /// that related the legs is persisted beside them; and the row can be settled under exactly
    /// one key, one settlement and one semantic identity.
    /// </summary>
    [Fact]
    public void FundingIntent_PersistsBothExchangeLegsAsBaseUnitsUnderOneSettlementLink()
    {
        var type = typeof(FundingIntent);

        type.GetProperty(nameof(FundingIntent.platformAmountBaseUnits))!.PropertyType.Should().Be(typeof(long));
        type.GetProperty(nameof(FundingIntent.projectUnitQuantity))!.PropertyType.Should().Be(typeof(long));
        type.GetProperty(nameof(FundingIntent.usdReferenceMinorUnits))!.PropertyType.Should().Be(typeof(long));

        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(FundingIntent.semanticKey),
                nameof(FundingIntent.exchangeIdempotencyKey),
                nameof(FundingIntent.settlementId),
            });

        RequiredPropertyNames(type).Should().Contain(new[]
        {
            nameof(FundingIntent.eligibilitySnapshot),
            nameof(FundingIntent.termsSnapshot),
            nameof(FundingIntent.termsHash),
            nameof(FundingIntent.declaredRateArdaPerUnit),
            nameof(FundingIntent.declaredRateSource),
            nameof(FundingIntent.projectAssetId),
        });
    }

    [Fact]
    public void TaskCommerceAgreement_PersistsOneAcceptedAwardPerBidAndTask()
    {
        var type = typeof(TaskCommerceAgreement);

        type.GetProperty(nameof(TaskCommerceAgreement.awardAmount))!.PropertyType.Should().Be(typeof(decimal));
        type.GetProperty(nameof(TaskCommerceAgreement.awardAmount))!
            .GetCustomAttributes(typeof(PrecisionAttribute), inherit: false)
            .Cast<PrecisionAttribute>()
            .Single()
            .Precision.Should().Be(38);

        type.GetCustomAttributes(typeof(IndexAttribute), inherit: false)
            .Cast<IndexAttribute>()
            .Where(index => index.IsUnique)
            .SelectMany(index => index.PropertyNames)
            .Should().Contain(new[]
            {
                nameof(TaskCommerceAgreement.semanticKey),
                nameof(TaskCommerceAgreement.taskId),
                nameof(TaskCommerceAgreement.bidId),
                nameof(TaskCommerceAgreement.escrowId),
                nameof(TaskCommerceAgreement.settlementId),
            });

        RequiredPropertyNames(type).Should().Contain(new[]
        {
            nameof(TaskCommerceAgreement.acceptedTermsSnapshot),
            nameof(TaskCommerceAgreement.termsHash),
        });
    }

    [Theory]
    [InlineData(EconomicSettlementStatus.DRAFT, EconomicSettlementStatus.AUTHORIZED, true)]
    [InlineData(EconomicSettlementStatus.AUTHORIZED, EconomicSettlementStatus.PENDING_DISPATCH, true)]
    [InlineData(EconomicSettlementStatus.PENDING_DISPATCH, EconomicSettlementStatus.SUBMITTED, true)]
    [InlineData(EconomicSettlementStatus.SUBMITTED, EconomicSettlementStatus.CONFIRMED, true)]
    [InlineData(EconomicSettlementStatus.SUBMITTED, EconomicSettlementStatus.AWAITING_RECONCILIATION, true)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.CONFIRMED, true)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.SUBMITTED, true)]
    [InlineData(EconomicSettlementStatus.CONFIRMED, EconomicSettlementStatus.PENDING_DISPATCH, false)]
    [InlineData(EconomicSettlementStatus.AWAITING_RECONCILIATION, EconomicSettlementStatus.PENDING_DISPATCH, false)]
    public void SettlementStateMachine_OnlyAllowsForwardOrTerminalTransitions(
        EconomicSettlementStatus current,
        EconomicSettlementStatus next,
        bool expected)
    {
        EconomicSettlementStateMachine.CanTransition(current, next).Should().Be(expected);
    }

    // -- The database this code actually writes to ---------------------------
    //
    // Every test above reads C# attributes, and an attribute cannot tell you whether the column
    // exists. `kind = EconomicSettlementKind.EXCHANGE` compiled, satisfied every attribute test
    // here, and then raised 22P02 against a native enum holding four labels; `azoaGrossBaseUnits`
    // and its two siblings were EF-mapped onto columns no migration had ever created, so the
    // SaveChanges that completed a paid sponsorship raised 42703 and rolled the record back after
    // the funds had moved. The tests below read `ardanova-client/prisma` from disk -- the ordered
    // migration set that builds the database, and the schema Prisma Client is generated from --
    // so this contract is checked against the artefacts that reach PostgreSQL.

    /// <summary>The reader must fail loudly rather than pass over sources it never found.</summary>
    [Fact]
    public void PrismaSources_ReadTheRealMigrationSetRatherThanHealingOverAMissingOne()
    {
        var sources = PrismaSources.Current;

        sources.MigrationFiles.Should().HaveCountGreaterThan(5);
        sources.MigratedColumnsOf("EconomicSettlement").Should().Contain("idempotencyKey");
        sources.MigratedEnumValuesOf("EconomicSettlementKind").Should().Contain("TASK_REWARD");
        sources.PrismaFieldsOf("EconomicSettlement").Should().Contain("idempotencyKey");
        sources.PrismaEnumValuesOf("EconomicSettlementKind").Should().Contain("TASK_REWARD");

        // Navigation properties are relations, not columns; a reader that returned them would
        // make the column contract below pass for the wrong reason.
        sources.MigratedColumnsOf("EconomicSettlement").Should().NotContain("beneficiaryUser");
        sources.PrismaFieldsOf("EconomicSettlement").Should().NotContain("beneficiaryUser");
    }

    /// <summary>Every EF-mapped settlement property has a column the migration set creates.</summary>
    [Fact]
    public void SettlementColumns_AreCreatedByTheMigrationSet()
    {
        var migrated = PrismaSources.Current.MigratedColumnsOf("EconomicSettlement");

        MappedColumnNames(typeof(EconomicSettlement)).Should().BeSubsetOf(migrated);
        migrated.Should().Contain(new[]
        {
            nameof(EconomicSettlement.azoaGrossBaseUnits),
            nameof(EconomicSettlement.azoaFeeBaseUnits),
            nameof(EconomicSettlement.azoaNetBaseUnits),
        });
    }

    /// <summary>Every settlement kind the code can write is a label the native enum holds.</summary>
    [Fact]
    public void SettlementKinds_AreValuesOfThePostgresEnumTheMigrationSetBuilds()
    {
        var migrated = PrismaSources.Current.MigratedEnumValuesOf("EconomicSettlementKind");

        Enum.GetNames<EconomicSettlementKind>().Should().BeSubsetOf(migrated);
        migrated.Should().Contain(new[] { "EXCHANGE", "HOLD_RELEASE" });
    }

    /// <summary>The generated client and the migration set describe the same settlement row.</summary>
    [Fact]
    public void SettlementColumns_MatchThePrismaSchemaInBothDirections()
    {
        var sources = PrismaSources.Current;

        sources.PrismaFieldsOf("EconomicSettlement")
            .Should().BeEquivalentTo(sources.MigratedColumnsOf("EconomicSettlement"));
        sources.PrismaEnumValuesOf("EconomicSettlementKind")
            .Should().BeEquivalentTo(sources.MigratedEnumValuesOf("EconomicSettlementKind"));
    }

    /// <summary>Cash-out minor units exist in the entity, the migration set and the schema alike.</summary>
    [Fact]
    public void PayoutExactMinorUnits_ExistInTheEntityTheMigrationSetAndThePrismaSchema()
    {
        var sources = PrismaSources.Current;
        var columns = new[]
        {
            nameof(PayoutRequest.ardaMinorUnits),
            nameof(PayoutRequest.usdReferenceMinorUnits),
        };

        typeof(PayoutRequest).GetProperty(columns[0])!.PropertyType.Should().Be(typeof(long?));
        typeof(PayoutRequest).GetProperty(columns[1])!.PropertyType.Should().Be(typeof(long?));
        sources.MigratedColumnsOf("PayoutRequest").Should().Contain(columns);
        sources.PrismaFieldsOf("PayoutRequest").Should().Contain(columns);
    }

    private static IEnumerable<string> RequiredPropertyNames(Type type) => type
        .GetProperties()
        .Where(property => property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), inherit: false).Any())
        .Select(property => property.Name);

    /// <summary>Column names EF maps for an entity: scalar, non-navigation, not [NotMapped].</summary>
    private static IReadOnlyCollection<string> MappedColumnNames(Type entity) => entity
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(IsMappedScalar)
        .Select(property =>
            property.GetCustomAttributes(typeof(ColumnAttribute), inherit: false)
                .Cast<ColumnAttribute>()
                .FirstOrDefault()?.Name ?? property.Name)
        .ToHashSet(StringComparer.Ordinal);

    private static bool IsMappedScalar(PropertyInfo property)
    {
        if (property.GetCustomAttributes(typeof(NotMappedAttribute), inherit: false).Length > 0) return false;
        if (property.GetIndexParameters().Length > 0) return false;

        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return type.IsPrimitive
            || type.IsEnum
            || type == typeof(string)
            || type == typeof(decimal)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(TimeSpan)
            || type == typeof(Guid)
            || type == typeof(byte[]);
    }
}

/// <summary>
/// The Prisma artefacts that build and describe the database, read from disk: the ordered
/// migration set replayed into table and enum shapes, and schema.prisma parsed into models and
/// enums. Nothing here reaches a database; it reads exactly what a deploy would apply.
/// </summary>
internal sealed class PrismaSources
{
    private static readonly Lazy<PrismaSources> Sources = new(Load, isThreadSafe: true);

    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _migratedTables;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _migratedEnums;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _prismaModels;
    private readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> _prismaEnums;

    private PrismaSources(
        IReadOnlyList<string> migrationFiles,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> migratedTables,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> migratedEnums,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> prismaModels,
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> prismaEnums)
    {
        MigrationFiles = migrationFiles;
        _migratedTables = migratedTables;
        _migratedEnums = migratedEnums;
        _prismaModels = prismaModels;
        _prismaEnums = prismaEnums;
    }

    public static PrismaSources Current => Sources.Value;

    public IReadOnlyList<string> MigrationFiles { get; }

    public IReadOnlyCollection<string> MigratedColumnsOf(string table)
        => Require(_migratedTables, table, "table", "the migration set");

    public IReadOnlyCollection<string> MigratedEnumValuesOf(string enumName)
        => Require(_migratedEnums, enumName, "enum type", "the migration set");

    public IReadOnlyCollection<string> PrismaFieldsOf(string model)
        => Require(_prismaModels, model, "model", "schema.prisma");

    public IReadOnlyCollection<string> PrismaEnumValuesOf(string enumName)
        => Require(_prismaEnums, enumName, "enum", "schema.prisma");

    private static IReadOnlyCollection<string> Require(
        IReadOnlyDictionary<string, IReadOnlyCollection<string>> source,
        string name,
        string kind,
        string origin)
        => source.TryGetValue(name, out var members)
            ? members
            : throw new InvalidOperationException(
                $"No {kind} '{name}' in {origin}: this contract must not pass for want of the thing it checks.");

    private static PrismaSources Load()
    {
        var prismaDirectory = LocatePrismaDirectory();
        var migrationFiles = Directory
            .EnumerateFiles(Path.Combine(prismaDirectory, "migrations"), "migration.sql", SearchOption.AllDirectories)
            .OrderBy(path => Path.GetFileName(Path.GetDirectoryName(path)) ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        if (migrationFiles.Count == 0)
            throw new InvalidOperationException($"No migration.sql under '{prismaDirectory}/migrations'.");

        var tables = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var enums = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var file in migrationFiles)
        {
            foreach (var statement in SqlStatements(File.ReadAllText(file)))
                Apply(statement, tables, enums);
        }

        var schema = ReadPrismaSchema(File.ReadAllText(Path.Combine(prismaDirectory, "schema.prisma")));

        return new PrismaSources(
            migrationFiles,
            Freeze(tables),
            Freeze(enums),
            Freeze(schema.Models),
            Freeze(schema.Enums));
    }

    private static IReadOnlyDictionary<string, IReadOnlyCollection<string>> Freeze(
        Dictionary<string, HashSet<string>> source)
        => source.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyCollection<string>)entry.Value,
            StringComparer.Ordinal);

    private static string LocatePrismaDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "ardanova-client", "prisma");
            if (File.Exists(Path.Combine(candidate, "schema.prisma"))) return candidate;
        }

        throw new InvalidOperationException(
            $"No ardanova-client/prisma/schema.prisma above '{AppContext.BaseDirectory}'. This contract reads the real "
            + "migration set; skipping when its sources are absent is how the drift it exists to catch got through.");
    }

    // -- The migration set ---------------------------------------------------

    private const RegexOptions SqlOptions =
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;

    private const string Qualifier = @"(?:(?:""public""|public)\s*\.\s*)?";

    private static readonly Regex DollarTag = new(@"\$[A-Za-z_][A-Za-z0-9_]*\$|\$\$", RegexOptions.Compiled);
    private static readonly Regex CreateTable = new(
        $@"^CREATE\s+TABLE\s+(?:IF\s+NOT\s+EXISTS\s+)?{Qualifier}""(?<table>[^""]+)""\s*\((?<body>.*)\)\s*$", SqlOptions);
    private static readonly Regex DropTable = new(
        $@"^DROP\s+TABLE\s+(?:IF\s+EXISTS\s+)?{Qualifier}""(?<table>[^""]+)""", SqlOptions);
    private static readonly Regex AlterTable = new(
        $@"^ALTER\s+TABLE\s+(?:ONLY\s+)?(?:IF\s+EXISTS\s+)?{Qualifier}""(?<table>[^""]+)""\s+(?<body>.*)$", SqlOptions);
    private static readonly Regex RenameTable = new(@"^RENAME\s+TO\s+""?(?<table>[^""\s]+)""?", SqlOptions);
    private static readonly Regex AddColumn = new(
        @"ADD\s+COLUMN\s+(?:IF\s+NOT\s+EXISTS\s+)?""(?<column>[^""]+)""", SqlOptions);
    private static readonly Regex DropColumn = new(
        @"DROP\s+COLUMN\s+(?:IF\s+EXISTS\s+)?""(?<column>[^""]+)""", SqlOptions);
    private static readonly Regex RenameColumn = new(
        @"^RENAME\s+COLUMN\s+""(?<from>[^""]+)""\s+TO\s+""(?<to>[^""]+)""", SqlOptions);
    private static readonly Regex CreateEnumType = new(
        $@"^CREATE\s+TYPE\s+{Qualifier}""(?<type>[^""]+)""\s+AS\s+ENUM\s*\((?<body>.*)\)\s*$", SqlOptions);
    private static readonly Regex AlterTypeAddValue = new(
        $@"^ALTER\s+TYPE\s+{Qualifier}""(?<type>[^""]+)""\s+ADD\s+VALUE\s+(?:IF\s+NOT\s+EXISTS\s+)?'(?<value>[^']*)'", SqlOptions);
    private static readonly Regex AlterTypeRename = new(
        $@"^ALTER\s+TYPE\s+{Qualifier}""(?<type>[^""]+)""\s+RENAME\s+TO\s+""?(?<to>[^""\s]+)""?", SqlOptions);
    private static readonly Regex DropEnumType = new(
        $@"^DROP\s+TYPE\s+(?:IF\s+EXISTS\s+)?{Qualifier}""(?<type>[^""]+)""", SqlOptions);
    private static readonly Regex ColumnItem = new(@"^""(?<column>[^""]+)""", RegexOptions.Compiled);
    private static readonly Regex EnumLabel = new(@"'(?<value>[^']*)'", RegexOptions.Compiled);

    /// <summary>Replays one statement onto the table and enum shapes built so far.</summary>
    private static void Apply(
        string statement,
        Dictionary<string, HashSet<string>> tables,
        Dictionary<string, HashSet<string>> enums)
    {
        var createTable = CreateTable.Match(statement);
        if (createTable.Success)
        {
            var columns = Members(tables, createTable.Groups["table"].Value);
            columns.Clear();
            foreach (var item in TopLevelItems(createTable.Groups["body"].Value))
            {
                var column = ColumnItem.Match(item.Trim());
                if (column.Success) columns.Add(column.Groups["column"].Value);
            }

            return;
        }

        var dropTable = DropTable.Match(statement);
        if (dropTable.Success)
        {
            tables.Remove(dropTable.Groups["table"].Value);
            return;
        }

        var alterTable = AlterTable.Match(statement);
        if (alterTable.Success)
        {
            var table = alterTable.Groups["table"].Value;
            var alteration = alterTable.Groups["body"].Value.Trim();

            var renameTable = RenameTable.Match(alteration);
            if (renameTable.Success)
            {
                if (tables.Remove(table, out var renamed)) tables[renameTable.Groups["table"].Value] = renamed;
                return;
            }

            var columns = Members(tables, table);
            var renameColumn = RenameColumn.Match(alteration);
            if (renameColumn.Success)
            {
                columns.Remove(renameColumn.Groups["from"].Value);
                columns.Add(renameColumn.Groups["to"].Value);
                return;
            }

            foreach (Match match in AddColumn.Matches(alteration)) columns.Add(match.Groups["column"].Value);
            foreach (Match match in DropColumn.Matches(alteration)) columns.Remove(match.Groups["column"].Value);
            return;
        }

        var createEnum = CreateEnumType.Match(statement);
        if (createEnum.Success)
        {
            var values = Members(enums, createEnum.Groups["type"].Value);
            values.Clear();
            foreach (Match match in EnumLabel.Matches(createEnum.Groups["body"].Value))
                values.Add(match.Groups["value"].Value);
            return;
        }

        var addValue = AlterTypeAddValue.Match(statement);
        if (addValue.Success)
        {
            Members(enums, addValue.Groups["type"].Value).Add(addValue.Groups["value"].Value);
            return;
        }

        var renameType = AlterTypeRename.Match(statement);
        if (renameType.Success)
        {
            if (enums.Remove(renameType.Groups["type"].Value, out var renamed))
                enums[renameType.Groups["to"].Value] = renamed;
            return;
        }

        var dropType = DropEnumType.Match(statement);
        if (dropType.Success) enums.Remove(dropType.Groups["type"].Value);
    }

    private static HashSet<string> Members(Dictionary<string, HashSet<string>> source, string key)
    {
        if (!source.TryGetValue(key, out var members))
        {
            members = new HashSet<string>(StringComparer.Ordinal);
            source[key] = members;
        }

        return members;
    }

    /// <summary>Statements split on ';', honouring quotes, dollar-quoted bodies and comments.</summary>
    private static IEnumerable<string> SqlStatements(string sql)
    {
        var builder = new StringBuilder();
        var index = 0;
        while (index < sql.Length)
        {
            var character = sql[index];

            if (character == '-' && index + 1 < sql.Length && sql[index + 1] == '-')
            {
                while (index < sql.Length && sql[index] != '\n') index++;
                builder.Append(' ');
                continue;
            }

            if (character == '/' && index + 1 < sql.Length && sql[index + 1] == '*')
            {
                var close = sql.IndexOf("*/", index + 2, StringComparison.Ordinal);
                index = close < 0 ? sql.Length : close + 2;
                builder.Append(' ');
                continue;
            }

            if (character is '\'' or '"')
            {
                index = AppendQuoted(sql, index, character, builder);
                continue;
            }

            if (character == '$')
            {
                var tag = DollarTag.Match(sql, index);
                if (tag.Success && tag.Index == index)
                {
                    var close = sql.IndexOf(tag.Value, index + tag.Length, StringComparison.Ordinal);
                    var end = close < 0 ? sql.Length : close + tag.Length;
                    builder.Append(sql, index, end - index);
                    index = end;
                    continue;
                }
            }

            if (character == ';')
            {
                var statement = builder.ToString().Trim();
                if (statement.Length > 0) yield return statement;
                builder.Clear();
                index++;
                continue;
            }

            builder.Append(character);
            index++;
        }

        var trailing = builder.ToString().Trim();
        if (trailing.Length > 0) yield return trailing;
    }

    private static int AppendQuoted(string sql, int index, char quote, StringBuilder builder)
    {
        builder.Append(quote);
        index++;
        while (index < sql.Length)
        {
            if (sql[index] == quote)
            {
                if (index + 1 < sql.Length && sql[index + 1] == quote)
                {
                    builder.Append(quote, 2);
                    index += 2;
                    continue;
                }

                builder.Append(quote);
                return index + 1;
            }

            builder.Append(sql[index]);
            index++;
        }

        return index;
    }

    /// <summary>Comma-separated items of a parenthesised body, ignoring nested parentheses.</summary>
    private static IEnumerable<string> TopLevelItems(string body)
    {
        var depth = 0;
        var start = 0;
        for (var index = 0; index < body.Length; index++)
        {
            var character = body[index];
            if (character is '\'' or '"')
            {
                index = SkipQuoted(body, index, character) - 1;
                continue;
            }

            if (character == '(') depth++;
            else if (character == ')') depth--;
            else if (character == ',' && depth == 0)
            {
                yield return body[start..index];
                start = index + 1;
            }
        }

        yield return body[start..];
    }

    private static int SkipQuoted(string text, int index, char quote)
    {
        index++;
        while (index < text.Length)
        {
            if (text[index] == quote)
            {
                if (index + 1 < text.Length && text[index + 1] == quote)
                {
                    index += 2;
                    continue;
                }

                return index + 1;
            }

            index++;
        }

        return index;
    }

    // -- schema.prisma -------------------------------------------------------

    private static readonly Regex BlockOpener = new(
        @"^(?<kind>model|enum|view|type|datasource|generator)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{", RegexOptions.Compiled);
    private static readonly Regex PrismaField = new(
        @"^(?<name>[A-Za-z_][A-Za-z0-9_]*)\s+(?<type>[A-Za-z_][A-Za-z0-9_]*)(?<modifier>\[\]|\?)?(?<attributes>.*)$", RegexOptions.Compiled);
    private static readonly Regex PrismaEnumValue = new(
        @"^(?<value>[A-Za-z_][A-Za-z0-9_]*)\s*$", RegexOptions.Compiled);

    /// <summary>Scalar fields per model and values per enum; relation fields are not columns.</summary>
    private static (Dictionary<string, HashSet<string>> Models, Dictionary<string, HashSet<string>> Enums) ReadPrismaSchema(string schema)
    {
        var lines = schema.Split('\n').Select(line => line.Trim()).ToList();
        var modelNames = lines
            .Select(line => BlockOpener.Match(line))
            .Where(match => match.Success && match.Groups["kind"].Value == "model")
            .Select(match => match.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var models = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var enums = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        var kind = string.Empty;
        var name = string.Empty;

        foreach (var line in lines)
        {
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;

            if (kind.Length == 0)
            {
                var opener = BlockOpener.Match(line);
                if (!opener.Success) continue;
                kind = opener.Groups["kind"].Value;
                name = opener.Groups["name"].Value;
                if (kind == "model") Members(models, name);
                else if (kind == "enum") Members(enums, name);
                continue;
            }

            if (line.StartsWith("}", StringComparison.Ordinal))
            {
                kind = string.Empty;
                continue;
            }

            if (line.StartsWith("@@", StringComparison.Ordinal)) continue;

            if (kind == "enum")
            {
                var value = PrismaEnumValue.Match(line);
                if (value.Success) Members(enums, name).Add(value.Groups["value"].Value);
                continue;
            }

            if (kind != "model") continue;

            var field = PrismaField.Match(line);
            if (!field.Success) continue;
            if (field.Groups["modifier"].Value == "[]") continue;
            if (field.Groups["attributes"].Value.Contains("@relation", StringComparison.Ordinal)) continue;
            if (modelNames.Contains(field.Groups["type"].Value)) continue;

            Members(models, name).Add(field.Groups["name"].Value);
        }

        return (models, enums);
    }
}
