using System.Net.NetworkInformation;

namespace PingTool
{
    class PingResult
    {
        public DateTime DateTime { get; }
        public int Value { get; }

        public PingResult(DateTime dateTime, int value)
        {
            DateTime = dateTime;
            Value = value;
        }
        public PingResult(PingReply reply) : this(DateTime.Now, (int)reply.RoundtripTime)
        { }
    }
}
