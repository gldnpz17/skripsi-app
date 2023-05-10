using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.UseCases.Extensions;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeamsRepository;

namespace SkripsiAppBackend.Calculations
{
    public class MiscellaneousCalculations
    {
        private readonly TeamEvmCalculations evm;
        private readonly CommonCalculations common;
        private readonly Database database;
        private readonly IAzureDevopsService azureDevops;

        public MiscellaneousCalculations(
            TeamEvmCalculations evm,
            CommonCalculations common,
            Database database,
            IAzureDevopsService azureDevops)
        {
            this.evm = evm;
            this.common = common;
            this.database = database;
            this.azureDevops = azureDevops;
        }

        public async Task<long> CalculateRemainingBudget(string organizationName, string projectId, string teamId, DateTime now)
        {
            var budgetAtCompletion = evm.CalculateBudgetAtCompletion(organizationName, projectId, teamId);
            var actualCost = evm.CalculateActualCost(organizationName, projectId, teamId, now);

            await Task.WhenAll(budgetAtCompletion, actualCost);

            return budgetAtCompletion.Result - actualCost.Result;
        }

        public async Task<DateTime> CalculateEstimatedCompletionDate(string organizationName, string projectId, string teamId, DateTime now)
        {
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

            var plannedDuration = startDate.Result.WorkingDaysUntil((DateTime)team.Result.Deadline, workDays.Result);
            var estimatedDuration = plannedDuration / schedulePerformanceIndex.Result;

            var estimatedCompletionDate = startDate.Result.AddWorkingDays(estimatedDuration, workDays.Result);

            return estimatedCompletionDate;
        }
    }
}
