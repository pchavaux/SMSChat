using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SMSChat.Models;
using SMSChat.Data;

namespace SMSChat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChannelsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChannelsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Channel>>> GetChannels()
        {
            return await _context.Channels.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<Channel>> CreateChannel(Channel channel)
        {
            _context.Channels.Add(channel);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetChannel), new { id = channel.Id }, channel);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Channel>> GetChannel(int id)
        {
            var channel = await _context.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }
            return channel;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateChannel(int id, Channel channel)
        {
            if (id != channel.Id)
            {
                return BadRequest();
            }

            _context.Entry(channel).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChannel(int id)
        {
            var channel = await _context.Channels.FindAsync(id);
            if (channel == null)
            {
                return NotFound();
            }

            _context.Channels.Remove(channel);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}