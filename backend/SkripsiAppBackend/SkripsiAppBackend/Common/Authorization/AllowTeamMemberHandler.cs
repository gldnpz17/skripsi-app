using Microsoft.AspNetCore.Authorization;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.DomainModel;

namespace SkripsiAppBackend.Common.Authorization
{
    public class AllowTeamMemberHandler : AuthorizationHandler<AllowTeamMemberRequirement, TrackedTeam>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            AllowTeamMemberRequirement requirement, 
            TrackedTeam resource)
        {
            var userTeams = context.User.GetTeams();

            if (userTeams.Any(userTeam => userTeam.Equals(resource)))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class AllowTeamMemberRequirement : IAuthorizationRequirement { }
}
