using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.ObjectCachingService;
using static SkripsiAppBackend.Common.Authentication.AuthenticationMiddleware;
using System.Security.Claims;
using SkripsiAppBackend.Common.Exceptions;

namespace SkripsiAppBackend.Common.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ErrorHandlingMiddleware(RequestDelegate requestDelegate)
        {
            next = requestDelegate;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IAzureDevopsService azureDevopsService,
            IObjectCachingService<List<ProfileTeam>> teamsCachingService)
        {
            try
            {
                await next(httpContext);
            }
            catch (UserFacingException userFacingException)
            {
                httpContext.Response.StatusCode = 400;
                httpContext.Response.ContentType = "text/plain";
                await httpContext.Response.WriteAsync(userFacingException.ErrorCode.ToString());
            }
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
