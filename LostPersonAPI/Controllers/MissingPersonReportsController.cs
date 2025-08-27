using Microsoft.AspNetCore.Mvc;
using LostPersonAPI.Data;
using LostPersonAPI.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
namespace LostPersonAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MissingPersonReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MissingPersonReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]

        [HttpPost]
        public async Task<ActionResult<MissingPersonReport>> CreateReport(MissingPersonReport report)
        {
            report.ReportingDate = DateTime.UtcNow;

        
            if (string.IsNullOrEmpty(report.Status))
            {
                report.Status = "Active";
            }

            _context.MissingPersonReports.Add(report);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReportById", new { id = report.ReportID }, report);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MissingPersonReport>> GetReportById(int id)
        {
            var report = await _context.MissingPersonReports.FindAsync(id);

            if (report == null)
            {
                return NotFound(); 
            }

            return Ok(report); 
        }
    }
}
