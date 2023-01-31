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
            var teams = JsonSerializer.Deserialize<List<AuthenticationMiddleware.ProfileTeam>>(principal.FindFirstValue("teams"));

            if (teams == null)
            {
                throw new Exception("Unable to access profile teams");
            }

            return teams;
        }
    }
}
