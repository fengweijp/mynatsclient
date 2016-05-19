namespace NatsFun.Logging
{
    public class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger() { }

        public void Info(string message) { }
        public void Error(string message) { }
    }
}