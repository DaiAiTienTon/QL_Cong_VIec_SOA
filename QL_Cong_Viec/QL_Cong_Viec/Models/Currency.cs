namespace QL_Cong_Viec.Models
{
    public class Currency
    {
        public string From { get; set; } = string.Empty;


        public string To { get; set; } = string.Empty;


        public double Rate { get; set; }


        public DateTime Date { get; set; }


        public string Provider { get; set; } = string.Empty;
    }
}