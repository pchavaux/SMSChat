using SIPSorcery.SIP;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

using SIPSorcery.Media;

using SIPSorcery.SIP.App;
using System.Diagnostics;
using SIPSorcery.Net;
using SIPSorceryMedia.Encoders;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System.Threading;


namespace SMSChat.Services

{
    public class SipService : IDisposable
    {
        private SIPTransport _sipTransport;
        private SIPUserAgent _userAgent; // SIP client user agent
        private VoIPMediaSession _mediaSession;

        private RTPSession _rtpSession;

        private SIPServerUserAgent _serverUserAgent;
        public event Action<string> OnStatusChanged;
        public event Action<SipService> CallAnswer;                 // Fires when an outgoing SIP call is answered.
        public event Action<SipService> CallEnded;
        public event Action<SipService> IncomingCall;

        private readonly ILogger<SipService> _logger;
        //private string m_sipUsername = "184942_vm";
        //private string m_sipPassword = "#{J2{e[{+P!f";
        //private string m_sipServer = "208.100.60.10";
        //private string m_sipFromName = "Softphone Sample";
        //private string m_sipUsername = "184942_1003";
        //private string m_sipPassword = "12P15a57ul!";
        private string m_sipFromName = "Softphone Sample";
        private string m_sipUsername = "1003";
        private string m_sipPassword = "12P15a57ul!";
        private string m_sipServer = "162.221.94.162";
        private SIPServerUserAgent _pendingIncomingCall;
        private readonly IJSRuntime _jsRuntime;
        // Fires when an incoming or outgoing call is over.
        public event Action<SipService, string> StatusMessage;      // Fires when the SIP client has a status message it wants to inform the UI about.

        public event Action<SipService> RemotePutOnHold;            // Fires when the remote call party puts us on hold.	
        public event Action<SipService> RemoteTookOffHold;
       
