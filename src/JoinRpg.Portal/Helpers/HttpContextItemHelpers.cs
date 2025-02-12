using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace JoinRpg.Portal.Helpers;

/// <summary>
/// Read something stored in HttpContext by attributes
/// </summary>
internal static class HttpContextItemHelpers
{
    public const string ProjectidKey = "projectId";

    public static int GetProjectId(this HttpContext httpContext)
    {
        var item = httpContext.Items[ProjectidKey];
        if (item is int i)
        {
            return i;
        }

        var projectIdRawValue = ((ValueProviderResult)item).Values;
        if (projectIdRawValue.GetType().IsArray)
        {
            return int.Parse(((string[])projectIdRawValue)[0]);
        }
        else
        {
            return int.Parse(projectIdRawValue);
        }
    }

    public static int? GetProjectIdFromRouteOrDefault(this HttpContext httpContext)
    {
        if (httpContext.GetRouteValue("projectId") is string projectIdAsObj
                        && int.TryParse(projectIdAsObj.ToString(), out var projectId))
        {
            return projectId;
        }
        return null;
    }
}
