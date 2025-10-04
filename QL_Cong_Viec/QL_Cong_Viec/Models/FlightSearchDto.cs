using System.ComponentModel.DataAnnotations;

namespace QL_Cong_Viec.Models
{
    public class FlightSearchDto
    {
        [Required]
        public string? From { get; set; }

        [Required]
        public string? To { get; set; }

 
    }
}
