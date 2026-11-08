using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using MeChat.Api.Configurations;
using MeChat.Api.Features.Friends;
using MeChat.Api.Features.Notifications;
using MeChat.Api.Infrastructure.Persistence;
using MeChat.Api.Infrastructure.Persistence.Migrations;

namespace MeChat.Bcr.UnitTests;

public sealed class ContractAndMigrationTests
{
    [Fact]
    public void Migration_is_exactly_two_nullable_columns_and_one_transactional_backfill()
    {
        var migration = new NotificationTrashFriendSeen();
        var columns = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        Assert.Equal(2, columns.Length);
        Assert.All(columns, c => { Assert.True(c.IsNullable); Assert.Equal("datetime2(7)", c.ColumnType); Assert.Null(c.DefaultValue); Assert.Null(c.DefaultValueSql); });
        Assert.Contains(columns, c => c.Table == "Notifications" && c.Name == "DeletedAt");
        Assert.Contains(columns, c => c.Table == "FriendRequests" && c.Name == "SeenAt");
        var sql = Assert.Single(migration.UpOperations.OfType<SqlOperation>());
        Assert.False(sql.SuppressTransaction); Assert.Contains("SYSUTCDATETIME()", sql.Sql);
        Assert.StartsWith("EXEC(N'", sql.Sql.TrimStart()); // Defer compiling references to the newly added column.
        Assert.Contains("[Status] = N'Pending' AND [SeenAt] IS NULL", sql.Sql.Replace("''", "'"));
        Assert.DoesNotContain("CreatedAt", sql.Sql); Assert.DoesNotContain("DELETE", sql.Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, migration.UpOperations.Count);
        Assert.Equal(2, migration.DownOperations.Count); Assert.All(migration.DownOperations, op => Assert.IsType<DropColumnOperation>(op));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Flags_off_reject_before_model_binding_with_safe_no_store_response(bool notifications)
    {
        using var services = new ServiceCollection().AddOptions().Configure<BcrFeatureOptions>(_ => { }).BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = services };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var resource = new ResourceExecutingContext(action, [], new List<IValueProviderFactory>());
        new BcrFeatureAttribute(notifications).OnResourceExecuting(resource);
        var result = Assert.IsType<ObjectResult>(resource.Result);
        Assert.Equal(404, result.StatusCode); Assert.Equal("private, no-store", http.Response.Headers.CacheControl.ToString());
        Assert.Equal("BCR_FEATURE_DISABLED", Assert.IsType<ProblemDetails>(result.Value).Extensions["code"]);
    }

    [Fact]
    public void Invalid_binding_errors_are_replaced_without_echoing_submitted_values()
    {
        var http = new DefaultHttpContext(); var action = new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        action.ModelState.AddModelError("requestIds[0]", "secret-submitted-test-value");
        var context = new ActionExecutingContext(action, [], new Dictionary<string, object?>(), new object());
        new BcrInputValidationAttribute("INVALID_REQUEST_IDS").OnActionExecuting(context);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(400, result.StatusCode); var json = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("secret-submitted-test-value", json); Assert.DoesNotContain("requestIds[0]", json);
        Assert.Contains("INVALID_REQUEST_IDS", json);
    }

    [Fact]
    public void Six_new_actions_are_flag_guarded_and_legacy_actions_are_not()
    {
        var actions = new[] { typeof(NotificationsController), typeof(FriendsController) }
            .SelectMany(t => t.GetMethods()).Where(m => m.GetCustomAttribute<BcrFeatureAttribute>() != null).ToArray();
        Assert.Equal(6, actions.Length);
        Assert.Equal(8192L, ((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)typeof(FriendsController).GetMethod("MarkSeen")!.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize);
        Assert.Null(typeof(NotificationsController).GetMethod("List")!.GetCustomAttribute<BcrInputValidationAttribute>());
        Assert.Null(typeof(NotificationsController).GetMethod("List")!.GetCustomAttribute<BcrFeatureAttribute>());
    }

    [Fact]
    public void Production_guard_is_unchanged_and_never_accepts_disposable_catalog()
    {
        // String validation only; no connection is created or opened.
        Assert.Equal("MeChat", DatabaseTarget.Database);
        Assert.Throws<InvalidOperationException>(() => DatabaseTarget.Validate("Server=lpc:.\\MSSQLSERVER01;Database=MeChat_BcrTest_Unit;Integrated Security=True;Encrypt=True"));
    }
}
