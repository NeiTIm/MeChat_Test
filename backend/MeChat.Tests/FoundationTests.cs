using MeChat.Api.Common;
using MeChat.Api.Infrastructure.Persistence;

namespace MeChat.Tests;

public class FoundationTests
{
    [Fact]
    public void Health_response_identifies_the_API()
    {
        var response = new HealthResponse("ok", "MeChat.Api");
        Assert.Equal("MeChat.Api", response.Service);
        Assert.Equal("ok", response.Status);
    }

    [Theory]
    [InlineData("Server=localhost;Database=MeChat;Trusted_Connection=True;")]
    [InlineData(@"Server=lpc:.\OTHER;Database=MeChat;Integrated Security=True;")]
    [InlineData(@"Server=lpc:.\MSSQLSERVER01;Database=OtherDb;Integrated Security=True;")]
    [InlineData(@"Server=lpc:.\MSSQLSERVER01;Database=MeChat;User ID=test;Password=synthetic;")]
    [InlineData(@"Server=lpc:.\MSSQLSERVER01;Database=MeChat;Integrated Security=True;Failover Partner=remote;")]
    [InlineData(@"Server=lpc:.\MSSQLSERVER01;Database=MeChat;Integrated Security=True;Encrypt=False;")]
    [InlineData("Server=remote;Database=MeChat;Trusted_Connection=True;")]
    [InlineData("Server=localhost;Database=OtherDb;Trusted_Connection=True;")]
    [InlineData("Server=localhost;Database=MeChat;User ID=sa;Password=example;")]
    public void Database_target_rejects_unapproved_connections(string connection)
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseTarget.Validate(connection));
    }

    [Fact]
    public void Database_target_accepts_dedicated_local_database()
    {
        var connection = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(DatabaseTarget.Validate(DatabaseTarget.DefaultConnection));
        Assert.Equal(DatabaseTarget.Server, connection.DataSource);
        Assert.Equal(DatabaseTarget.Database, connection.InitialCatalog);
        Assert.True(connection.IntegratedSecurity);
    }
}
