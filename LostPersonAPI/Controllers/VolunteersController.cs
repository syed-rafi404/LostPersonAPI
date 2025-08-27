using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LostPersonAPI.Data;
using LostPersonAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LostPersonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VolunteersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VolunteersController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Volunteer>>> GetVolunteers()
        {
            var volunteers = await _context.Volunteers.ToListAsync();

            return Ok(volunteers);
        }
    }
}
