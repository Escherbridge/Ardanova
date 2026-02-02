
import fs from 'fs';
import path from 'path';
import pluralize from 'pluralize';
// @ts-ignore - @dbml/core lacks types
import { Parser } from '@dbml/core';

const DBML_PATH = path.join(process.cwd(), 'prisma/database-architecture.dbml');
const DOMAIN_PATH = path.join(process.cwd(), '../ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models');
const DB_CONTEXT_PATH = path.join(process.cwd(), '../ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Data/ArdaNovaDbContext.cs');
const GENERATED_CONFIG_PATH = path.join(process.cwd(), '../ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Data/GeneratedModelConfigurations.cs');

// =============================================================================
// GENERATOR CONFIGURATION
// =============================================================================
const CONFIG = {
    // Default precision for decimal fields when not specified in DBML
    DEFAULT_DECIMAL_PRECISION: [18, 8] as [number, number],
};

// Delete behavior overrides for specific FK relationships.
// Key format: "TableName.fkField" → EF Core DeleteBehavior value.
// Any FK not listed here defaults to "SetNull" for nullable FKs.
const DELETE_BEHAVIOR_OVERRIDES: Record<string, 'Cascade' | 'SetNull' | 'Restrict' | 'NoAction'> = {
    // When a task is deleted, cascade delete its linked opportunity
    'Opportunity.taskId': 'Cascade',
};

// CLI flags
const args = process.argv.slice(2);
const noRemove = args.includes('--no-remove');
const dryRun = args.includes('--dry-run');

// Ensure directories exist
if (!dryRun) {
    if (!fs.existsSync(path.join(DOMAIN_PATH, 'Entities'))) {
        fs.mkdirSync(path.join(DOMAIN_PATH, 'Entities'), { recursive: true });
    }
    if (!fs.existsSync(path.join(DOMAIN_PATH, 'Enums'))) {
        fs.mkdirSync(path.join(DOMAIN_PATH, 'Enums'), { recursive: true });
    }
}

// Type mapping from DBML to C#
const TYPE_MAP: Record<string, string> = {
    'varchar': 'string',
    'text': 'string',
    'int': 'int',
    'integer': 'int',
    'bigint': 'long',
    'decimal': 'decimal',
    'float': 'double',
    'double': 'double',
    'boolean': 'bool',
    'bool': 'bool',
    'datetime': 'DateTime',
    'timestamp': 'DateTime',
    'date': 'DateTime',
    'json': 'string',
    'jsonb': 'string',
    'bytea': 'byte[]',
    'uuid': 'string'
};

interface Field {
    name: string;
    type: { type_name: string; args?: string };
    pk: boolean;
    unique: boolean;
    not_null: boolean;
    dbdefault?: { value: string };
}

interface Index {
    columns: { value: string }[];
    unique?: boolean;
    pk?: boolean;
}

interface Endpoint {
    tableName: string;
    fieldNames: string[];
    relation: '1' | '*';
}

interface Ref {
    endpoints: [Endpoint, Endpoint];
}

interface Table {
    name: string;
    fields: Field[];
    indexes?: Index[];
}

interface Enum {
    name: string;
    values: { name: string }[];
}

interface Schema {
    enums: Enum[];
    tables: Table[];
    refs: Ref[];
}

// Bidirectional FK tracking: two tables each with an FK to the other
interface BidirectionalFK {
    tableA: string;           // e.g., "Opportunity"
    fkFieldA: string;         // e.g., "taskId"
    navPropertyA: string;     // e.g., "Task"
    collectionOnB: string;    // e.g., "Opportunities"
    tableB: string;           // e.g., "ProjectTask"
    fkFieldB: string;         // e.g., "opportunityId"
    navPropertyB: string;     // e.g., "Opportunity"
    collectionOnA: string;    // e.g., "ProjectTasks"
}

// Multi-FK tracking for [InverseProperty] generation
interface MultiFKRelation {
    sourceTable: string;      // e.g., "Referral"
    targetTable: string;      // e.g., "User"
    fkField: string;          // e.g., "referrerId"
    navPropertyName: string;  // e.g., "Referrer"
    collectionName: string;   // e.g., "ReferralsAsReferrer"
}

