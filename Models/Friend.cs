using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using SMSChat.Data;

namespace SMSChat.Models
{
    public class Friend
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string FriendSipPhoneNumber { get; set; }
        public string UserId { get; set; } // FK to Identity User
        public ApplicationUser User { get; set; } // Navigation property
    }
}
