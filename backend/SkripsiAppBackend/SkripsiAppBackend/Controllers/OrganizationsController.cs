using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Services.AzureDevopsService;

namespace SkripsiAppBackend.Controllers
{
    [Route("/api/organizations")]
    [ApiController]
    public class OrganizationsController
    {
        private readonly IAzureDevopsService azureDevopsService;

        public OrganizationsController(IAzureDevopsService azureDevopsService)
        {
            this.azureDevopsService = azureDevopsService;
        }

        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.AllowAuthenticated)]
        public async Task<List<IAzureDevopsService.Organization>> ReadAllOrganizations()
        {
            var organizations = await azureDevopsService.ReadAllOrganizations();

            return organizations;
        }
    }
}
