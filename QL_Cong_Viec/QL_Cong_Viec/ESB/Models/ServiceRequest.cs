namespace QL_Cong_Viec.ESB.Models
{
    public class ServiceRequest
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public string ServiceName { get; set; }
        public string Operation { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string SourceService { get; set; }
    }
}