        public SipService(IJSRuntime jsRuntime,ILogger<SipService> logger)
        {
            // Initialize SIP transport and User Agent
            _sipTransport = new SIPTransport();
            _userAgent = new SIPUserAgent(_sipTransport, null);
            
            _sipTransport.EnableTraceLogs();  // Enables detailed SIP message tracing
            _jsRuntime = jsRuntime;
            _logger = logger;
            // Set up logging for SIPSorcery
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            SIPSorcery.LogFactory.Set(loggerFactory);
            // Subscribe to call state events to monitor call progress
           // _sipTransport.SIPTransportRequestReceived += OnIncomingSipRequest;

            // Subscribe to call state events to monitor call progress
            _userAgent.ClientCallTrying += (uac, resp) => LogStatus($"Call trying: {resp.StatusCode} {resp.ReasonPhrase}");
            _userAgent.ClientCallRinging += (uac, resp) => LogStatus($"Call ringing: {resp.StatusCode} {resp.ReasonPhrase}");
            _userAgent.ClientCallAnswered += (uac, response) => OnCallAnswered(uac, response);
            _userAgent.ClientCallFailed += (uac, err, resp) => LogStatus($"Call failed: {err}, Status code: {resp?.StatusCode}");
            _userAgent.OnCallHungup += OnCallHungupHandler;

            // Subscribe to the OnIncomingCall event
            _userAgent.OnIncomingCall += async (ua, req) =>
            {
                if (req.Method == SIPMethodsEnum.INVITE)
                {
                 _logger.LogInformation("Incoming call detected.");

                    // Accept the incoming call to create a SIPServerUserAgent
                    var uas = ua.AcceptCall(req);

                    if (uas != null)
                    {
                        // Create a media session for the call
                        var mediaSession = CreateMediaSession();

                        // Answer the call
                        bool answerResult = await ua.Answer(uas, mediaSession);

                        if (answerResult)
                        {
                           _logger.LogInformation("Call answered successfully.");
                        }
                        else
                        {
                            _logger.LogInformation("Failed to answer the call.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Failed to accept the call.");
                    }
                }
            };

            //  _userAgent.ServerTransactionReceivedResponse += OnSIPResponseReceived;
        }




        private void LogStatus(string message)
        {
            // Log to both OnStatusChanged event and Debug output
            OnStatusChanged?.Invoke(message);
            Debug.WriteLine(message);
        }


        // Event handler when the call is hung up by the remote party
        private void CallHungup(SIPDialogue dialogue)
        {
            OnStatusChanged?.Invoke("Call ended.");
            Debug.WriteLine("Call ended by the remote party.");
        }


        private void OnCallHungupHandler(SIPDialogue dialogue)
        {
            OnStatusChanged?.Invoke("Call ended.");
        }

        public async Task Register(string username, string password, string serverUri)
        {
            // Ensure required parameters are provided
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(serverUri))
            {
                OnStatusChanged?.Invoke("Missing registration parameters.");
                Debug.WriteLine("Registration failed: Missing parameters.");
                return;
            }

            // Construct the SIP URI
            var sipUri = SIPURI.ParseSIPURI($"sip:{username}@{serverUri}");

            // Set up a registration agent
            var regAgent = new SIPRegistrationUserAgent(
                _sipTransport, username, password, serverUri, 120); // Adjust the expiry time if needed

            // Handle registration events
            regAgent.RegistrationFailed += (uri, resp, err) =>
            {
                OnStatusChanged?.Invoke($"Registration failed: {err}");
                _logger.LogError($"Registration failed for {uri}: {err}");
            };

            regAgent.RegistrationTemporaryFailure += (uri, resp, msg) =>
            {
                OnStatusChanged?.Invoke($"Registration temporary failure: {msg}");
                _logger.LogError($"Registration temporary failure for {uri}: {msg}");
            };

            regAgent.RegistrationSuccessful += (uri, resp) =>
            {
                OnStatusChanged?.Invoke("Registration successful.");
                _logger.LogInformation($"Registration successful for {uri}");
            };
            bool isRegistered = false;

            // Subscribe to registration events.
            regAgent.RegistrationSuccessful += (uri, resp) => isRegistered = true;
            regAgent.RegistrationFailed += (uri, resp, err) => isRegistered = false;

            // Attempt registration
            regAgent.Start();

            OnStatusChanged?.Invoke("Registration attempted.");
            _logger.LogInformation("Registration attempted.");
            await Task.Run(() =>
            {
                while (!isRegistered)
                {
                    Task.Delay(100).Wait(); // Small delay to prevent busy-waiting.
                }
            });

            if (isRegistered)
            {
                OnStatusChanged?.Invoke("Registration successful.");
                _logger.LogInformation($"Registration successful for {username}@{serverUri}");
            }
            else
            {
                OnStatusChanged?.Invoke("Registration failed.");
                _logger.LogError($"Registration failed for {username}@{serverUri}");
            }
            // Let the task complete for async compatibility
            await Task.CompletedTask;

        }

        public Task<bool> Call(string destinationUri, string username, string password, IMediaSession mediaSession)
        {
            // Validate and parse the destination URI
            if (!SIPURI.TryParse(destinationUri, out var dstUri))
            {
                Debug.WriteLine("Call error: The destination URI is invalid.");
                throw new ApplicationException("The destination was not recognized as a valid SIP URI.");
            }

            SIPCallDescriptor callDescriptor = new SIPCallDescriptor(
                username ?? SIPConstants.SIP_DEFAULT_USERNAME,
                password,
                dstUri.ToString(),
                SIPConstants.SIP_DEFAULT_FROMURI,
                dstUri.CanonicalAddress,
                null, null, null,
                SIPCallDirection.Out,
                SDP.SDP_MIME_CONTENTTYPE,
                null,
                null);

            Debug.WriteLine("Initiating Call to " + dstUri);
            Debug.WriteLine("CallDescriptor created with:");
            Debug.WriteLine($"Username: {username}, Password: {password}, Destination URI: {destinationUri}");

            // Check if media session is initialized
            if (mediaSession == null)
            {
                Debug.WriteLine("Error: Media session not initialized.");
                throw new InvalidOperationException("Media session is not initialized.");
            }

            return _userAgent.Call(callDescriptor, mediaSession);
        }

