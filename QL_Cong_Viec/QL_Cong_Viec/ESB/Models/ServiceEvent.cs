namespace QL_Cong_Viec.ESB.Models
{
    public class ServiceEvent
    {
        public string EventId { get; set; } = Guid.NewGuid().ToString();
        public string EventType { get; set; }
        public string SourceService { get; set; }
        public object Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
