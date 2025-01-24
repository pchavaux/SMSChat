using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SMSChat.Models
{
    public class ChatThread
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
            public string UserId { get; set; } // FK to Identity User
            public string FriendPhoneNumber { get; set; } // Friend's phone number
            public DateTime LastMessageAt { get; set; }

            // Navigation property for related messages
            public ICollection<Message> Messages { get; set; }      

    }
}