const mapType = (dbmlType: string): string => {
    // Handle types with args like "decimal(18,8)" -> extract base type
    const baseType = dbmlType.toLowerCase().split('(')[0];
    return TYPE_MAP[baseType] || dbmlType;
};

// Helper to get decimal precision from field type args
const getDecimalPrecision = (field: Field): [number, number] => {
    if (field.type.args) {
        const parts = field.type.args.split(',').map(s => parseInt(s.trim(), 10));
        if (parts.length >= 2 && !isNaN(parts[0]) && !isNaN(parts[1])) {
            return [parts[0], parts[1]];
        }
    }
    return CONFIG.DEFAULT_DECIMAL_PRECISION;
};

// Types that conflict with System namespace types and need full qualification
const CONFLICTING_TYPES = new Set(['TaskStatus']);

const getFullyQualifiedType = (typeName: string): string => {
    if (CONFLICTING_TYPES.has(typeName)) {
        return `ArdaNova.Domain.Models.Enums.${typeName}`;
    }
    return typeName;
};

// Helper to derive navigation property name from FK field
const getNavPropertyName = (fkField: string, remoteModel: string): string => {
    let name = fkField.endsWith('Id')
        ? fkField.slice(0, -2)
        : remoteModel;
    return name.charAt(0).toUpperCase() + name.slice(1);
};

// Build map of multi-FK relationships for [InverseProperty] generation
const buildMultiFKMap = (schema: Schema): Map<string, MultiFKRelation[]> => {
    // First pass: collect all many-to-one refs grouped by (sourceTable, targetTable)
    const refsByPair: Map<string, { fkField: string; navName: string }[]> = new Map();

    schema.refs.forEach((ref) => {
        const [ep1, ep2] = ref.endpoints;

        // Find the many side (FK holder) and one side (target)
        let manyEp: Endpoint, oneEp: Endpoint;
        if (ep1.relation === '*' && ep2.relation === '1') {
            manyEp = ep1;
            oneEp = ep2;
        } else if (ep1.relation === '1' && ep2.relation === '*') {
            manyEp = ep2;
            oneEp = ep1;
        } else {
            return; // Skip one-to-one for now
        }

        const pairKey = `${manyEp.tableName}->${oneEp.tableName}`;
        const fkField = manyEp.fieldNames[0];
        const navName = getNavPropertyName(fkField, oneEp.tableName);

        if (!refsByPair.has(pairKey)) {
            refsByPair.set(pairKey, []);
        }
        refsByPair.get(pairKey)!.push({ fkField, navName });
    });

    // Second pass: identify pairs with multiple FKs (multi-FK scenarios)
    const multiFKMap: Map<string, MultiFKRelation[]> = new Map();

    refsByPair.forEach((relations, pairKey) => {
        if (relations.length > 1) {
            // This is a multi-FK scenario - all relations need [InverseProperty]
            const [sourceTable, targetTable] = pairKey.split('->');

            relations.forEach(({ fkField, navName }) => {
                const collectionName = `${pluralize(sourceTable)}As${navName}`;

                const relation: MultiFKRelation = {
                    sourceTable,
                    targetTable,
                    fkField,
                    navPropertyName: navName,
                    collectionName
                };

                // Index by sourceTable for quick lookup when generating many-side
                const sourceKey = sourceTable;
                if (!multiFKMap.has(sourceKey)) {
                    multiFKMap.set(sourceKey, []);
                }
                multiFKMap.get(sourceKey)!.push(relation);

                // Also index by targetTable for quick lookup when generating one-side collections
                const targetKey = `target:${targetTable}`;
                if (!multiFKMap.has(targetKey)) {
                    multiFKMap.set(targetKey, []);
                }
                multiFKMap.get(targetKey)!.push(relation);
            });
        }
    });

    return multiFKMap;
};

