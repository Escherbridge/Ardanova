using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArdaNova.Infrastructure.Conventions;

/// <summary>
/// Extension methods for applying enum-to-string conversion convention.
/// </summary>
public static class EnumStringConventionExtensions
{
    /// <summary>
    /// Configures all enum properties to be stored as strings in the database.
    /// This provides better readability, debugging, and prevents issues when enum order changes.
    /// </summary>
    public static void ApplyEnumStringConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var clrType = property.ClrType;
                var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

                if (underlyingType.IsEnum)
                {
                    var converterType = typeof(EnumToStringConverter<>).MakeGenericType(underlyingType);
                    var converter = (ValueConverter)Activator.CreateInstance(converterType)!;
                    property.SetValueConverter(converter);
                }
            }
        }
    }
}
