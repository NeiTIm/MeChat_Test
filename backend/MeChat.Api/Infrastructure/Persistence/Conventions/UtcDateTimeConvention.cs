using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MeChat.Api.Infrastructure.Persistence.Conventions;

public static class UtcDateTimeConvention
{
    public static void Apply(ModelBuilder model)
    {
        // Preserve the existing SQL Server datetime2 wire contract.
        var utc = new ValueConverter<DateTime, DateTime>(
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
        foreach (var entity in model.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
                if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateTime))
                    property.SetValueConverter(utc);
            foreach (var foreignKey in entity.GetForeignKeys())
                foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }
    }
}
