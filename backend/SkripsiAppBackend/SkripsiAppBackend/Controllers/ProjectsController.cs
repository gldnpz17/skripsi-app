using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SkripsiAppBackend.Services.AzureDevopsService;
using System.Security.Cryptography.X509Certificates;

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

        public struct Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public IAzureDevopsService.Organization Organization { get; set; }
            
        }

        [HttpGet]
        public async Task<ActionResult<List<Project>>> ReadAllProjects()
        {
            var organizations = await azureDevopsService.ReadAllOrganizations();

            var projects =
                (await Task.WhenAll(
                    organizations.Select(organization =>
                    {
                        return Task.Run(async () =>
                        {
                            var projects = await azureDevopsService.ReadProjectsByOrganization(organization.Name);

                            return projects
                                .Select(project => new Project()
                                {
                                    Id = project.Id,
                                    Name = project.Name,
                                    Organization = organization
                                })
                                .ToList();
                        });
                    })
                ))
                .SelectMany((List<Project> project) => project);

            return projects.ToList();
        }
    }
}
