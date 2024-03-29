﻿using Microsoft.AspNetCore.Authorization;
using SkripsiAppBackend.Common.Authentication;
using SkripsiAppBackend.Persistence.Models;

namespace SkripsiAppBackend.Common.Authorization
{
    public class AllowTeamMemberHandler : AuthorizationHandler<AllowTeamMemberRequirement, TrackedTeam>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            AllowTeamMemberRequirement requirement, 
            TrackedTeam resource)
        {
            try
            {
                var userTeams = context.User.GetTeams();

                if (userTeams.Any(userTeam => userTeam.Equals(resource)))
                {
                    context.Succeed(requirement);
                }
            }
            catch (Exception _)
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }

    public class AllowTeamMemberRequirement : IAuthorizationRequirement { }
}
