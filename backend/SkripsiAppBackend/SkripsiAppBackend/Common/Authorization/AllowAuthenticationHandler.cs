using Microsoft.AspNetCore.Authorization;

namespace SkripsiAppBackend.Common.Authorization
{
    public class AllowAuthenticationHandler : AuthorizationHandler<AllowAuthenticatedRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AllowAuthenticatedRequirement requirement)
        {
            if (context.User == null)
            {
                context.Fail();
            }
            else
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class AllowAuthenticatedRequirement : IAuthorizationRequirement { }
}
