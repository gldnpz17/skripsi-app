using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Services;
using Flurl.Http;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/profile")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private AccessTokenService accessTokenService;
        public ProfileController(AccessTokenService accessTokenService)
        {
            this.accessTokenService = accessTokenService;
        }

        public struct Profile
        {
            public string Name { get; set; }
        }

        private struct GetProfileResponse
        {
            public string displayName { get; set; }
            public string publicAlias { get; set; }
        }

        [HttpGet("self")]
        public async Task<ActionResult<Profile>> GetSelfProfile()
        {
            var response = await "https://app.vssps.visualstudio.com/_apis/profile/profiles/me?api-version=7.1-preview.3"
                .WithHeader("Authorization", await accessTokenService.GetToken(HttpContext))
                .GetAsync()
                .ReceiveJson<GetProfileResponse>();

            return new Profile()
            {
                Name = response.displayName
            };
        }
    }
}
