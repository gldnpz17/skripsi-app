using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.LoggingService;
using SkripsiAppBackend.UseCases.Extensions;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;
using static SkripsiAppBackend.Services.LoggingService.LoggingService.CalculationLog;

namespace SkripsiAppBackend.Calculations
{
    public class MiscellaneousCalculations
    {
        private readonly TeamEvmCalculations evm;
        private readonly CommonCalculations common;
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;
        private readonly LoggingService logging;

        public MiscellaneousCalculations(
            TeamEvmCalculations evm,
            CommonCalculations common,
            Database database,
            IAzureDevopsService azureDevops,
            LoggingService logging)
        {
            this.evm = evm;
            this.common = common;
            this.database = database;
            this.azureDevops = azureDevops;
            this.logging = logging;
        }

        public async Task<long> CalculateRemainingBudget(string organizationName, string projectId, string teamId, DateTime now)
        {
            var budgetAtCompletion = evm.CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var actualCost = evm.CalculateActualCost(organizationName, projectId, teamId, now);

            await Task.WhenAll(budgetAtCompletion, actualCost);

            return budgetAtCompletion.Result - actualCost.Result;
        }

        public async Task<DateTime> CalculateEstimatedCompletionDate(
            string organizationName,
            string projectId,
            string teamId,
            DateTime now)
        {
            var log = logging.CreateCalculationLog("Estimated Completion Date");
            log.Argument(new Args(organizationName, projectId, teamId, now));
            var teamKey = new TrackedTeamKey(organizationName, projectId, teamId);

            var schedulePerformanceIndex = evm.CalculateSchedulePerformanceIndex(organizationName, projectId, teamId, now);
            var startDate = common.GetTeamStartDate(organizationName, projectId, teamId);
            var team = database.TrackedTeams.ReadByKey(teamKey);
            var workDays = azureDevops.ReadTeamWorkDays(organizationName, projectId, teamId);

            await Task.WhenAll(schedulePerformanceIndex, startDate, team, workDays);

            if (!team.Result.Deadline.HasValue)
            {
                throw new UserFacingException(UserFacingException.ErrorCodes.TEAM_NO_DEADLINE);
            }

            log.Record($"Start date = {startDate.Result}");
            log.Record($"Deadline = {team.Result.Deadline}");

            var plannedDuration = startDate.Result.WorkingDaysUntil((DateTime)team.Result.Deadline, workDays.Result);
            log.Record($"Planned duration = {plannedDuration}");
            log.Record($"Work days = {workDays.Result}");
            log.Record($"SPI = {schedulePerformanceIndex.Result}");

            var estimatedDuration = plannedDuration / schedulePerformanceIndex.Result;
            log.Record($"Estimated duration = {estimatedDuration}");

            var estimatedCompletionDate = startDate.Result.AddWorkingDays(estimatedDuration, workDays.Result);
            log.Record($"Estimated completion date = {estimatedCompletionDate}");

            log.Finish();

            return estimatedCompletionDate;
        }
    }
}
