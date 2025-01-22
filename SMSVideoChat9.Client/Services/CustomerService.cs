 
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using SharedLibrary.Models;
using Thread = SharedLibrary.Models.Thread;

namespace SMSVideoChat9.Client.Services
    {
        public class CustomerService : ICustomerService
        {
            private readonly HttpClient _httpClient;

            public CustomerService(HttpClient httpClient)
            {
                _httpClient = httpClient;
            }

            public async Task<IEnumerable<Customer>> GetCustomerAsync()
            {
                return await _httpClient.GetFromJsonAsync<IEnumerable<Customer>>("api/Customers");
            }

            public async Task<Customer> GetCustomerAsync(int id)
            {
                return await _httpClient.GetFromJsonAsync<Customer>($"api/Customers/{id}");
            }

            public async Task CreateCustomerAsync(Customer customer)
            {
                await _httpClient.PostAsJsonAsync("api/Customers", customer);
            }

            public async Task UpdateCustomerAsync(int id, Customer customer)
            {
                await _httpClient.PutAsJsonAsync($"api/Customers/{id}", customer);
            }

            public async Task DeleteCustomerAsync(int id)
            {
                await _httpClient.DeleteAsync($"api/Customers/{id}");
            }
        public async Task<List<Channel>> GetChannelsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Channel>>("api/channels");
        }

        public async Task AddChannelAsync(Channel channel)
        {
            await _httpClient.PostAsJsonAsync("api/channels", channel);
        }

        public async Task UpdateChannelAsync(Channel channel)
        {
            await _httpClient.PutAsJsonAsync($"api/channels/{channel.Id}", channel);
        }

        public async Task<List<Thread>> GetThreadsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Thread>>("api/threads");
        }

        public async Task AddThreadAsync(Thread thread)
        {
            await _httpClient.PostAsJsonAsync("api/threads", thread);
        }

        public async Task UpdateThreadAsync(Thread thread)
        {
            await _httpClient.PutAsJsonAsync($"api/threads/{thread.Id}", thread);
        }
    }
    }
