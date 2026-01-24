
import fs from 'fs';
import path from 'path';
import pluralize from 'pluralize';
// @ts-ignore - @dbml/core lacks types
import { Parser } from '@dbml/core';

const DBML_PATH = path.join(process.cwd(), 'prisma/database-archietecture.dbml');
const DOMAIN_PATH = path.join(process.cwd(), '../ardanova-backend-api-mcp/api-server/src/ArdaNova.Domain/Models');
const DB_CONTEXT_PATH = path.join(process.cwd(), '../ardanova-backend-api-mcp/api-server/src/ArdaNova.Infrastructure/Data/ArdaNovaDbContext.cs');

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
    type: { type_name: string; args?: string[] };
    pk: boolean;
    unique: boolean;
    not_null: boolean;
    dbdefault?: { value: string };
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

const mapType = (dbmlType: string): string => {
    const normalized = dbmlType.toLowerCase();
    return TYPE_MAP[normalized] || dbmlType;
};

const generateCSharp = async () => {
    console.log(`Reading DBML from ${DBML_PATH}...`);
    const dbml = fs.readFileSync(DBML_PATH, 'utf-8').replace(/^\uFEFF/, '');
    const database = Parser.parse(dbml, 'dbml');
    const schema: Schema = database.schemas[0];

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

        let content = `using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ArdaNova.Domain.Models.Enums;

namespace ArdaNova.Domain.Models.Entities;

[Table("${table.name}")]
public class ${table.name}
{
`;

        // Properties
        table.fields.forEach((field) => {
            const isId = field.pk;
            const isNullable = !field.not_null && !isId;
            let csharpType = mapType(field.type.type_name);

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
            if (field.unique) {
                // Note: EF Core uses Fluent API for unique, but we can add a comment
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

                content += `    [ForeignKey("${fkField}")]\n`;
                content += `    public virtual ${remoteModel}? ${relationName} { get; set; }\n\n`;

            } else if (localEp.relation === '1' && remoteEp.relation === '*') {
                // One-to-Many
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

        content += `}\n`;

        const filePath = path.join(DOMAIN_PATH, 'Entities', `${table.name}.cs`);
        if (!dryRun) {
            fs.writeFileSync(filePath, content);
        } else {
            console.log(`[DRY-RUN] Would write: ${filePath}`);
        }
    });

    // 3. Update DbContext with Reconciliation
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
