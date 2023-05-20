using Microsoft.Extensions.ObjectPool;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SkripsiAppBackend.Services.LoggingService
{
    public class LoggingService
    {
        public ILoggingStrategy Strategy { get; set; }

        private Queue<List<string>> pendingLogs = new();
        private bool queueLocked = false;

        public class CalculationLog
        {
            private readonly LoggingService logging;
            private readonly string calculationName;
            private readonly List<KeyValuePair<string, object>> arguments = new();
            private readonly List<string> logs = new();

            public CalculationLog(LoggingService logging, string calculationName)
            {
                this.logging = logging;
                this.calculationName = calculationName;
            }

            public struct Args
            {
                public object[] Items { get; private set; }

                public Args(params object[] args)
                {
                    Items = args;
                }
            }

            public CalculationLog Argument(Args args, [CallerMemberName] string callerName = "")
            {
                var stackTrace = new StackTrace();
                var method = stackTrace.GetFrames().ToList()
                    .First(frame => frame.GetMethod().Name == callerName)
                    .GetMethod();

                var parameters = method.GetParameters();

                for (var i = 0; i < Math.Min(parameters.Length, args.Items.Length); i++)
                {
                    arguments.Add(new KeyValuePair<string, object>(parameters[i].Name, args.Items[i]));
                }

                return this;
            }

            public CalculationLog Argument(string argument, object data)
            {
                arguments.Add(new KeyValuePair<string, object>(argument, data));
                return this;
            }

            public void Record(string log)
            {
                logs.Add(log);
            }

            public void Finish()
            {
                var records = new List<string>();

                records.Add($"[Calculation] {calculationName}");
                arguments.ForEach(argument => records.Add($"{argument.Key} = {argument.Value}"));
                logs.ForEach(log => records.Add(log));

                logging.Record(records);
            }
        }

        public CalculationLog CreateCalculationLog(string calculationName)
        {
            return new CalculationLog(this, calculationName);
        }

        public void Record(List<string> logs)
        {
            pendingLogs.Enqueue(logs);

            if (!queueLocked)
            {
                queueLocked = true;
                Task.Run(() =>
                {
                    while (pendingLogs.Count > 0)
                    {
                        var logs = pendingLogs.Dequeue();
                        if (logs != null)
                        {
                            logs.ForEach(log => Strategy.Record(log));
                        }
                    }
                    queueLocked = false;
                });
            }
        }

        public void Record(string log)
        {
            Record(new List<string>() { log });
        }
    }
}
