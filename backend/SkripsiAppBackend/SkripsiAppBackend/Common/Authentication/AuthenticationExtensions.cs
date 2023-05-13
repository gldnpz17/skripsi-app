using SkripsiAppBackend.Services.AzureDevopsService;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;

namespace SkripsiAppBackend.Common.Authentication
{
    public static class AuthenticationExtensions
    {
        public static List<AuthenticationMiddleware.ProfileTeam> GetTeams(this ClaimsPrincipal principal)
        {
            var teamsString = principal.FindFirstValue("teams");

            if (teamsString == null)
            {
                throw new Exception("Unable to access profile teams");
            }

            var teams = JsonSerializer.Deserialize<List<AuthenticationMiddleware.ProfileTeam>>(teamsString);

            return teams;
        }
    }
}
