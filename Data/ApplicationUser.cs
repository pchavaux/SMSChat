using Microsoft.AspNetCore.Identity;

namespace SMSChat.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? SipUserName { get; set; }
        public string? SipPassword { get; set; }
        public string? SipServer { get; set; }
        public string? SipPhoneNumber { get; set; }
    }
}
