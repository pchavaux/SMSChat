using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SMSChat.Models
{
    public class Attachment
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int MessageId { get; set; } // FK to Messages
        public string FilePath { get; set; }
        public string FileType { get; set; } // e.g., "image/png"
    }
}
