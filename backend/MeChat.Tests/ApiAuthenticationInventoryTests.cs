using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MeChat.Tests;

public sealed class ApiAuthenticationInventoryTests
{
    [Fact]
    public async Task Every_protected_controller_route_rejects_anonymous_requests_before_business_logic()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)) })));
        using var client = factory.CreateClient();
        var actions = factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>();
        var count = 0; var inventory = new List<string>();
        foreach (var action in actions)
        {
            var protectedRoute = action.ControllerTypeInfo.GetCustomAttributes<AuthorizeAttribute>(true).Any() || action.MethodInfo.GetCustomAttributes<AuthorizeAttribute>(true).Any();
            var anonymous = action.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>(true).Any() || action.ControllerTypeInfo.GetCustomAttributes<AllowAnonymousAttribute>(true).Any();
            if (!protectedRoute || anonymous) continue;
            var template = action.AttributeRouteInfo!.Template!;
            var path = "/" + Regex.Replace(template, @"\{[^}]+\}", _ => Guid.NewGuid().ToString());
            var methods = action.ActionConstraints!.OfType<HttpMethodActionConstraint>().SelectMany(c => c.HttpMethods);
            foreach (var method in methods)
            {
                using var request = new HttpRequestMessage(new HttpMethod(method), path);
                var response = await client.SendAsync(request);
                Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, $"{method} {template}: expected401, got{(int)response.StatusCode}");
                inventory.Add($"{method} {template}: anonymous401"); count++;
            }
        }
        Assert.True(count >= 60, $"Unexpectedly small authentication inventory: {count}");
        var directory = Path.Combine(factory.Services.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>().ContentRootPath, "private-uploads"); Directory.CreateDirectory(directory);
        await File.WriteAllLinesAsync(Path.Combine(directory, "qa-authenticated-route-inventory-2026-11-05.txt"), inventory.Order());
    }
}
