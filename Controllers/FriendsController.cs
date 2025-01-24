
    using Microsoft.AspNetCore.Mvc;
    using SMSChat.Models;
    using Microsoft.EntityFrameworkCore;

namespace SMSChat.Controllers
{
    [ApiController]
        [Route("api/[controller]")]
        public class FriendsController : ControllerBase
        {
            private readonly DbContext _context;

            public FriendsController(DbContext context)
            {
                _context = context;
            }

            // GET: api/Friends
            [HttpGet]
            public async Task<ActionResult<IEnumerable<Friend>>> GetFriends()
            {
                return await _context.Set<Friend>().ToListAsync();
            }

            // GET: api/Friends/{id}
            [HttpGet("{id}")]
            public async Task<ActionResult<Friend>> GetFriend(int id)
            {
                var friend = await _context.Set<Friend>().FindAsync(id);

                if (friend == null)
                {
                    return NotFound();
                }

                return friend;
            }

            // POST: api/Friends
            [HttpPost]
            public async Task<ActionResult<Friend>> CreateFriend(Friend friend)
            {
                _context.Set<Friend>().Add(friend);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetFriend), new { id = friend.Id }, friend);
            }

            // PUT: api/Friends/{id}
            [HttpPut("{id}")]
            public async Task<IActionResult> UpdateFriend(int id, Friend friend)
            {
                if (id != friend.Id)
                {
                    return BadRequest();
                }

                _context.Entry(friend).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FriendExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return NoContent();
            }

            // DELETE: api/Friends/{id}
            [HttpDelete("{id}")]
            public async Task<IActionResult> DeleteFriend(int id)
            {
                var friend = await _context.Set<Friend>().FindAsync(id);
                if (friend == null)
                {
                    return NotFound();
                }

                _context.Set<Friend>().Remove(friend);
                await _context.SaveChangesAsync();

                return NoContent();
            }

            private bool FriendExists(int id)
            {
                return _context.Set<Friend>().Any(e => e.Id == id);
            }
        }
    }
