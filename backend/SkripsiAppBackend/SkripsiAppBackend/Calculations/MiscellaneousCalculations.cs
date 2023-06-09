using SkripsiAppBackend.Common.Exceptions;
using SkripsiAppBackend.Persistence;
using SkripsiAppBackend.Services.AzureDevopsService;
using SkripsiAppBackend.Services.LoggingService;
using SkripsiAppBackend.UseCases.Extensions;
using static SkripsiAppBackend.Persistence.Repositories.TrackedTeams.TrackedTeamsRepository;
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
    }
}
