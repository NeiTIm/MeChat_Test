using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MeChat.Api.Configurations;

// Run before ApiController's model-state filter, only on new BCR actions.
// Framework binding errors can contain the submitted value; never echo those values.
[AttributeUsage(AttributeTargets.Method)]
public sealed class BcrInputValidationAttribute(string code) : Attribute, IActionFilter, IOrderedFilter
{
    public int Order => -3000;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
            context.Result = BcrResponses.Invalid(code);
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class BcrFeatureAttribute(bool notifications) : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.HttpContext.Response.Headers.CacheControl = "private, no-store";
        var options = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<BcrFeatureOptions>>().Value;
        if (!(notifications ? options.NotificationLifecycleEnabled : options.FriendRequestSeenEnabled))
            context.Result = BcrResponses.Unavailable(disabled: true);
    }

    public void OnResourceExecuted(ResourceExecutedContext context) { }
}

public static class BcrResponses
{
    public static ObjectResult Invalid(string code) => new(new ValidationProblemDetails(
        new Dictionary<string, string[]> { ["request"] = ["Invalid request."] })
    {
        Status = 400,
        Title = "Invalid request",
        Extensions = { ["code"] = code }
    }) { StatusCode = 400 };

    public static ObjectResult Unavailable(bool disabled = false) => new(new ProblemDetails
    {
        Status = 404,
        Title = disabled ? "Feature unavailable" : "Notification unavailable",
        Extensions = { ["code"] = disabled ? "BCR_FEATURE_DISABLED" : "NOTIFICATION_UNAVAILABLE" }
    }) { StatusCode = 404 };
}
