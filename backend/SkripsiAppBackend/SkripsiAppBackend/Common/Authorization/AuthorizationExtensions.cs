using Microsoft.AspNetCore.Authorization;
using SkripsiAppBackend.Persistence;
using System.Security.Claims;

namespace SkripsiAppBackend.Common.Authorization
{
    public static class AuthorizationExtensions
    {
        public static async Task<AuthorizationResult> AllowTeamMembers(
            this IAuthorizationService authorization,
            Database database,
            ClaimsPrincipal principal,
            string organizationName,
            string projectId,
            string teamId)
        {
            var trackedTeam = await database.TrackedTeams.ReadByKey(organizationName, projectId, teamId);
            return await authorization.AuthorizeAsync(principal, trackedTeam, AuthorizationPolicies.AllowTeamMember);
        }
    }
}
