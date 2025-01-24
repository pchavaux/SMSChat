using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace SMSChat.Services
{
    public class VoipMsSmsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "https://voip.ms/api/v1/rest.php"; // Corrected base URL
        private readonly string _bearerToken = "S0hCVVhjY25WS3dUUTRuL0hLT0tEUUVkMnczWTFJSW8vamtNMWhlcXN6TT0=";
        public VoipMsSmsService(HttpClient httpClient, string bearerToken)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _bearerToken);
        
        }

        public async Task<bool> SendSmsAsync(string did, string dst, string message)
        {
            // var url = $"{_apiBaseUrl}?did=3149366360&dst=6362935405&method=sendSMS&message={message}&api_username=paul.chavaux@gmail.com&api_password=uykMdVwGUT5L8n!";
            var url = $"{_apiBaseUrl}?did={did}&dst={dst}&method=sendSMS&message={message}&api_username=paul.chavaux@gmail.com&api_password=Runwiththewind%23!%23!1#!#!1";
            Debug.WriteLine("URL: " + url);
            var response = await _httpClient.GetAsync(url); // Use PostAsync for sendSMS
            if (response.IsSuccessStatusCode)
            {
               // var result = await response.Content.ReadFromJsonAsync<VoipMsSmsResponse>();
                Debug.WriteLine("Success! ");
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<IEnumerable<SmsMessage>> GetSmsMessagesAsync(string did)
        {
           // var url = $"{_apiBaseUrl}?method=getSMS&did={did}&api_username=paul.chavaux@gmail.com&api_password=uykMdVwGUT5L8n!&from=2024-11-06";
            var url = $"{_apiBaseUrl}?did={did}&method=getSMS&api_username=paul.chavaux@gmail.com&api_password=Runwiththewind%23!%23!1&from=2024-11-05#!#!1&from=2024-11-05";
            Debug.WriteLine("URL: " + url);
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<VoipMsSmsResponse>();
                return result?.Sms;
            }
            return null;
        }
    }
}