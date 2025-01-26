using SMSChat.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SMSChat.Data; // Assumes you are using Entity Framework Core

namespace SMSChat.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _dbContext;

        public CustomerService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Customer>> GetCustomerAsync()
        {
            return await _dbContext.Customers.ToListAsync();
        }

        public async Task<Customer> GetCustomerAsync(int id)
        {
            return await _dbContext.Customers.FindAsync(id);
        }

        public async Task CreateCustomerAsync(Customer customer)
        {
            await _dbContext.Customers.AddAsync(customer);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateCustomerAsync(int id, Customer customer)
        {
            var existingCustomer = await _dbContext.Customers.FindAsync(id);
            if (existingCustomer != null)
            {
                existingCustomer.Name = customer.Name; // Update other fields as needed
                existingCustomer.Email = customer.Email;

                _dbContext.Customers.Update(existingCustomer);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var customer = await _dbContext.Customers.FindAsync(id);
            if (customer != null)
            {
                _dbContext.Customers.Remove(customer);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<Channel>> GetChannelsAsync()
        {
            return await _dbContext.Channels.ToListAsync();
        }

        public async Task AddChannelAsync(Channel channel)
        {
            await _dbContext.Channels.AddAsync(channel);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateChannelAsync(Channel channel)
        {
            var existingChannel = await _dbContext.Channels.FindAsync(channel.Id);
            if (existingChannel != null)
            {
                existingChannel.Name = channel.Name; // Update other fields as needed

                _dbContext.Channels.Update(existingChannel);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<List<SMSChat.Models.Thread>> GetThreadsAsync()
        {
            return await _dbContext.Threads.ToListAsync();
        }

        public async Task AddThreadAsync(SMSChat.Models.Thread thread)
        {
            await _dbContext.Threads.AddAsync(thread);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateThreadAsync(SMSChat.Models.Thread thread)
        {
            var existingThread = await _dbContext.Threads.FindAsync(thread.Id);
            if (existingThread != null)
            {
                existingThread.Title = thread.Title; // Update other fields as needed

                _dbContext.Threads.Update(existingThread);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
