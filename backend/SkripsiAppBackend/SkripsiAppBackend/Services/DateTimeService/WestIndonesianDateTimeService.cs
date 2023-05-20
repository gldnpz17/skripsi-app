namespace SkripsiAppBackend.Services.DateTimeService
{
    public class WestIndonesianDateTimeService : IDateTimeService
    {
        public DateTime GetNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        }
    }
}
