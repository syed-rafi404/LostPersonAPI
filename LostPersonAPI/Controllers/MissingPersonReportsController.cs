using LostPersonAPI.Data;
using LostPersonAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Add this using directive
using System.Collections.Generic;   // Add this using directive
using System.Linq;
using MySqlConnector;
using System.Threading.Tasks;

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

        // POST: api/MissingPersonReports
        // This endpoint is for creating a new report.
        [HttpPost]
        // GET: api/MissingPersonReports
        [HttpGet]
        public async Task<ActionResult> GetReports(
    [FromQuery] string? name,
    [FromQuery] string? status,
    [FromQuery] int? minAge,
    [FromQuery] int? maxAge,
    [FromQuery] string? gender,
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 8) // Set a default page size
        {
            var query = _context.MissingPersonReports.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(name)) query = query.Where(r => r.Name.Contains(name));
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            if (minAge.HasValue) query = query.Where(r => r.Age >= minAge.Value);
            if (maxAge.HasValue) query = query.Where(r => r.Age <= maxAge.Value);
            if (!string.IsNullOrEmpty(gender)) query = query.Where(r => r.Gender == gender);

            // Get the total count of records that match the filter, before paging
            var totalRecords = await query.CountAsync();

            // Apply pagination
            var reports = await query
                .OrderByDescending(r => r.ReportID)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create a response object that includes both the data and pagination metadata
            var response = new
            {
                TotalRecords = totalRecords,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Reports = reports
            };

            return Ok(response);
        }






        // GET: api/MissingPersonReports/5
        // This endpoint is used by CreateReport to generate the location header.
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
