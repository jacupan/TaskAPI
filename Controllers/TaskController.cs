using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TaskAPI.Models;
using TaskAPI.Models.Persistent;
using Task = TaskAPI.Models.Persistent.Task;

namespace TaskAPI.Controllers
{
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly TaskContext _context;

        public TaskController(TaskContext context)
        {
            _context = context;
        }

        // GET: api/task/2ab4fcbd993f49ce8a21103c713bf47a
        [HttpGet("{taskListId}")]
        public async Task<ActionResult<IEnumerable<Task>>> GetAll(string taskListId)
        {
            return Ok(await _context.Tasks.Where(p => p.TaskListId == taskListId && p.IsDeleted != true).ToListAsync());
        }

        // POST api/task
        [HttpPost]
        public async Task<ActionResult<object>> Post([FromBody] CreateTaskRequest request)
        {
            if (!ModelState.IsValid) return BadRequest();

            var itemExists = await _context.Tasks.AnyAsync(i
                => i.Title == request.TaskTitle && i.TaskListId == request.TaskListId && i.IsDeleted != true);

            if (itemExists) return BadRequest();

            var item = new Task
            {
                TaskListId = request.TaskListId,
                TaskId = Guid.NewGuid().ToString("N"),
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                Title = request.TaskTitle
            };
            _context.Add(item);
            await _context.SaveChangesAsync();

            var tasks = await _context.Tasks.Where(i => i.TaskListId == request.TaskListId && i.IsDeleted != true)
                .Select(p => new {Title = p.Title})
                .ToListAsync();
            var getTaskList = await _context.TaskLists.Where(i => i.TaskListId == request.TaskListId)
                .SingleOrDefaultAsync();

            var user = getTaskList != null
                ? await _context.Users.Where(u => u.UserId == getTaskList.UserId).SingleOrDefaultAsync()
                : null;

            //TODO: Response should not be anonymous objects, this disallows easy documentation.
            return Ok(new {User = user?.EmailAddress, Tasks = tasks, TaskList = getTaskList?.Title});
        }

        // PUT api/task
        [HttpPut, ProducesResponseType(typeof(void), 204)]
        public async Task<IActionResult> Put([FromBody] UpdateTaskRequest request)
        {
            if (!ModelState.IsValid) return BadRequest();

            var itemExists = await _context.Tasks.SingleOrDefaultAsync(i
                => i.TaskId == request.TaskId && i.TaskListId == request.TaskListId && i.IsDeleted != true);
            if (itemExists == null) return BadRequest(new {Message = "Record not found. Make sure it exists"});
            // parse the updated properties
            foreach (var item in request.Data)
            {
                switch (item.Key)
                {
                    case TaskPropertyEnum.IsCompleted:
                        itemExists.IsCompleted = bool.Parse(item.Value);
                        break;
                    case TaskPropertyEnum.CompletedOn:
                        itemExists.CompletedOnUtc = DateTime.Parse(item.Value);
                        break;
                    case TaskPropertyEnum.DueOn:
                        itemExists.DueOnUtc = DateTime.Parse(item.Value);
                        break;
                    case TaskPropertyEnum.IsActive:
                        itemExists.IsActive = bool.Parse(item.Value);
                        break;
                    case TaskPropertyEnum.Title:
                        itemExists.Title = item.Value;
                        break;
                    default:
                        break;
                }
            }

            _context.Entry(itemExists).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/task/1ab4fcbd993f49ce8a21103c713bf47a
        [HttpDelete, ProducesResponseType(typeof(void), 204)]
        public async Task<IActionResult> Delete([FromBody] DeleteTaskRequest request)
        {
            var item = await _context.Tasks.FirstOrDefaultAsync(x
                => x.TaskId == request.TaskId && x.TaskListId == request.TaskListId && x.IsDeleted != true);
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
