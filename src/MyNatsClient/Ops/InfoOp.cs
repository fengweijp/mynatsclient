namespace NatsFun.Ops
{
    public class InfoOp : IOp
    {
        public string Code => "INFO";
        public string Message { get; }

        public InfoOp(string message)
        {
            Message = message;
        }

        public string GetAsString()
        {
            return $"INFO {Message}\r\n";
        }
    }
}