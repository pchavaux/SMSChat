using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SMSChat.Models
{
    public class SystemCredentials
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Token { get; set; }
        public string? APIUrl { get; set; }
        public string? APIPassword { get; set; }
    }
}
