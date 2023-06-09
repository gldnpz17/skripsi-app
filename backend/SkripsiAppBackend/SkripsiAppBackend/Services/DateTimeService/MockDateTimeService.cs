namespace SkripsiAppBackend.Services.DateTimeService
{
    public class MockDateTimeService : IDateTimeService
    {
        public DateTime GetNow()
        {
            return DateTime.Parse("2023-04-10T00:00:00.000");
        }
    }
}
