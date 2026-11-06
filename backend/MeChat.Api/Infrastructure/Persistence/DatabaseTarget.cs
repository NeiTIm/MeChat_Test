using Microsoft.Data.SqlClient;

namespace MeChat.Api.Infrastructure.Persistence;

public static class DatabaseTarget
{
    public const string Server = @"lpc:.\MSSQLSERVER01";
    public const string Database = "MeChat";
    public const string DefaultConnection = @"Server=lpc:.\MSSQLSERVER01;Database=MeChat;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

    public static string Validate(string? value)
    {
        var builder = new SqlConnectionStringBuilder(value ?? DefaultConnection);
        if (!string.Equals(builder.DataSource, Server, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(builder.InitialCatalog, Database, StringComparison.Ordinal)
            || !builder.IntegratedSecurity || builder.UserID.Length > 0 || builder.Password.Length > 0
            || builder.AttachDBFilename.Length > 0 || builder.FailoverPartner.Length > 0
            || builder.UserInstance || builder.Encrypt == SqlConnectionEncryptOption.Optional)
            throw new InvalidOperationException("Only lpc:.\\MSSQLSERVER01/MeChat with encrypted Windows Authentication is permitted.");
        return builder.ConnectionString;
    }

    public static string Resolve(IConfiguration configuration)
    {
        var canonical = configuration.GetConnectionString("DefaultConnection");
        var legacy = configuration.GetConnectionString("MeChat");
        // Reject stale hidden overrides too; never silently use a different configured target.
        if (legacy is not null) Validate(legacy);
        return Validate(canonical ?? legacy);
    }
}
