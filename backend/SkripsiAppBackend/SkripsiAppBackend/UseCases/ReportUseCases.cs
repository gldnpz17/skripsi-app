using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.UseCases
{
    public class ReportUseCases
    {
        private readonly Database database;
        private readonly IAzureDevopsService azureDevopsService;
        private readonly TeamUseCases teamUseCases;

        public ReportUseCases(
            Database database,
            IAzureDevopsService azureDevopsService,
            TeamUseCases teamUseCases)
        {
            this.database = database;
            this.azureDevopsService = azureDevopsService;
            this.teamUseCases = teamUseCases;
        }

        public struct AvailableReport
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
        }

        public struct ReportSprint
        {
            public IAzureDevopsService.Sprint Sprint { get; set; }
            public DateTime AccountedStartDate { get; set; }
            public DateTime AccountedEndDate { get; set; }
            public double AccountedEffort { get; set; }
            public double AccountedWorkFactor { get; set; }
        }

        public async Task<List<ReportSprint>> GetTimespanSprints(string organizationName, string projectId, string teamId, DateTime startDate, DateTime endDate)
        {
            var sprints = await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId);

            var accountedSprints = sprints
                .Where(sprint => sprint.StartDate.HasValue && sprint.EndDate.HasValue)
                .Where(sprint =>
                    sprint.StartDate >= startDate && sprint.EndDate <= endDate ||
                    sprint.StartDate <= startDate && sprint.EndDate >= startDate ||
                    sprint.StartDate <= endDate && sprint.EndDate >= endDate
                )
                .Select(async (sprint) =>
                {
                    DateTime sprintStartDate = (DateTime)sprint.StartDate;
                    DateTime sprintEndDate = (DateTime)sprint.EndDate;

                    var accountedStartDate = new DateTime(Math.Max(sprintStartDate.Ticks, startDate.Ticks));
                    var accountedEndDate = new DateTime(Math.Min(sprintEndDate.Ticks, endDate.Ticks));

                    var accountedWorkFactor = (accountedEndDate - accountedStartDate).TotalDays / (sprintStartDate - sprintEndDate).TotalDays;

                    var sprintWorkItems = await azureDevopsService.ReadSprintWorkItems(organizationName, projectId, teamId, sprint.Id);
                    var totalEffort = teamUseCases.CalculateTotalEffort(sprintWorkItems);

                    return new ReportSprint()
                    {
                        Sprint = sprint,
                        AccountedWorkFactor = accountedWorkFactor,
                        AccountedStartDate = accountedStartDate,
                        AccountedEndDate = accountedEndDate,
                        AccountedEffort = totalEffort * accountedWorkFactor
                    };
                });

            await Task.WhenAll(accountedSprints);

            return accountedSprints.Select(task => task.Result).ToList();
        }

        public async Task<List<AvailableReport>> ListAvailableReports(string organizationName, string projectId, string teamId)
        {
            var sprints = await azureDevopsService.ReadTeamSprints(organizationName, projectId, teamId);

            if (sprints.Count == 0)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_SPRINTS);
            }

            var startDate = (DateTime)sprints
                .FindAll(sprint => sprint.StartDate != null)
                .OrderBy(sprint => sprint.StartDate)
                .First().StartDate;
            var endDate = (DateTime)sprints
                .FindAll(sprint => sprint.EndDate != null)
                .OrderBy(sprint => sprint.EndDate)
                .Last().EndDate;

            var existingReports = await database.Reports.ReadTeamReports(new TrackedTeamKey()
            {
                OrganizationName = organizationName,
                ProjectId = projectId,
                TeamId = teamId,
            });

            var currentMonthBeginning = new DateTime(startDate.Year, startDate.Month, 1);

            var availableReports = new List<AvailableReport>();

            while (currentMonthBeginning < endDate)
            {
                var currentMonthEnd = currentMonthBeginning.AddMonths(1).AddSeconds(-1);

                var collidingReport = existingReports
                    .FirstOrDefault(report =>
                        (report.StartDate >= currentMonthBeginning && report.EndDate <= currentMonthEnd) ||
                        (report.StartDate <= currentMonthBeginning && report.EndDate >= currentMonthEnd) ||
                        (report.StartDate <= currentMonthEnd && report.EndDate >= currentMonthBeginning)
                    );

                if (collidingReport == null)
                {
                    availableReports.Add(new AvailableReport()
                    {
                        StartDate = currentMonthBeginning,
                        EndDate = currentMonthEnd
                    });
                }

                currentMonthBeginning = currentMonthBeginning.AddMonths(1);
            }

            return availableReports;
        }
    }
}
