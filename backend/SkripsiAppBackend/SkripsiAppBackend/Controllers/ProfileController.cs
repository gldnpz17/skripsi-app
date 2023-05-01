using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Services;
using Flurl.Http;
using SkripsiAppBackend.Services.AzureDevopsService;
using Microsoft.AspNetCore.Authorization;
using SkripsiAppBackend.Common;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private IAzureDevopsService azureDevopsService;
        public ProfileController(IAzureDevopsService azureDevopsService)
        {
            this.azureDevopsService = azureDevopsService;
        }

        public struct Profile
        {
            public string Name { get; set; }
        }

        [HttpGet("self")]
        [Authorize(Policy = AuthorizationPolicies.AllowAuthenticated)]
        public async Task<ActionResult<Profile>> GetSelfProfile()
        {
            var profile = azureDevopsService.ReadSelfProfile();

            return new Profile()
            {
                Name = profile.DisplayName
            };
        }
    }
}
