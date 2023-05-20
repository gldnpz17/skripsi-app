namespace SkripsiAppBackend.Services.LoggingService
{
    public interface ILoggingStrategy
    {
        void Record(string log);
    }

    public class ConsoleLoggingStrategy : ILoggingStrategy
    {
        public void Record(string log)
        {
            Console.WriteLine(log);
        }
    }
}
