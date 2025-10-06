namespace QL_Cong_Viec.ESB.Models
{
    public class ServiceHealth
    {
        public string ServiceName { get; set; }
        public bool IsHealthy { get; set; }
        public string Status { get; set; }
        public DateTime LastCheck { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
    }
}
