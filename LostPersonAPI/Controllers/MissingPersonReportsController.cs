using LostPersonAPI.Data;
using LostPersonAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System; // Required for Math.Ceiling
using System.Linq;
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

        // GET: api/MissingPersonReports
        // This endpoint retrieves reports with filtering and pagination.
        [HttpGet]
        public async Task<ActionResult> GetReports(
            [FromQuery] string? name,
            [FromQuery] string? status,
            [FromQuery] int? minAge,
            [FromQuery] int? maxAge,
            [FromQuery] string? gender,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 8)
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

        // POST: api/MissingPersonReports
        // This endpoint creates a new report.
        [HttpPost]
        public async Task<ActionResult<MissingPersonReport>> PostMissingPersonReport(MissingPersonReportDto reportDto)
        {
            var newReport = new MissingPersonReport
            {
                Name = reportDto.Name,
                Age = reportDto.Age,
                Gender = reportDto.Gender,
                Status = "Active", // Always set new reports to Active
                LastSeenDate = reportDto.LastSeenDate,
                LastSeenLatitude = reportDto.LastSeenLatitude,
                LastSeenLongitude = reportDto.LastSeenLongitude,
                PhotoUrl = reportDto.PhotoUrl,
                Height = reportDto.Height,
                Weight = reportDto.Weight,
                SkinColor = reportDto.SkinColor,
                Clothing = reportDto.Clothing,
                MedicalCondition = reportDto.MedicalCondition
            };

            _context.MissingPersonReports.Add(newReport);
            await _context.SaveChangesAsync();

            // Return a 201 Created response with a location header pointing to the new resource.
            return CreatedAtAction(nameof(GetReportById), new { id = newReport.ReportID }, newReport);
        }

        // GET: api/MissingPersonReports/5
        // This endpoint retrieves a specific report by its ID.
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
