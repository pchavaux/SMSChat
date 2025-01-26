 
namespace SMSChat.Models
{
    public class Thread
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SipNumber { get; set; }
        public int ChannelId { get; set; }
        public string Title { get; set; }
    }
}
