namespace QL_Cong_Viec.ESB.Models
{
    public class ServiceResponse
    {
        public string RequestId { get; set; }
        public bool Success { get; set; }
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan ProcessingTime { get; set; }
    }
}
