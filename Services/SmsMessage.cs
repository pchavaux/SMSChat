namespace SMSChat.Services

{
    public class SmsMessage
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Type { get; set; } // "0" for received, "1" for sent
        public string Message { get; set; }
        public string Contact { get; set; }
    }
}
