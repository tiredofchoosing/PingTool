using System.Text;

namespace PingTool
{
    class Status
    {
        public int Count { get; set; }
        public double Avg { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double Median { get; set; }
        public int? Filtered { get; set; }
        public int Lost { get; set; }
        public StatusMessages StatusMessage { get; set; }

        public void Update(int count, double avg, double min, double max, double med, StatusMessages msg = StatusMessages.Running)
        {
            Count = count;
            Avg = avg;
            Min = min;
            Max = max;
            Median = med;
            StatusMessage = msg;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append(StatusMessage.ToString());
            if (Count > 0)
            {
                sb.Append($"   |   Total: {Count} | Avg: {Avg} | Min: {Min} | Max: {Max} | Median: {Median} | Lost: {Lost}");
                if (Filtered.HasValue)
                {
                    sb.Append($"   |   Filtered: {Filtered.Value}");
                }
            }

            return sb.ToString();
        }
    }

    enum StatusMessages
    {
        Starting,
        Stopped,
        Running,
        HostError,
        PingError
    }
}
