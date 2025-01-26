using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMSChat.Models
{
    public class Customer
    {
    [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string FriendSipPhoneNumber { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        // FK to Identity User
        //public ApplicationUser User { get; set; } // Navigation property
    }
}
