using System.ComponentModel.DataAnnotations;

namespace SMSChat.Models
{
    public class SmsRequest
    {
        [Key]
        public int Id { get; set; }
        public string Did { get; set; }
        public string Dst { get; set; }
        public string Message { get; set; }
    }
}
