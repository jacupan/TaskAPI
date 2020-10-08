using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TaskAPI.Models;
using TaskAPI.Models.Persistent;

namespace TaskAPI.Controllers
{
    [Route("api/[controller]")]
    public class TaskListController : ControllerBase
    {
        private readonly TaskContext _context;

        public TaskListController(TaskContext context)
        {
            _context = context;
        }

        // GET: api/tasklist/8ab4fcbd993f49ce8a21103c713bf47a
        [HttpGet("{userId}")]
        public async Task<IEnumerable<TaskList>> GetAll(string userId)
        {
            return await _context.TaskLists.Where(p => p.UserId == userId && p.IsDeleted != true).ToListAsync();
        }

        // POST api/tasklist
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CreateTaskListRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var userExists = await _context.Users.AnyAsync(i => i.UserId == request.UserId);
            if (!userExists) return BadRequest();

            var itemExists = await _context.TaskLists.AnyAsync(i
                => i.Title == request.TaskListTitle && i.UserId == request.UserId);
            if (itemExists)
            {
                return BadRequest();
            }

            var item = new TaskList
            {
                TaskListId = Guid.NewGuid().ToString("N"),
                UserId = request.UserId,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                Title = request.TaskListTitle
            };
            _context.Add(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/tasklist/5ab4fcbd993f49ce8a21103c713bf47a
        [HttpDelete, ProducesResponseType(typeof(void), 204)]
        public async Task<IActionResult> Delete([FromBody] DeleteTaskListRequest request)
        {
            var item = await _context.TaskLists.FirstOrDefaultAsync(x
                => x.TaskListId == request.TaskListId && x.UserId == request.UserId && x.IsDeleted != true);
            if (item == null)
            {
                return NotFound();
            }

            item.IsDeleted = true;
            item.UpdatedOnUtc = DateTime.UtcNow;
            _context.Entry(item).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return StatusCode(204); // 201 No Content
        }
    }
}
