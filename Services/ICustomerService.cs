 
using SMSChat.Models;
using Thread = SMSChat.Models.Thread;

namespace SMSChat.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<Customer>> GetCustomerAsync();
        Task<Customer> GetCustomerAsync(int id);
        Task CreateCustomerAsync(Customer customer);
        Task UpdateCustomerAsync(int id, Customer customer);
        Task DeleteCustomerAsync(int id);

        Task<List<Channel>> GetChannelsAsync();
        Task AddChannelAsync(Channel channel);
        Task UpdateChannelAsync(Channel channel);

        Task<List<Thread>> GetThreadsAsync();
        Task AddThreadAsync(Thread thread);
        Task UpdateThreadAsync(Thread thread);
    }
 }

