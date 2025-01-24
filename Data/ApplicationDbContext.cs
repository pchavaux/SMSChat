using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SMSChat.Models;
using Thread = SMSChat.Models.Thread;

namespace SMSChat.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Other DbSets
        public DbSet<Message> Messages { get; set; }
        public DbSet<SystemCredentials> SystemCredentials { get; set; }
        public DbSet<Friend> Friends { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<ChatThread> ChatThreads { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<Channel> Channels { get; set; }
        public DbSet<Thread> Threads { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: Fluent API configurations for relationships

            // Configure Friend's relationship with ApplicationUser
            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User)
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Message's relationship with ChatThread (if applicable)
            modelBuilder.Entity<Message>()
                .HasOne<ChatThread>()
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.ChatThreadId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Attachment's relationship with Message
            modelBuilder.Entity<Attachment>()
                .HasOne<Message>()
                .WithMany()
                .HasForeignKey(a => a.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