        //public async Task Dial()
        //{
        //    Debug.WriteLine("Dial method initiated with default number.");
        //    //await DialNumber("6362935405");
        //    Debug.WriteLine("Dial method completed.");
        //}
        //private async Task DialNumber(string number)
        //{
        //    // SIP configuration based on the previous context
        //    string username = "184942";
        //    string password = "y+Rm+OARzI2*";
        //    string serverUri = "sip:chicago3.voip.ms";
        //    string destinationUri = "sip:6362935405@chicago3.voip.ms";
        //    string fromUri = $"sip:{username}@chicago3.voip.ms";
        //    string routeSet = null;
        //    List<string> customHeaders = null;
        //    string authUsername = username;  // Authentication username typically matches the SIP account username
        //    SIPCallDirection callDirection = SIPCallDirection.Out;
        //    string contentType = SDP.SDP_MIME_CONTENTTYPE;
        //    string content = null;  // SDP content, if available, would go here
        //    IPAddress mangleIPAddress = null;  // Leave null if no specific IP mangle is needed

        //    SIPCallDescriptor _sipCallDescriptor = new SIPCallDescriptor(
        //        username: username,
        //        password: password,
        //        uri: destinationUri,
        //        from: fromUri,
        //        to: destinationUri,
        //        routeSet: routeSet,
        //        customHeaders: customHeaders,
        //        authUsername: authUsername,
        //        callDirection: callDirection,
        //        contentType: contentType,
        //        content: content,
        //        mangleIPAddress: mangleIPAddress
        //    );
        //    Debug.WriteLine("Initializing media session...");
        //    _mediaSession = new VoIPMediaSession();

        //    // Validate media session initialization
        //    if (_mediaSession == null)
        //    {
        //        Debug.WriteLine("Error: Failed to initialize media session.");
        //        OnStatusChanged?.Invoke("Error: Failed to initialize media session.");
        //        return;
        //    }
        //    else
        //    {
        //        Debug.WriteLine("Media session initialized....");
        //    }
        //    try
        //    {
        //        var result = await _userAgent.Call(_sipCallDescriptor, _mediaSession);

        //        if (result)
        //        {
        //            Debug.WriteLine("Call successfully initiated.");
        //            OnStatusChanged?.Invoke("Call initiated.");

        //        }
        //        else
        //        {
        //            Debug.WriteLine("Call failed to start.");
        //            OnStatusChanged?.Invoke("Call failed to start.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Exception during call initiation: {ex.Message}");
        //        OnStatusChanged?.Invoke($"Call error: {ex.Message}");
        //    }

        //    Debug.WriteLine("DialNumber method completed.");
        //    try
        //    {

        //        var result = await _userAgent.Call(_sipCallDescriptor, _mediaSession);

        //        if (result)
        //        {
        //            Debug.WriteLine("Call successfully initiated.");
        //            OnStatusChanged?.Invoke("Call initiated.");
        //        }
        //        else
        //        {
        //            Debug.WriteLine("Call initiation failed.");
        //            OnStatusChanged?.Invoke("Call failed to start.");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        OnStatusChanged?.Invoke($"Call error: {ex.Message}");
        //        Debug.WriteLine($"Exception during call initiation: {ex.Message}");
        //    }
        //}


        public void Hangup()
        {
            if (_userAgent.IsCallActive)
            {
                _userAgent.Hangup();
                OnStatusChanged?.Invoke("Call ended.");
            }
            else
            {
                OnStatusChanged?.Invoke("No active call to hang up.");
            }
        }

