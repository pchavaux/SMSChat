//-----------------------------------------------------------------------------
// Filename: SIPSoftPhoneState.cs
//
// Description: A helper class to load the application's settings and to hold 
// some application wide variables. 
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
// 
// History:
// 27 Mar 2012	Aaron Clauson	Refactored, Hobart, Australia.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Configuration;
using System.Net;
using System.Xml;


namespace SMSChat.Services
{
    public class SIPSoftPhoneState : IConfigurationSectionHandler
    {
        private const string SIPSOFTPHONE_CONFIGNODE_NAME = "sipsoftphone";
        private const string SIPSOCKETS_CONFIGNODE_NAME = "sipsockets";
        private const string STUN_SERVER_KEY = "STUNServerHostname";

        private static readonly XmlNode m_sipSoftPhoneConfigNode;
        public static readonly XmlNode SIPSocketsNode;
        public static readonly string STUNServerHostname;

        public static readonly string SIPUsername = "184942";    // Get the SIP username from the config file.
        public static readonly string SIPPassword = "y+Rm+OARzI2*";    // Get the SIP password from the config file.
        public static readonly string SIPServer = "208.100.60.10";        // Get the SIP server from the config file.
        public static readonly string SIPFromName = "Softphone Sample";    // Get the SIP From display name from the config file.
        public static readonly bool UseAudioScope = false;
        public static int AudioOutDeviceIndex = -1;

        public static IPAddress PublicIPAddress;

        static SIPSoftPhoneState()
        {
            AddDebugLogger();


            if (m_sipSoftPhoneConfigNode != null)
            {
                SIPSocketsNode = m_sipSoftPhoneConfigNode.SelectSingleNode(SIPSOCKETS_CONFIGNODE_NAME);
            }

            STUNServerHostname = "";
        }

        /// <summary>
        /// Handler for processing the App.Config file and retrieving a custom XML node.
        /// </summary>
        public object Create(object parent, object context, XmlNode configSection)
        {
            return configSection;
        }

        private static void AddDebugLogger()
        {
        
        }
    }
}
