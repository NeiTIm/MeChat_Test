using MeChat.Bcr.SqlTests;

namespace MeChat.Bcr.UnitTests;

public sealed class SqlTargetGuardTests
{
    private const string Host = "127.0.0.1,1", Database = "MeChat_BcrTest_NoConnection";
    private const string Connection = "Server=127.0.0.1,1;Database=MeChat_BcrTest_NoConnection;Integrated Security=True;Encrypt=True";
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AUTHORIZED")]
    public void Missing_explicit_approval_never_falls_back(string? approval) =>
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, Host, Database, Connection));

    [Fact]
    public void Rejects_default_database_mismatched_host_or_catalog_and_missing_connection()
    {
        const string approval = "BCR-G3-UNIT-STRING-ONLY";
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, Host, "MeChat", Connection.Replace(Database, "MeChat")));
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, "another-host", Database, Connection));
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, Host, "MeChat_BcrTest_Other", Connection));
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, Host, Database, null));
        Assert.Throws<InvalidOperationException>(() => SqlTestTarget.Validate(approval, Host, Database, Connection.Replace("Encrypt=True", "Encrypt=False")));
        Assert.Contains(Database, SqlTestTarget.Validate(approval, Host, Database, Connection));
    }
}
