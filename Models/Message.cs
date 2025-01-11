using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SMSChat.Models
{
    public class Message
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
            public string SenderPhoneNumber { get; set; }
            public string RecipientPhoneNumber { get; set; }
            public string Content { get; set; }
            public DateTime SentAt { get; set; }
            public bool IsRead { get; set; }
            public string MessageType { get; set; } // e.g., "sent" or "received"


            // Foreign Key to ChatThread
            public int ChatThreadId { get; set; }
            public ChatThread ChatThread { get; set; } // Navigation property
        }

    
}
