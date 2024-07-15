using System.Security.Claims;

namespace AICentral.ResultHandlers;

internal static class HttpContextEx
{
    public static string GetClientForLoggingPurposes(this HttpContext context)
    {
        if (context.User.Identity?.Name != null)
        {
            return context.User.Identity.Name;
        }
        var appIdClaim = context.User.Claims.FirstOrDefault(x => x.Type == "appid");
        if (appIdClaim != null)
        {
            return appIdClaim.Value;
        }
        var subjectClaim = context.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
        if (subjectClaim != null)
        {
            return subjectClaim.Value;
        }

        return string.Empty;
    }
}