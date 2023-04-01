using Jose;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Controllers;
using SkripsiAppBackend.Services.ObjectCachingService;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace SkripsiAppBackend.Services.AzureDevopsService
{
    public class RestAzureDevopsServiceMiddleware
    {
        private readonly RequestDelegate next;

        public RestAzureDevopsServiceMiddleware(RequestDelegate requestDelegate)
        {
            next = requestDelegate;
        }

        private static AuthController.SessionToken ValidateAuthenticationToken(string authToken, Configuration configuration)
        {
            var secretBytes = Encoding.UTF8.GetBytes(configuration.JwtSigningSecret);
            var payloadString = JWT.Decode(authToken, secretBytes, JwsAlgorithm.HS256);
            var payload = JsonSerializer.Deserialize<AuthController.SessionToken>(payloadString);

            if (payload == null)
            {
                throw new Exception("Invalid authentication JWT.");
            }

            return payload;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            RestAzureDevopsService azureDevopsService,
            Configuration configuration)
        {
            var authCookie = httpContext.Request.Cookies["auth"];
            if (authCookie != null)
            {
                var authToken = authCookie.Split(' ')[1];

                try
                {
                    var sessionToken = ValidateAuthenticationToken(authToken, configuration);

                    azureDevopsService.SetProfile(
                        sessionToken.profileId,
                        sessionToken.publicAlias,
                        sessionToken.displayName,
                        sessionToken.refreshToken,
                        sessionToken.sessionId
                    );
                }
                catch
                {
                    httpContext.Response.Cookies.Delete("auth");
                }
            }

            await next(httpContext);
        }
    }

    public enum AzureDevopsServiceType
    {
        REST
    }

    public static class RestAzureDevopsServiceMiddlewareExtensions
    {
        public static void AddAzureDevopsService(this IServiceCollection services, AzureDevopsServiceType type)
        {
            if (type == AzureDevopsServiceType.REST)
            {
                services.AddScoped<RestAzureDevopsService>();
                services.AddScoped<IAzureDevopsService>(service =>
                {
                    var azureDevops = service.GetService<RestAzureDevopsService>();
                    var cache = service.GetService<InMemoryUniversalCachingService>();

                    return new AzureDevopsCachingProxy(azureDevops, cache);
                });
            }
            else
            {
                throw new Exception("Invalid service type");
            }
        }
        public static IApplicationBuilder UseAzureDevopsService(this IApplicationBuilder builder, AzureDevopsServiceType type)
        {
            if (type == AzureDevopsServiceType.REST)
            {
                return builder.UseMiddleware<RestAzureDevopsServiceMiddleware>();
            }
            else
            {
                throw new Exception("Invalid service type");
            }
        }
    }
}