        public void Dispose()
        {
            _userAgent?.Hangup();
            _sipTransport?.Shutdown();
            _mediaSession?.Dispose();

        }
        private async void OnCallAnswered(ISIPClientUserAgent uac, SIPResponse response)
        {
            LogStatus("Call answered. Establishing audio connection...");

            // Set up audio for microphone and speaker
            var audioSource = new WindowsAudioEndPoint(new AudioEncoder());
            var audioSink = new WindowsAudioEndPoint(new AudioEncoder());
            LogStatus("Initializing audio source and sink...");
            LogStatus("Call answered: " + response.StatusCode + " " + response.ReasonPhrase + ".");

            if (audioSource == null || audioSink == null)
            {
                LogStatus("Error: Audio source or sink is not initialized.");
                return;
            }

            // Configure VoIP media session with the audio source and sink
            // _mediaSession = new VoIPMediaSession(new MediaEndPoints { AudioSource = audioSource, AudioSink = audioSink });
            //  var userAgent = new SIPUserAgent();
            var winAudio = new WindowsAudioEndPoint(new AudioEncoder(), -1);
            var windowsAudioEndPoint = new WindowsAudioEndPoint(new AudioEncoder(), -1);
            winAudio.RestrictFormats(x => x.Codec == AudioCodecsEnum.PCMU);
            // _mediaSession = new VoIPMediaSession(winAudio.ToMediaEndPoints());
            _mediaSession = CreateMediaSession();
            _mediaSession.AcceptRtpFromAny = true;
            // Start media session for two-way audio
            await _mediaSession.Start();

            LogStatus("Audio session started.");
            CallAnswer?.Invoke(this);
        }
        private VoIPMediaSession CreateMediaSessionIncoming(SDP offerSDP)
        {
            // ... (Logic to select appropriate audio and video endpoints based on offerSDP)
            var windowsAudioEndPoint = new WindowsAudioEndPoint(new AudioEncoder(), -1);
            var windowsVideoEndPoint = new WindowsVideoEndPoint(new VpxVideoEncoder());

            MediaEndPoints mediaEndPoints = new MediaEndPoints
            {
                AudioSink = windowsAudioEndPoint,
                AudioSource = windowsAudioEndPoint,
                // TODO: Not working for calls to sip:music@iptel.org. AC 29 Sep 2024.
                //VideoSink = windowsVideoEndPoint,
                //VideoSource = windowsVideoEndPoint,
            };

            // Fallback video source if a Windows webcam cannot be accessed.
            var testPatternSource = new VideoTestPatternSource(new VpxVideoEncoder());

            var voipMediaSession = new VoIPMediaSession(mediaEndPoints, testPatternSource);
            voipMediaSession.AcceptRtpFromAny = true;

            return voipMediaSession;
        }
        private VoIPMediaSession CreateMediaSession()
        {
            //var audioEndPoint = new WebRtcAudioEndPoint(loggerFactory: loggerFactory);
            var windowsAudioEndPoint = new WindowsAudioEndPoint(new AudioEncoder(), -1);
            var windowsVideoEndPoint = new WindowsVideoEndPoint(new VpxVideoEncoder());

            MediaEndPoints mediaEndPoints = new MediaEndPoints
            {
                AudioSink = windowsAudioEndPoint,
                AudioSource = windowsAudioEndPoint,
                // TODO: Not working for calls to sip:music@iptel.org. AC 29 Sep 2024.
                //VideoSink = windowsVideoEndPoint,
                //VideoSource = windowsVideoEndPoint,
            };

            // Fallback video source if a Windows webcam cannot be accessed.
            var testPatternSource = new VideoTestPatternSource(new VpxVideoEncoder());

            var voipMediaSession = new VoIPMediaSession(mediaEndPoints, testPatternSource);
            voipMediaSession.AcceptRtpFromAny = true;

            return voipMediaSession;
        }

        private SIPTransport m_sipTransport;
        private SIPUserAgent m_userAgent { get; set; } = new SIPUserAgent();

        private static string _sdpMimeContentType = SDP.SDP_MIME_CONTENTTYPE;

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public VoIPMediaSession MediaSession { get; private set; }