// Detect bidirectional FK relationships: table A has FK to B AND B has FK to A.
// These require explicit Fluent API configuration because EF Core can't resolve them
// from attributes alone.
const buildBidirectionalFKs = (schema: Schema): BidirectionalFK[] => {
    // Collect all many-to-one refs as { fkHolder, target, fkField }
    const manyToOneRefs: { fkHolder: string; target: string; fkField: string }[] = [];

    schema.refs.forEach((ref) => {
        const [ep1, ep2] = ref.endpoints;
        if (ep1.relation === '*' && ep2.relation === '1') {
            manyToOneRefs.push({ fkHolder: ep1.tableName, target: ep2.tableName, fkField: ep1.fieldNames[0] });
        } else if (ep1.relation === '1' && ep2.relation === '*') {
            manyToOneRefs.push({ fkHolder: ep2.tableName, target: ep1.tableName, fkField: ep2.fieldNames[0] });
        }
    });

    const result: BidirectionalFK[] = [];
    const seen = new Set<string>();

    for (const refA of manyToOneRefs) {
        for (const refB of manyToOneRefs) {
            if (refA.fkHolder === refB.target && refA.target === refB.fkHolder && refA.fkHolder !== refA.target) {
                const pairKey = [refA.fkHolder, refA.target].sort().join('<->');
                if (!seen.has(pairKey)) {
                    seen.add(pairKey);

                    const navA = getNavPropertyName(refA.fkField, refA.target);
                    const navB = getNavPropertyName(refB.fkField, refB.target);
                    const collectionOnA = pluralize(refB.fkHolder) + (navB !== refA.fkHolder ? '' : '');
                    const collectionOnB = pluralize(refA.fkHolder) + (navA !== refB.fkHolder ? '' : '');

                    result.push({
                        tableA: refA.fkHolder,
                        fkFieldA: refA.fkField,
                        navPropertyA: navA,
                        collectionOnB: collectionOnB,
                        tableB: refB.fkHolder,
                        fkFieldB: refB.fkField,
                        navPropertyB: navB,
                        collectionOnA: collectionOnA,
                    });
                }
            }
        }
    }

    return result;
};

