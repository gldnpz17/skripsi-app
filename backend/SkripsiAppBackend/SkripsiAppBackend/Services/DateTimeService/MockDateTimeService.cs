namespace SkripsiAppBackend.Services.DateTimeService
{
    public class MockDateTimeService : IDateTimeService
    {
        public DateTime GetNow()
        {
            return DateTime.Parse("2023-03-15T00:00:00.000");
        }
    }
}