        public void Hangup2()
        {
            if (m_userAgent.IsCallActive)
            {
                m_userAgent.Hangup();
                //  CallFinished(null);
            }
        }
        public async Task NewCallMethod(string destination)
        {
            Call(destination);
        }
        public async Task Call(string destination)
        {
            // Determine if this is a direct anonymous call or whether it should be placed using the pre-configured SIP server account. 
            SIPURI callURI = null;
            string sipUsername = null;
            string sipPassword = null;
            string fromHeader = null;

            if (destination.Contains("@") || m_sipServer == null)
            {

                callURI = SIPURI.ParseSIPURIRelaxed(destination);
                fromHeader = (new SIPFromHeader(m_sipFromName, SIPURI.ParseSIPURI(SIPFromHeader.DEFAULT_FROM_URI), null)).ToString();
            }
            else
            {
                // This call will use the pre-configured SIP account.
                callURI = SIPURI.ParseSIPURIRelaxed(destination + "@" + m_sipServer);
                sipUsername = m_sipUsername;
                sipPassword = m_sipPassword;
                fromHeader = (new SIPFromHeader(m_sipFromName, new SIPURI(m_sipUsername, m_sipServer, null), null)).ToString();
            }

            Debug.WriteLine($"Starting call to {callURI}.");

            var dstEndpoint = await SIPDns.ResolveAsync(callURI, false, _cts.Token);

            if (dstEndpoint == null)
            {
                Debug.WriteLine($"Call failed, could not resolve {callURI}.");
            }
            else
            {
                Debug.WriteLine($"Call progressing, resolved {callURI} to {dstEndpoint}.");
                System.Diagnostics.Debug.WriteLine($"DNS lookup result for {callURI}: {dstEndpoint}.");
                SIPCallDescriptor callDescriptor = new SIPCallDescriptor(sipUsername, sipPassword, callURI.ToString(), fromHeader, null, null, null, null, SIPCallDirection.Out, _sdpMimeContentType, null, null);

                MediaSession = CreateMediaSession();

                //   m_userAgent.RemotePutOnHold += OnRemotePutOnHold;
                //   m_userAgent.RemoteTookOffHold += OnRemoteTookOffHold;

                await m_userAgent.InitiateCallAsync(callDescriptor, MediaSession);
            }
        }
        public void Accept(SIPRequest sipRequest)
        {
            _pendingIncomingCall = m_userAgent.AcceptCall(sipRequest);
        }

        /// <summary>
        /// Answers an incoming SIP call.
        /// </summary>
        public async Task<bool> Answer2()
        {

            if (_pendingIncomingCall == null)
            {
                Debug.WriteLine($"There was no pending call available to answer.");
                return false;
            }
            else
            {
                var sipRequest = _pendingIncomingCall.ClientTransaction.TransactionRequest;

                // Assume that if the INVITE request does not contain an SDP offer that it will be an 
                // audio only call.
                bool hasAudio = true;
                bool hasVideo = false;

                if (sipRequest.Body != null)
                {
                    SDP offerSDP = SDP.ParseSDPDescription(sipRequest.Body);
                    hasAudio = offerSDP.Media.Any(x => x.Media == SDPMediaTypesEnum.audio && x.MediaStreamStatus != MediaStreamStatusEnum.Inactive);
                    hasVideo = offerSDP.Media.Any(x => x.Media == SDPMediaTypesEnum.video && x.MediaStreamStatus != MediaStreamStatusEnum.Inactive);
                }

                MediaSession = CreateMediaSession();

                m_userAgent.RemotePutOnHold += OnRemotePutOnHold;
                m_userAgent.RemoteTookOffHold += OnRemoteTookOffHold;

                bool result = await m_userAgent.Answer(_pendingIncomingCall, MediaSession);
                _pendingIncomingCall = null;

                return result;
            }
        }
        public async Task<bool> Answer()
        {
            if (_pendingIncomingCall == null)
            {
                Debug.WriteLine($"There was no pending call available to answer.");
                return false;
            }
            else
            {
                Debug.WriteLine("Incoming call is being detected...");
                var sipRequest = _pendingIncomingCall.ClientTransaction.TransactionRequest;

                // Assume that if the INVITE request does not contain an SDP offer that it will be an 
                // audio only call.
                bool hasAudio = true;
                bool hasVideo = false;
                SDP SDPInTransit = new SDP();
                if (sipRequest.Body != null)
                {
                    SDP offerSDP = SDP.ParseSDPDescription(sipRequest.Body);
                    Debug.WriteLine("SDP sipRequest.Body:" + sipRequest.Body.ToString());
                    hasAudio = offerSDP.Media.Any(x => x.Media == SDPMediaTypesEnum.audio && x.MediaStreamStatus != MediaStreamStatusEnum.Inactive);
                    hasVideo = offerSDP.Media.Any(x => x.Media == SDPMediaTypesEnum.video && x.MediaStreamStatus != MediaStreamStatusEnum.Inactive);
                    SDPInTransit = offerSDP;
                }
                var windowsAudioEndPoint = new WindowsAudioEndPoint(new AudioEncoder(), -1);
                var windowsVideoEndPoint = new WindowsVideoEndPoint(new VpxVideoEncoder());

                MediaEndPoints mediaEndPoints = new MediaEndPoints
                {
                    AudioSink = windowsAudioEndPoint,
                    AudioSource = windowsAudioEndPoint,
                    // TODO: Not working for calls to sip:music@iptel.org. AC 29 Sep 2024.
                    //VideoSink = windowsVideoEndPoint,
                    //VideoSource = windowsVideoEndPoint,
                };
                MediaSession = CreateMediaSessionIncoming(SDPInTransit);

                m_userAgent.RemotePutOnHold += OnRemotePutOnHold;
                m_userAgent.RemoteTookOffHold += OnRemoteTookOffHold;
                Debug.WriteLine("Answering call...");
                bool result = await m_userAgent.Answer(_pendingIncomingCall, MediaSession);
                // _pendingIncomingCall = null;
                Debug.WriteLine("Answered call...");
                // StatusMessage(this, "Call answered: " + sipResponse.StatusCode + " " + sipResponse.ReasonPhrase + ".");
                CallAnswer?.Invoke(this);
                return result;
            }
        }