const generateCSharp = async () => {
    console.log(`Reading DBML from ${DBML_PATH}...`);
    const dbml = fs.readFileSync(DBML_PATH, 'utf-8').replace(/^\uFEFF/, '');
    // eslint-disable-next-line @typescript-eslint/no-explicit-any, @typescript-eslint/no-unsafe-assignment
    const database = Parser.parse(dbml, 'dbml') as any;
    // eslint-disable-next-line @typescript-eslint/no-unsafe-assignment, @typescript-eslint/no-unsafe-member-access
    const schema: Schema = database.schemas[0];

    // Build multi-FK map for [InverseProperty] generation
    const multiFKMap = buildMultiFKMap(schema);
    console.log(`Detected ${multiFKMap.size / 2} multi-FK relationship groups`);

    // Detect bidirectional FK relationships (table A → B and B → A)
    const bidirectionalFKs = buildBidirectionalFKs(schema);
    if (bidirectionalFKs.length > 0) {
        console.log(`Detected ${bidirectionalFKs.length} bidirectional FK relationship(s):`);
        bidirectionalFKs.forEach(bfk => console.log(`  ${bfk.tableA}.${bfk.fkFieldA} <-> ${bfk.tableB}.${bfk.fkFieldB}`));
    }

    const generatedModels: string[] = [];
    const relationNamesPerModel: Map<string, Set<string>> = new Map();

    // Helper to get unique relation name
    const getUniqueRelationName = (modelName: string, baseName: string): string => {
        if (!relationNamesPerModel.has(modelName)) {
            relationNamesPerModel.set(modelName, new Set());
        }
        const usedNames = relationNamesPerModel.get(modelName)!;
        let name = baseName;
        let counter = 1;
        while (usedNames.has(name)) {
            name = `${baseName}${counter}`;
            counter++;
        }
        usedNames.add(name);
        return name;
    };

    // 1. Generate Enums
    console.log('Generating C# Enums...');
    schema.enums.forEach((en) => {
        let content = `namespace ArdaNova.Domain.Models.Enums;

public enum ${en.name}
{
`;
        en.values.forEach((val, index) => {
            content += `    ${val.name}`;
            if (index < en.values.length - 1) content += ',';
            content += '\n';
        });
        content += `}\n`;

        const filePath = path.join(DOMAIN_PATH, 'Enums', `${en.name}.cs`);
        if (!dryRun) {
            fs.writeFileSync(filePath, content);
        } else {
            console.log(`[DRY-RUN] Would write: ${filePath}`);
        }
    });

    // Clean up stale enum files not in DBML
    const generatedEnumNames = schema.enums.map(en => en.name);
    const enumDir = path.join(DOMAIN_PATH, 'Enums');
    if (fs.existsSync(enumDir)) {
        const existingEnumFiles = fs.readdirSync(enumDir).filter(f => f.endsWith('.cs'));
        const generatedEnumFileSet = new Set(generatedEnumNames.map(n => `${n}.cs`));
        const staleEnumFiles = existingEnumFiles.filter(f => !generatedEnumFileSet.has(f));

        if (staleEnumFiles.length > 0) {
            if (noRemove) {
                console.log(`[--no-remove] Would remove stale enum files: ${staleEnumFiles.join(', ')}`);
            } else if (dryRun) {
                console.log(`[DRY-RUN] Would remove stale enum files: ${staleEnumFiles.join(', ')}`);
            } else {
                console.log(`Removing stale enum files: ${staleEnumFiles.join(', ')}`);
                staleEnumFiles.forEach(f => fs.unlinkSync(path.join(enumDir, f)));
            }
        }
    }

    // Helper for relations
    const getRelations = (tableName: string): Ref[] => {
        return schema.refs.filter((ref) =>
            ref.endpoints.some((ep) => ep.tableName === tableName)
        );
    };

    // 2. Generate Entities
    console.log('Generating C# Entities...');
    schema.tables.forEach((table) => {
        generatedModels.push(table.name);

        // Collect simple unique indexes (single field) for [Index] attributes
        const simpleUniqueFields = table.fields.filter(f => f.unique && !f.pk);

        let content = `using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

`;
        // Add [Index] attributes for simple unique fields
        simpleUniqueFields.forEach(field => {
            content += `[Index(nameof(${field.name}), IsUnique = true)]\n`;
        });

        content += `[Table("${table.name}")]
public class ${table.name}
{

`;

        // Properties
        table.fields.forEach((field) => {
            const isId = field.pk;
            const isNullable = !field.not_null && !isId;
            const baseTypeName = field.type.type_name.toLowerCase().split('(')[0];
            let csharpType = mapType(field.type.type_name);

            // Handle types that conflict with System namespace
            csharpType = getFullyQualifiedType(csharpType);

            // Handle nullability
            if (isNullable && csharpType !== 'string' && csharpType !== 'byte[]') {
                csharpType += '?';
            } else if (isNullable && (csharpType === 'string' || csharpType === 'byte[]')) {
                csharpType += '?';
            }

            // Attributes
            if (isId) content += `    [Key]\n`;
            if (field.not_null && !isId && csharpType !== 'string') {
                content += `    [Required]\n`;
            }
            if (field.not_null && csharpType === 'string') {
                content += `    [Required]\n`;
            }

            // [Column(TypeName = "text")] for text fields
            if (baseTypeName === 'text') {
                content += `    [Column(TypeName = "text")]\n`;
            }

            // [Precision(x, y)] for decimal fields
            if (baseTypeName === 'decimal') {
                const [precision, scale] = getDecimalPrecision(field);
                content += `    [Precision(${precision}, ${scale})]\n`;
            }

            // Handle string defaults to avoid CS8618
            let initializer = '';
            if (csharpType === 'string' && !isNullable) {
                initializer = ' = string.Empty;';
            }

            content += `    public ${csharpType} ${field.name} { get; set; }${initializer}\n\n`;
        });

        // Relations
        const tableRefs = getRelations(table.name);
        tableRefs.forEach((ref) => {
            const [ep1, ep2] = ref.endpoints;

            let localEp: Endpoint, remoteEp: Endpoint;
            if (ep1.tableName === table.name) {
                localEp = ep1;
                remoteEp = ep2;
            } else {
                localEp = ep2;
                remoteEp = ep1;
            }

            const remoteModel = remoteEp.tableName;
            const fkField = localEp.fieldNames[0];

            if (localEp.relation === '*' && remoteEp.relation === '1') {
                // Many-to-One
                let relationName = fkField.endsWith('Id')
                    ? fkField.slice(0, -2)
                    : remoteModel;
                relationName = relationName.charAt(0).toUpperCase() + relationName.slice(1);
                relationName = getUniqueRelationName(table.name, relationName);

                if (relationName === table.name) relationName += 'Ref';

                // Check if this is a multi-FK scenario requiring [InverseProperty]
                const multiFKRelations = multiFKMap.get(table.name);
                const multiFKRelation = multiFKRelations?.find(r => r.fkField === fkField);

                content += `    [ForeignKey("${fkField}")]\n`;
                if (multiFKRelation) {
                    content += `    [InverseProperty("${multiFKRelation.collectionName}")]\n`;
                }
                content += `    public virtual ${remoteModel}? ${relationName} { get; set; }\n\n`;

            } else if (localEp.relation === '1' && remoteEp.relation === '*') {
                // One-to-Many
                // Check if this target entity has multi-FK relations pointing to it
                const multiFKRelationsToThis = multiFKMap.get(`target:${table.name}`);
                const multiFKForThisRef = multiFKRelationsToThis?.find(
                    r => r.sourceTable === remoteEp.tableName && r.targetTable === table.name
                );

                if (multiFKForThisRef) {
                    // Multi-FK scenario: use the specific collection name from the map
                    // Skip generation here - we'll generate all multi-FK collections together below
                    return;
                }

                // Normal one-to-many: use pluralized name
                let relationName = pluralize(remoteEp.tableName);
                relationName = getUniqueRelationName(table.name, relationName);

                content += `    public virtual ICollection<${remoteEp.tableName}> ${relationName} { get; set; } = new List<${remoteEp.tableName}>();\n\n`;

            } else if (localEp.relation === '1' && remoteEp.relation === '1') {
                // One-to-One
                const hasFk = table.fields.some((f) => f.name === fkField);
                let relationName = fkField?.endsWith('Id')
                    ? fkField.slice(0, -2)
                    : remoteModel;
                relationName = relationName.charAt(0).toUpperCase() + relationName.slice(1);
                relationName = getUniqueRelationName(table.name, relationName);

                if (hasFk) {
                    content += `    [ForeignKey("${fkField}")]\n`;
                    content += `    public virtual ${remoteModel}? ${relationName} { get; set; }\n\n`;
                } else {
                    content += `    public virtual ${remoteModel}? ${relationName} { get; set; }\n\n`;
                }
            }
        });

        // Generate multi-FK collections with descriptive names
        // Note: [InverseProperty] only needs to be on ONE side - we put it on the many-side (FK holder)
        const multiFKRelationsToThis = multiFKMap.get(`target:${table.name}`);
        if (multiFKRelationsToThis) {
            // Group by source table to avoid duplicate collections
            const generatedCollections = new Set<string>();

            multiFKRelationsToThis.forEach(relation => {
                if (!generatedCollections.has(relation.collectionName)) {
                    generatedCollections.add(relation.collectionName);

                    content += `    public virtual ICollection<${relation.sourceTable}> ${relation.collectionName} { get; set; } = new List<${relation.sourceTable}>();\n\n`;
                }
            });
        }

        content += `}\n`;

        const filePath = path.join(DOMAIN_PATH, 'Entities', `${table.name}.cs`);
        if (!dryRun) {
            fs.writeFileSync(filePath, content);
        } else {
            console.log(`[DRY-RUN] Would write: ${filePath}`);
        }
    });

    // Clean up stale entity files not in DBML
    const entityDir = path.join(DOMAIN_PATH, 'Entities');
    if (fs.existsSync(entityDir)) {
        const existingEntityFiles = fs.readdirSync(entityDir).filter(f => f.endsWith('.cs'));
        const generatedEntityFileSet = new Set(generatedModels.map(m => `${m}.cs`));
        const staleEntityFiles = existingEntityFiles.filter(f => !generatedEntityFileSet.has(f));

        if (staleEntityFiles.length > 0) {
            if (noRemove) {
                console.log(`[--no-remove] Would remove stale entity files: ${staleEntityFiles.join(', ')}`);
            } else if (dryRun) {
                console.log(`[DRY-RUN] Would remove stale entity files: ${staleEntityFiles.join(', ')}`);
            } else {
                console.log(`Removing stale entity files: ${staleEntityFiles.join(', ')}`);
                staleEntityFiles.forEach(f => fs.unlinkSync(path.join(entityDir, f)));
            }
        }
    }

    // 3. Generate Model Configurations file (for composite indexes)
    console.log('Generating model configurations...');
    const compositeIndexes: { table: string; fields: string[]; unique: boolean }[] = [];

    // Collect composite indexes from table.indexes
    schema.tables.forEach((table) => {
        if (table.indexes) {
            table.indexes.forEach((idx) => {
                if (idx.columns.length > 1) {
                    compositeIndexes.push({
                        table: table.name,
                        fields: idx.columns.map(c => c.value),
                        unique: idx.unique === true
                    });
                }
            });
        }
    });

    // Generate the configuration file
    let configContent = `// Auto-generated by generate-csharp-models.ts - DO NOT EDIT manually
// Contains composite indexes and relationship configurations that require Fluent API
using Microsoft.EntityFrameworkCore;
using ArdaNova.Domain.Models.Entities;

namespace ArdaNova.Infrastructure.Data;

public static class GeneratedModelConfigurations
{
    /// <summary>
    /// Applies generated model configurations (composite indexes, bidirectional FK disambiguation).
    /// </summary>
    public static void ApplyGeneratedConfigurations(this ModelBuilder modelBuilder)
    {
`;

    // Composite indexes
    compositeIndexes.forEach(idx => {
        const fields = idx.fields.map(f => `e.${f}`).join(', ');
        const uniqueSuffix = idx.unique ? '.IsUnique()' : '';
        configContent += `        modelBuilder.Entity<${idx.table}>().HasIndex(e => new { ${fields} })${uniqueSuffix};\n`;
    });

    // Bidirectional FK relationship configurations
    if (bidirectionalFKs.length > 0) {
        configContent += `\n        // Bidirectional FK disambiguation\n`;
        bidirectionalFKs.forEach(bfk => {
            const deleteBehaviorA = DELETE_BEHAVIOR_OVERRIDES[`${bfk.tableA}.${bfk.fkFieldA}`] ?? 'SetNull';
            const deleteBehaviorB = DELETE_BEHAVIOR_OVERRIDES[`${bfk.tableB}.${bfk.fkFieldB}`] ?? 'SetNull';

            configContent += `        // ${bfk.tableA}.${bfk.fkFieldA} -> ${bfk.tableB}\n`;
            configContent += `        modelBuilder.Entity<${bfk.tableA}>()\n`;
            configContent += `            .HasOne(e => e.${bfk.navPropertyA})\n`;
            configContent += `            .WithMany(e => e.${bfk.collectionOnB})\n`;
            configContent += `            .HasForeignKey(e => e.${bfk.fkFieldA})\n`;
            configContent += `            .OnDelete(DeleteBehavior.${deleteBehaviorA});\n\n`;

            configContent += `        // ${bfk.tableB}.${bfk.fkFieldB} -> ${bfk.tableA}\n`;
            configContent += `        modelBuilder.Entity<${bfk.tableB}>()\n`;
            configContent += `            .HasOne(e => e.${bfk.navPropertyB})\n`;
            configContent += `            .WithMany(e => e.${bfk.collectionOnA})\n`;
            configContent += `            .HasForeignKey(e => e.${bfk.fkFieldB})\n`;
            configContent += `            .OnDelete(DeleteBehavior.${deleteBehaviorB});\n\n`;
        });
    }

    configContent += `    }
}
`;

    if (!dryRun) {
        fs.writeFileSync(GENERATED_CONFIG_PATH, configContent);
        console.log(`Generated ${compositeIndexes.length} composite index configurations`);
    } else {
        console.log(`[DRY-RUN] Would write: ${GENERATED_CONFIG_PATH}`);
        console.log(`[DRY-RUN] ${compositeIndexes.length} composite indexes`);
    }

    // 4. Update DbContext with Reconciliation
    console.log(`Updating DbContext at ${DB_CONTEXT_PATH}...`);
    if (fs.existsSync(DB_CONTEXT_PATH)) {
        let dbContext = fs.readFileSync(DB_CONTEXT_PATH, 'utf-8');

        // Find all existing DbSet declarations
        const existingDbSetRegex = /public\s+DbSet<(\w+)>\s+\w+/g;
        const existingModels: string[] = [];
        let match;
        while ((match = existingDbSetRegex.exec(dbContext)) !== null) {
            existingModels.push(match[1]);
        }

        // Find models to add
        const modelsToAdd = generatedModels.filter(m => !existingModels.includes(m));

        // Find models to remove (in DbContext but not in DBML)
        const modelsToRemove = existingModels.filter(m => !generatedModels.includes(m));

        if (modelsToRemove.length > 0 && !noRemove) {
            console.log(`Removing stale DbSets: ${modelsToRemove.join(', ')}`);
            modelsToRemove.forEach(m => {
                // Remove DbSet line
                const removeRegex = new RegExp(`\\s*public\\s+DbSet<${m}>\\s+\\w+\\s*(?:=>\\s*Set<${m}>\\(\\)|{\\s*get;\\s*set;\\s*});?\\n?`, 'g');
                dbContext = dbContext.replace(removeRegex, '\n');
            });
        } else if (modelsToRemove.length > 0 && noRemove) {
            console.log(`[--no-remove] Would remove: ${modelsToRemove.join(', ')}`);
        }

        if (modelsToAdd.length > 0) {
            console.log(`Adding ${modelsToAdd.length} missing DbSets...`);

            // Find insertion point
            const dbSetInsertRegex = /public\s+DbSet<.*?>\s+.*?(?:=>\s*Set<.*?>\(\)|{\s*get;\s*set;\s*});?/g;
            const matches = [...dbContext.matchAll(dbSetInsertRegex)];

            if (matches.length > 0) {
                const lastMatch = matches[matches.length - 1];
                const insertionIndex = lastMatch.index! + lastMatch[0].length;

                let newSets = '\n';
                modelsToAdd.forEach(m => {
                    const plural = pluralize(m);
                    newSets += `    public DbSet<${m}> ${plural} => Set<${m}>();\n`;
                });

                dbContext = dbContext.slice(0, insertionIndex) + newSets + dbContext.slice(insertionIndex);

                if (!dbContext.includes('using ArdaNova.Domain.Models.Entities;')) {
                    dbContext = 'using ArdaNova.Domain.Models.Entities;\n' + dbContext;
                }
            } else {
                console.warn('Could not find existing DbSets. Please add manually.');
            }

            console.log(`Added: ${modelsToAdd.join(', ')}`);
        } else {
            console.log('All DbSets up to date.');
        }

        if (!dryRun) {
            fs.writeFileSync(DB_CONTEXT_PATH, dbContext);
        } else {
            console.log('[DRY-RUN] Would update DbContext');
        }
    } else {
        console.error('DbContext file not found!');
    }

    console.log('Done.');
    if (dryRun) {
        console.log('\n[DRY-RUN MODE] No files were modified.');
    }
};

generateCSharp().catch(console.error);
