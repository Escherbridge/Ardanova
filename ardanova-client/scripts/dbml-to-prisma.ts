
import fs from 'fs';
import path from 'path';
import pluralize from 'pluralize';
// @ts-ignore - @dbml/core lacks types
import { Parser } from '@dbml/core';

const DBML_PATH = path.join(process.cwd(), 'prisma/database-archietecture.dbml');
const PRISMA_PATH = path.join(process.cwd(), 'prisma/schema.prisma');

// Type mapping from DBML to Prisma
const TYPE_MAP: Record<string, string> = {
    'varchar': 'String',
    'text': 'String',
    'int': 'Int',
    'integer': 'Int',
    'bigint': 'BigInt',
    'decimal': 'Decimal',
    'float': 'Float',
    'double': 'Float',
    'boolean': 'Boolean',
    'bool': 'Boolean',
    'datetime': 'DateTime',
    'timestamp': 'DateTime',
    'date': 'DateTime',
    'json': 'Json',
    'jsonb': 'Json',
    'bytea': 'Bytes',
    'uuid': 'String'
};

interface FieldType {
    type_name: string;
    args?: string[];
}

interface Field {
    name: string;
    type: FieldType;
    pk: boolean;
    unique: boolean;
    not_null: boolean;
    dbdefault?: { value: string; type: string };
    note?: string;
}

interface Index {
    columns: { value: string }[];
    unique?: boolean | string;
    pk?: boolean | string;
    name?: string;
}

interface Table {
    name: string;
    alias?: string;
    fields: Field[];
    indexes?: Index[];
}

interface Endpoint {
    tableName: string;
    fieldNames: string[];
    relation: '1' | '*';
}

interface Ref {
    name?: string;
    endpoints: [Endpoint, Endpoint];
    onDelete?: string;
    onUpdate?: string;
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
    return TYPE_MAP[normalized] || dbmlType; // Fallback to original (for enums)
};

const parseTypeArgs = (field: Field): string => {
    const typeName = field.type.type_name.toLowerCase();
    const args = field.type.args;

    if (typeName === 'decimal' && args && args.length >= 2) {
        return `@db.Decimal(${args[0]}, ${args[1]})`;
    }
    if (typeName === 'varchar' && args && args.length >= 1) {
        return `@db.VarChar(${args[0]})`;
    }
    if (typeName === 'text') {
        return '@db.Text';
    }
    return '';
};

