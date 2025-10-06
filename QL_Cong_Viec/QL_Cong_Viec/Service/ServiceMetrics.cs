namespace QL_Cong_Viec.Service
{

    public class ServiceMetrics
    {
        public string ServiceName { get; set; }
        public int TotalCalls { get; set; }
        public int SuccessfulCalls { get; set; }
        public int FailedCalls { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public DateTime LastCallTime { get; set; }
        public double SuccessRate => TotalCalls > 0 ? (double)SuccessfulCalls / TotalCalls * 100 : 0;
    }
}
