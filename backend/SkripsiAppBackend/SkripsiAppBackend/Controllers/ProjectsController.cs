using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Common;
using SkripsiAppBackend.Services.AzureDevopsService;
using System.Security.Cryptography.X509Certificates;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.Controllers
{
    [Route("api/projects")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly IAzureDevopsService azureDevopsService;

        public ProjectsController(IAzureDevopsService azureDevopsService)
        {
            this.azureDevopsService = azureDevopsService;
        }

        [HttpGet("/api/organizations/{organizationName}/projects")]
        [Authorize(Policy = AuthorizationPolicies.AllowAuthenticated)]
        public async Task<ActionResult<List<Project>>> ReadOrganizationProjects([FromRoute] string organizationName)
        {
            var projects = await azureDevopsService.ReadProjectsByOrganization(organizationName);

            return projects;
        }
    }
}