const generatePrisma = async () => {
    console.log(`Reading DBML from ${DBML_PATH}...`);
    const dbml = fs.readFileSync(DBML_PATH, 'utf-8').replace(/^\uFEFF/, '');

    console.log('Parsing DBML...');
    const database = Parser.parse(dbml, 'dbml');
    const schema: Schema = database.schemas[0];

    let output = `// Auto-generated from database-archietecture.dbml
// DO NOT EDIT DIRECTLY - modify the DBML file and run: npm run generate:prisma

generator client {
  provider        = "prisma-client-js"
  previewFeatures = ["driverAdapters"]
}

generator zod {
  provider         = "zod-prisma-types"
  output           = "../src/lib/zod"
  useMultipleFiles = true
  createInputTypes = false
}

datasource db {
  provider = "postgresql"
  url      = env("DATABASE_URL")
}

`;

    // Track generated relation names per model to avoid duplicates
    const relationNamesPerModel: Map<string, Set<string>> = new Map();

    // 1. Generate Enums
    console.log('Generating Enums...');
    schema.enums.forEach((en) => {
        output += `enum ${en.name} {\n`;
        en.values.forEach((val) => {
            output += `  ${val.name}\n`;
        });
        output += `}\n\n`;
    });

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

    // Helper to find relations for a table
    const getRelations = (tableName: string): Ref[] => {
        return schema.refs.filter((ref) =>
            ref.endpoints.some((ep) => ep.tableName === tableName)
        );
    };

    // 2. Generate Models
    console.log('Generating Models...');
    schema.tables.forEach((table) => {
        output += `model ${table.name} {\n`;

        const tableRefs = getRelations(table.name);
        const processedRefPairs = new Set<string>();

        // Generate fields
        table.fields.forEach((field) => {
            const mappedType = mapType(field.type.type_name);
            let line = `  ${field.name} ${mappedType}`;

            // Nullability
            if (!field.not_null && !field.pk) {
                line += '?';
            }

            // Attributes
            if (field.pk) line += ' @id';
            if (field.unique) line += ' @unique';

            // Default values
            if (field.dbdefault) {
                let def = field.dbdefault.value;
                const defType = field.dbdefault.type;

                // Check if field type is an enum (not in TYPE_MAP means it's likely an enum or model)
                const isEnumType = !TYPE_MAP[field.type.type_name.toLowerCase()];

                if (defType === 'expression') {
                    // Handle expressions like now(), cuid(), uuid()
                    if (def.includes('now')) def = 'now()';
                    else if (def.includes('cuid')) def = 'cuid()';
                    else if (def.includes('uuid')) def = 'uuid()';
                    else if (def.includes('autoincrement')) def = 'autoincrement()';
                } else if (defType === 'boolean') {
                    def = def === 'true' ? 'true' : 'false';
                } else if (defType === 'number') {
                    // Keep as is
                } else if (isEnumType) {
                    // Enum values should NOT be quoted in Prisma
                    // def stays as is (e.g., INDIVIDUAL, not "INDIVIDUAL")
                } else if (defType === 'string') {
                    def = `"${def}"`;
                }

                line += ` @default(${def})`;
            } else if (field.name === 'updatedAt' && mappedType === 'DateTime') {
                line += ' @updatedAt';
            }

            // Type-specific attributes (precision, length)
            const typeAttr = parseTypeArgs(field);
            if (typeAttr) line += ` ${typeAttr}`;

            output += line + '\n';
        });

        // Generate relation fields
        tableRefs.forEach((ref) => {
            const [ep1, ep2] = ref.endpoints;

            // Determine local and remote endpoints
            let localEp: Endpoint, remoteEp: Endpoint;
            if (ep1.tableName === table.name) {
                localEp = ep1;
                remoteEp = ep2;
            } else {
                localEp = ep2;
                remoteEp = ep1;
            }

            const remoteModel = remoteEp.tableName;
            const refKey = `${localEp.tableName}-${localEp.fieldNames[0]}-${remoteEp.tableName}-${remoteEp.fieldNames[0]}`;

            // Skip if already processed from other direction
            if (processedRefPairs.has(refKey)) return;
            processedRefPairs.add(refKey);

            const fkField = localEp.fieldNames[0];
            const pkField = remoteEp.fieldNames[0];

            // Generate relation name from FK field
            let relationFieldName = fkField.endsWith('Id')
                ? fkField.slice(0, -2)
                : remoteModel.charAt(0).toLowerCase() + remoteModel.slice(1);

            // Create a unique relation name for Prisma disambiguation
            // Use format: ModelName_FieldName to ensure uniqueness
            const relationDbName = `${table.name}_${fkField}`;

            // Handle onDelete
            const onDelete = ref.onDelete ? `, onDelete: ${ref.onDelete}` : '';

            if (localEp.relation === '*' && remoteEp.relation === '1') {
                // Many-to-One: This table holds the FK
                relationFieldName = getUniqueRelationName(table.name, relationFieldName);

                // Check if FK field is nullable (not marked as not_null)
                const fkFieldDef = table.fields.find((f) => f.name === fkField);
                const isOptional = fkFieldDef && !fkFieldDef.not_null;
                const optionalMarker = isOptional ? '?' : '';

                output += `  ${relationFieldName} ${remoteModel}${optionalMarker} @relation("${relationDbName}", fields: [${fkField}], references: [${pkField}]${onDelete})\n`;

                // For self-referential relations, also generate the back-reference array
                if (ep1.tableName === ep2.tableName) {
                    const childrenFieldName = getUniqueRelationName(table.name, relationFieldName + 'Children');
                    output += `  ${childrenFieldName} ${table.name}[] @relation("${relationDbName}")\n`;
                }

            } else if (localEp.relation === '1' && remoteEp.relation === '*') {
                // One-to-Many: This table is referenced by many
                const pluralName = pluralize(remoteEp.tableName.charAt(0).toLowerCase() + remoteEp.tableName.slice(1));
                const listName = getUniqueRelationName(table.name, pluralName);
                // Use the FK field from the remote side for the relation name
                const remoteRefKey = `${remoteEp.tableName}_${remoteEp.fieldNames[0]}`;
                output += `  ${listName} ${remoteEp.tableName}[] @relation("${remoteRefKey}")\n`;

            } else if (localEp.relation === '1' && remoteEp.relation === '1') {
                // One-to-One
                const hasFk = table.fields.some((f) => f.name === fkField);
                relationFieldName = getUniqueRelationName(table.name, relationFieldName);

                if (hasFk) {
                    output += `  ${relationFieldName} ${remoteModel}? @relation("${relationDbName}", fields: [${fkField}], references: [${pkField}]${onDelete})\n`;
                } else {
                    // Inverse side of one-to-one
                    const remoteRefKey = `${remoteModel}_${remoteEp.fieldNames[0]}`;
                    output += `  ${relationFieldName} ${remoteModel}? @relation("${remoteRefKey}")\n`;
                }
            }
        });

        // Generate table-level indexes
        if (table.indexes && table.indexes.length > 0) {
            output += '\n';
            table.indexes.forEach((idx) => {
                const columns = idx.columns.map((c) => c.value).join(', ');
                if (idx.unique) {
                    output += `  @@unique([${columns}])\n`;
                } else if (idx.pk) {
                    output += `  @@id([${columns}])\n`;
                } else {
                    output += `  @@index([${columns}])\n`;
                }
            });
        }

        // Table alias/mapping
        if (table.alias) {
            output += `  @@map("${table.alias}")\n`;
        }

        output += `}\n\n`;
    });

    fs.writeFileSync(PRISMA_PATH, output);
    console.log(`Generated Prisma schema at ${PRISMA_PATH}`);
    console.log('Run "npx prisma validate" to verify the schema.');
};

generatePrisma().catch(console.error);
