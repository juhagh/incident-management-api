using Microsoft.AspNetCore.Mvc;

namespace API.Http.Etags;

public static class ConditionalResults
{
    public static ActionResult<T> ConditionalOk<T>(
        this ControllerBase controller,
        T body,
        uint rowVersion)
    {
        var etag = ETagHelper.CreateWeakETag(rowVersion);

        controller.Response.Headers.ETag = etag;
        controller.Response.Headers.CacheControl = "private, max-age=0";

        var ifNoneMatch = controller.Request.Headers.IfNoneMatch.ToString();

        if (ETagHelper.ShouldReturnNotModified(ifNoneMatch, rowVersion))
            return controller.StatusCode(StatusCodes.Status304NotModified);

        return controller.Ok(body);
    }
}