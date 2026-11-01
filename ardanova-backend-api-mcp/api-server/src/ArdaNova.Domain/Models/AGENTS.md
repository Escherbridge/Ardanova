# Models

`Entities/` and `Enums/` hold the C# side of the schema contract. Column meanings are in
`Entities/AGENTS.md`; enum member meanings are in `Enums/AGENTS.md`.

# Generated contract

`npm run generate:csharp` (`ardanova-client/scripts/generate-csharp-models.ts`) writes
`Entities/<Table>.cs`, `Enums/<Enum>.cs` and
`ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs`, and reconciles the `DbSet`s in
`ArdaNovaDbContext.cs`. Its output is a PURE FUNCTION of
`ardanova-client/prisma/database-architecture.dbml`: it reads nothing else, including which
hand-written files exist. CI ("Assert generated contract artifacts are committed" in
`.github/workflows/ardanova-client-image.yml`) regenerates and fails on any difference, so a
hand edit to a generated file cannot merge. Every generated file opens with a one-line
`// Generated from database-architecture.dbml ...` header naming where hand-written code goes.

## Hand-written code: `<Table>.Custom.cs`

Every entity is emitted as `public partial class <Table>`. The hand-written half lives in
`Entities/<Table>.Custom.cs`, same namespace, `public partial class <Table>`, and holds the type's
`<summary>`, `[NotMapped]` computed members, constants and constructors (for example the
`protected` constructor that retires `TreasuryTransaction`). Standalone helper types get their own
file (`PostVisibilityPolicy.cs`, `ProjectFundingUnits.cs`, `ProjectMediaPaths.cs`).

Every entity is partial, not only the ones with a `.Custom.cs` today. Emitting `partial`
conditionally would make the output depend on the filesystem rather than the DBML, and adding
the first custom member to an entity would then require regenerating it.

The generator's stale-file sweep removes an `Entities/*.cs` only when its table left the DBML AND
the file maps a table (`[Table(`); `*.Custom.cs` is exempt by name. The `Enums/` sweep deletes
EVERY `.cs` whose enum is not in the DBML, so never put hand-written code there. Enums cannot be
partial; member notes go in `Enums/AGENTS.md`.

## Runtime defaults: `csharp-default:`

A property initializer cannot live in a second partial declaration, so a C# default is declared
in the DBML: a column note that ENDS in `csharp-default: <Member.Path>` becomes the initializer.

```dbml
fundingGoalUnit varchar(16) [not null, default: 'ARDA', note: 'Unit the goal is denominated in. csharp-default: ProjectFundingUnits.Ledger']
```

emits `public string fundingGoalUnit { get; set; } = ProjectFundingUnits.Ledger;`. The value must
be a C# member path (the generator throws on anything else, or on text after it), and the member
must be hand-written (here in `Entities/ProjectFundingUnits.cs`). Field notes never reach
`schema.prisma`, so this is invisible to Prisma and to the database.

Why a note rather than the alternatives:

- Deriving the initializer from the DBML `default:` would change behaviour: `fundingGoalUnit`'s
  database default is `'ARDA'` while every EF insert must write USD, and every other non-null
  string with a database default (`categories 'OTHER'`, ...) is `string.Empty` in C# today.
- A list of overrides inside the generator (as `DELETE_BEHAVIOR_OVERRIDES` does for FK delete
  rules) would be a second place the column is defined; the note keeps the default beside the
  column, in the file schema reviewers already read.
- C# partial properties would need the generator to know which properties are customised, so its
  output would stop being a function of the DBML alone.

Sized `varchar(n)` columns are emitted with `[MaxLength(n)]`, so the EF model (and every schema
`EnsureCreated` builds in the real-Postgres tests) matches the column Prisma creates.

## Documentation

Column and enum-member meaning lives in `Entities/AGENTS.md` and `Enums/AGENTS.md`, not in the
generated files. The generator could emit the DBML `note:`s as XML doc-comments instead, but
that would rewrite 56 entity files (259 field notes) and 8 enum files (37 value notes) in one
go. It is a separate change, made deliberately for the whole directory.
