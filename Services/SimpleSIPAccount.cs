using SIPSorcery.SIP.App;
using SIPSorcery.SIP;

namespace SMSChat.Services
{
    public class SimpleSIPAccount : ISIPAccount
    {
        public string SIPUsername { get; set; }
        public string SIPDomain { get; set; }
        public string SIPPassword { get; set; }

        public string ID => throw new NotImplementedException();

        public string HA1Digest => throw new NotImplementedException();

        public bool IsDisabled => throw new NotImplementedException();

        public SimpleSIPAccount(string username, string domain, string password)
        {
            SIPUsername = username;
            SIPDomain = domain;
            SIPPassword = password;
        }

        public bool IsAuthenticated(SIPRequest sipRequest) => true; // Implement actual logic if needed.
        public bool HasSIPUsername(string username) => username == SIPUsername;
    }
}
