using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Tests;

public sealed class UtcTimestampTests
{
    [Fact]
    public void Sql_datetime2_materialization_preserves_utc_wire_contract_for_required_and_nullable_timestamps()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(DatabaseTarget.DefaultConnection).Options);
        var databaseValue = new DateTime(2026, 11, 4, 13, 35, 0, DateTimeKind.Unspecified);
        foreach (var (type, name) in new[] { (typeof(Message), nameof(Message.CreatedAt)), (typeof(User), nameof(User.LastSeenAt)) })
        {
            var converter = db.Model.FindEntityType(type)!.FindProperty(name)!.GetValueConverter()!;
            var value = Assert.IsType<DateTime>(converter.ConvertFromProvider(databaseValue));
            Assert.Equal(databaseValue.Ticks, value.Ticks);
            Assert.Equal(DateTimeKind.Utc, value.Kind);
            Assert.EndsWith("Z\"", JsonSerializer.Serialize(value));
        }
    }
}
