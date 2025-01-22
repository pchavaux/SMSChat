using SharedLibrary.Models;
using SMSVideoChat9.Client.Services;
using Thread = SharedLibrary.Models.Thread;

namespace SMSVideoChat9.Client.Services
{
    public interface ICustomerService
    {
            Task<IEnumerable<Customer>> GetCustomerAsync();
            Task<Customer> GetCustomerAsync(int id);
            Task CreateCustomerAsync(Customer friend);
            Task UpdateCustomerAsync(int id, Customer friend);
            Task DeleteCustomerAsync(int id);
        Task<List<Channel>> GetChannelsAsync();
        Task AddChannelAsync(Channel channel);
        Task UpdateChannelAsync(Channel channel);
        Task<List<Thread>> GetThreadsAsync();
        Task AddThreadAsync(Thread thread);
        Task UpdateThreadAsync(Thread thread);
    }
 }