        private void OnRemotePutOnHold()
        {
            RemotePutOnHold?.Invoke(this);
        }

        /// <summary>	
        /// Event handler that notifies us the remote party has taken us off hold.	
        /// </summary>	
        private void OnRemoteTookOffHold()
        {
            RemoteTookOffHold?.Invoke(this);
        }
        private async Task OnIncomingSipRequest(SIPEndPoint localSIPEndPoint, SIPEndPoint remoteEndPoint, SIPRequest sipRequest)
        {
            _logger.LogInformation("Starting OnIncomingSipRequest!");
            if (sipRequest.Method == SIPMethodsEnum.INVITE)
            {
                _logger.LogInformation("Received an invite! "+sipRequest.Body.ToString());
                LogStatus("Attempting to answer incoming call...");

                // Create a UAS transaction for the incoming INVITE request.
                var uasTransaction = new UASInviteTransaction(_sipTransport, sipRequest, localSIPEndPoint, false);
                _logger.LogInformation("Create a uas as localSIPEndPoint: " );
                // Initialize the SIPServerUserAgent with the UAS transaction.
                var serverUserAgent = new SIPServerUserAgent(_sipTransport, null, uasTransaction, null);

                // Send a 100 Trying response to acknowledge the INVITE.
                serverUserAgent.Progress(SIPResponseStatusCodesEnum.Trying, null, null, null, null);
                _logger.LogInformation("Create a media session: ");
                // Create a media session for handling audio.
                var audioEndpoint = new WindowsAudioEndPoint(new AudioEncoder());
                var mediaSession = new VoIPMediaSession(audioEndpoint.ToMediaEndPoints())
                {
                    AcceptRtpFromAny = true,
                };

                // Answer the call with the media session.
                SDP sdpOffer = mediaSession.CreateOffer(null);
                string sdpOfferString = sdpOffer.ToString();
                _logger.LogInformation("Create an sdp offer string: "+sdpOfferString);
                // Answer the call with the appropriate parameters
                serverUserAgent.Answer(SDP.SDP_MIME_CONTENTTYPE, sdpOfferString, null, SIPDialogueTransferModesEnum.NotAllowed);
                // Notify the UI or other components about the incoming call.
                OnStatusChanged?.Invoke("Incoming call answered.");
                IncomingCall?.Invoke(this);

                await Task.CompletedTask; // Ensures compatibility with async delegate
            }
        }


        

