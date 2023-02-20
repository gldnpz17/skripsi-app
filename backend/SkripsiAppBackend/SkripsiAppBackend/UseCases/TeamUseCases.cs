﻿using System.Reflection.Metadata.Ecma335;
using static SkripsiAppBackend.Services.AzureDevopsService.IAzureDevopsService;

namespace SkripsiAppBackend.UseCases
{
    public class TeamUseCases
    {
        public struct SprintWorkItems
        {
            public Sprint Sprint { get; set; }
            public List<WorkItem> WorkItems { get; set; }
        }

        public DateTime GetEstimatedEndDate(DateTime startDate, int remainingWorkingDays, List<DayOfWeek> workDays)
        {
            // TODO: Probably would need to accomodate for national holidays?
            var endDate = new DateTime(startDate.Ticks);
            while (remainingWorkingDays > 0)
            {
                endDate = endDate.AddDays(1);
                if (workDays.Contains(endDate.DayOfWeek))
                {
                    remainingWorkingDays--;
                }
            }

            return endDate;
        }

        public int CalculateRemainingWorkingDays(double averageVelocity, List<WorkItem> unfinishedWorkItems)
        {
            double remainingEffort = CalculateTotalEffort(unfinishedWorkItems);

            return Convert.ToInt32(Math.Ceiling(remainingEffort / averageVelocity));
        }

        public double CalculateAverageVelocity(List<SprintWorkItems> sprintWorkItems)
        {
            double totalDays = 0;
            double totalEffort = 0;
            foreach (var sprintWorkItem in sprintWorkItems)
            {
                totalDays += GetSprintDurationInDays(sprintWorkItem.Sprint);
                totalEffort += CalculateTotalEffort(sprintWorkItem.WorkItems);
            }

            return totalEffort / totalDays;
        }

        public double CalculateVelocity(Sprint sprint, List<WorkItem> workItems)
        {
            double days = GetSprintDurationInDays(sprint);
            double totalEffort = CalculateTotalEffort(workItems);

            return totalEffort / days;
        }

        private static double CalculateTotalEffort(List<WorkItem> workItems)
        {
            double totalEffort = 0;
            foreach (var workItem in workItems)
            {
                totalEffort += workItem.Effort;
            }

            return totalEffort;
        }

        private static double GetSprintDurationInDays(Sprint sprint)
        {
            if (sprint.StartDate == null || sprint.EndDate == null)
            {
                throw new ArgumentException("Invalid sprint start or end date.");
            }

            return ((DateTime)sprint.EndDate - (DateTime)sprint.StartDate).TotalDays;
        }
    }
}
