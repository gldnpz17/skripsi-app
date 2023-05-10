namespace SkripsiAppBackend.UseCases.Extensions
{
    public static class DateTimeExtensions
    {
        public static double WorkingDaysUntil(this DateTime startDate, DateTime endDate, List<DayOfWeek> workDays)
        {
            var currentDate = new DateTime(startDate.Ticks);
            double workingDays = 0;

            while (currentDate < endDate)
            {
                if (workDays.Contains(currentDate.DayOfWeek))
                {
                    workingDays += 1;
                }

                currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }

        public static DateTime AddWorkingDays(this DateTime startDate, double days, List<DayOfWeek> workDays)
        {
            var currentDate = new DateTime(startDate.Ticks);
            double remainingDays = days;

            while (remainingDays > 0)
            {
                if (workDays.Contains(currentDate.DayOfWeek))
                {
                    remainingDays -= 1;
                }

                currentDate.AddDays(1);
            }

            return currentDate;
        }
    }
}