        public void CreateRTPSession()
        {
            // Initialize RTP session for audio
            _rtpSession = new RTPSession(false, false, false);
        }

        public async Task SendDTMFTone(char digit)
        {
            CreateRTPSession();
            if (_rtpSession == null)
            {
                Debug.WriteLine("RTP session not active. Cannot send DTMF.");
                return;
            }

            if (!char.IsDigit(digit) && "ABCD*#".IndexOf(digit) == -1)
            {
                Debug.WriteLine($"Invalid DTMF tone: {digit}");
                return;
            }

            try
            {
                // Map the DTMF digit to the corresponding RTP event ID (RFC 2833).
                int eventId = GetRTPEventId(digit);
                Debug.WriteLine($"Event Id: {eventId}");
                const int dtmfPayloadType = 101;
                // Create the RTPEvent for the DTMF tone.









                var dtmfEvent = new RTPEvent((byte)eventId, false, 5, 160, dtmfPayloadType);

                // Define a cancellation token for the operation.
                var cancellationToken = new CancellationTokenSource().Token;

                // Send the DTMF event via RTP.
                Debug.WriteLine($"Sending DTMF tone: {digit}");
                if (dtmfEvent == null)
                {
                    return;
                }
                else
                {
                    await _rtpSession.SendDtmfEvent(dtmfEvent, cancellationToken, 8000, 50); // 8000 Hz sample rate, volume 50.
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending DTMF tone: {ex.Message}");
            }
        }
        private int GetRTPEventId(char digit)
        {
            return digit switch
            {
                '0' => 0,
                '1' => 1,
                '2' => 2,
                '3' => 3,
                '4' => 4,
                '5' => 5,
                '6' => 6,
                '7' => 7,
                '8' => 8,
                '9' => 9,
                '*' => 10,
                '#' => 11,
                'A' => 12,
                'B' => 13,
                'C' => 14,
                'D' => 15,
                _ => throw new ArgumentException($"Invalid DTMF digit: {digit}")
            };
        }
        public async Task<bool> StartAudioCaptureAsync()
        {
            // Call JavaScript to start capturing audio
            return await _jsRuntime.InvokeAsync<bool>("webrtcInterop.startAudioCapture");
        }

        public async Task StopAudioCaptureAsync()
        {
            await _jsRuntime.InvokeVoidAsync("webrtcInterop.stopAudioCapture");
        }
        public async Task SendSipInfoRequest(string destination, char digit)
        {
            if (!char.IsDigit(digit) && "ABCD*#".IndexOf(digit) == -1)
            {
                Debug.WriteLine($"Invalid DTMF digit: {digit}");
                return;
            }

            try
            {
                // Construct the SIP URI and SIP INFO request.
                var sipUri = SIPURI.ParseSIPURI(destination);
                Debug.WriteLine($"SIPUri: {destination}");
                var sipInfoRequest = new SIPRequest(SIPMethodsEnum.INFO, sipUri);

                // Add DTMF payload.
                sipInfoRequest.Body = $"Signal={digit}\r\nDuration=160\r\n";
                sipInfoRequest.Header.ContentType = SDP.SDP_MIME_CONTENTTYPE;

                // Send the SIP INFO request.
                await _sipTransport.SendRequestAsync(sipInfoRequest);
                Debug.WriteLine($"SIP INFO sent to {destination} for DTMF: {digit}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending SIP INFO request: {ex.Message}");
            }
        }

        private async Task OnIncomingCall(SIPUserAgent userAgent, SIPRequest sipRequest)
        {
            if (sipRequest.Method == SIPMethodsEnum.INVITE)
            {
                Console.WriteLine("Incoming call detected.");

                // Create a media session for the call
                _mediaSession = CreateMediaSession();

                // Answer the call
              //  bool answerResult = await _userAgent.Answer(uas, _mediaSession);

                //if (answerResult)
                //{
                //    Console.WriteLine("Call answered successfully.");
                //}
                //else
                //{
                //    Console.WriteLine("Failed to answer the call.");
                //}
            }
        }

    }
}