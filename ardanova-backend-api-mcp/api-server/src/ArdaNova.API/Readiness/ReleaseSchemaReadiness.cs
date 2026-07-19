namespace ArdaNova.API.Readiness;

using System.Data;
using ArdaNova.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

public static class ReleaseSchemaReadiness
{
    private const string RequiredCatalogSql = """
        SELECT
            to_regclass('public."ActorAssertionReplay"') IS NOT NULL
            AND to_regclass('public."TaskEscrow"') IS NOT NULL
            AND (
                SELECT COUNT(*) = 4
                    AND bool_and(is_nullable = 'YES')
                    AND bool_and(
                        CASE
                            WHEN column_name = 'disputedAt'
                                THEN data_type = 'timestamp without time zone'
                                    AND datetime_precision = 3
                            ELSE data_type = 'text'
                        END
                    )
                FROM information_schema.columns
                WHERE table_schema = 'public'
                  AND table_name = 'TaskEscrow'
                  AND column_name IN (
                      'disputeReason',
                      'disputeDescription',
                      'disputedByUserId',
                      'disputedAt'
                  )
            )
            AND EXISTS (
                SELECT 1
                FROM pg_class AS index_class
                INNER JOIN pg_index AS index_metadata
                    ON index_metadata.indexrelid = index_class.oid
                INNER JOIN pg_class AS table_class
                    ON table_class.oid = index_metadata.indrelid
                INNER JOIN pg_namespace AS namespace
                    ON namespace.oid = table_class.relnamespace
                WHERE namespace.nspname = 'public'
                  AND table_class.relname = 'ActorAssertionReplay'
                  AND index_class.relname = 'ActorAssertionReplay_expiresAt_jti_idx'
                  AND index_metadata.indisvalid
                  AND index_metadata.indisready
                  AND NOT index_metadata.indisunique
                  AND index_metadata.indpred IS NULL
                  AND (
                      SELECT string_agg(attribute.attname, ',' ORDER BY key_column.ordinality)
                      FROM unnest(index_metadata.indkey) WITH ORDINALITY
                          AS key_column(attribute_number, ordinality)
                      INNER JOIN pg_attribute AS attribute
                          ON attribute.attrelid = table_class.oid
                         AND attribute.attnum = key_column.attribute_number
                      WHERE key_column.ordinality <= index_metadata.indnkeyatts
                  ) = 'expiresAt,jti'
            );
        """;

    public static async Task<bool> CheckAsync(
        ArdaNovaDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        if (openedHere)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = RequiredCatalogSql;
            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is bool ready && ready;
        }
        finally
        {
            if (openedHere)
                await connection.CloseAsync();
        }
    }
}
