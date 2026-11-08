using Microsoft.Data.SqlClient;

namespace MeChat.Bcr.SqlTests;

// Pure validation shared by fixture and safe unit tests. Never creates a connection.
public static class SqlTestTarget
{
    public static string Validate(string? approval, string? allowedHost, string? allowedDatabase, string? connection)
    {
        if (approval?.StartsWith("BCR-G3-", StringComparison.Ordinal) != true || string.IsNullOrWhiteSpace(allowedHost) ||
            string.IsNullOrWhiteSpace(allowedDatabase) || string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("BLOCKED: explicit G3 approval and isolated target configuration required.");
        SqlConnectionStringBuilder builder;
        try { builder = new SqlConnectionStringBuilder(connection); }
        catch (ArgumentException) { throw new InvalidOperationException("BLOCKED: invalid SQL test target configuration."); }
        if (!allowedDatabase.StartsWith("MeChat_BcrTest_", StringComparison.Ordinal) ||
            !string.Equals(builder.InitialCatalog, allowedDatabase, StringComparison.Ordinal) ||
            string.Equals(builder.InitialCatalog, "MeChat", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(builder.DataSource, allowedHost, StringComparison.OrdinalIgnoreCase) ||
            !builder.IntegratedSecurity || builder.UserID.Length != 0 || builder.Password.Length != 0 ||
            builder.AttachDBFilename.Length != 0 || builder.FailoverPartner.Length != 0 || builder.UserInstance ||
            builder.Encrypt == SqlConnectionEncryptOption.Optional)
            throw new InvalidOperationException("BLOCKED: unsafe SQL test target.");
        builder.ConnectTimeout = 5;
        return builder.ConnectionString;
    }
}
