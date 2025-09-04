using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;

namespace LostPersonAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TimelineController : ControllerBase
    {
        private readonly string _cs;
        public TimelineController(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DefaultConnection")!;
        }

        private async Task<MySqlConnection> OpenAsync()
        {
            var c = new MySqlConnection(_cs);
            await c.OpenAsync();
            return c;
        }

        // GET api/timeline/{reportId}?afterId=0&limit=100
        [HttpGet("{reportId}")]
        public async Task<IActionResult> GetTimeline(int reportId, [FromQuery] int afterId = 0, [FromQuery] int limit = 100)
        {
            if (limit < 1 || limit > 500) limit = 100;
            await using var conn = await OpenAsync();
            // Ensure report exists and user can view
            string status = "";
            await using (var chk = new MySqlCommand("SELECT Status FROM MissingPersonReports WHERE ReportID=@id", conn))
            {
                chk.Parameters.AddWithValue("@id", reportId);
                var obj = await chk.ExecuteScalarAsync();
                if (obj == null) return NotFound(new { message = "Report not found" });
                status = obj?.ToString() ?? "";
                if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) && !User.IsInRole("Admin"))
                    return Forbid();
            }

            var list = new List<object>();
            string sql = afterId > 0
                ? "SELECT Id,Username,Message,CreatedAt FROM ReportTimeline WHERE ReportID=@rid AND Id>@after ORDER BY Id ASC LIMIT @lim"
                : "SELECT Id,Username,Message,CreatedAt FROM ReportTimeline WHERE ReportID=@rid ORDER BY Id ASC LIMIT @lim";

            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@rid", reportId);
                cmd.Parameters.AddWithValue("@lim", limit);
                if (afterId > 0) cmd.Parameters.AddWithValue("@after", afterId);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    list.Add(new
                    {
                        id = r.GetInt32("Id"),
                        username = r.GetString("Username"),
                        message = r.GetString("Message"),
                        createdAt = r.GetDateTime("CreatedAt")
                    });
                }
            }
            return Ok(new { reportId, items = list });
        }

        public class TimelinePostDto { public int ReportId { get; set; } public string Message { get; set; } = string.Empty; }

        // POST api/timeline
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] TimelinePostDto dto)
        {
            if (dto.ReportId <= 0 || string.IsNullOrWhiteSpace(dto.Message)) return BadRequest(new { message = "ReportId and Message required" });
            var trimmed = dto.Message.Trim();
            if (trimmed.Length > 2000) trimmed = trimmed.Substring(0, 2000);
            await using var conn = await OpenAsync();
            // Validate report & visibility
            bool isAdmin = User.IsInRole("Admin");
            string? status = null;
            await using (var chk = new MySqlCommand("SELECT Status FROM MissingPersonReports WHERE ReportID=@id", conn))
            {
                chk.Parameters.AddWithValue("@id", dto.ReportId);
                var obj = await chk.ExecuteScalarAsync();
                if (obj == null) return NotFound(new { message = "Report not found" });
                status = obj.ToString();
                if (string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) && !isAdmin)
                    return Forbid();
            }

            // Ensure table exists (first use convenience)
            const string createSql = @"CREATE TABLE IF NOT EXISTS ReportTimeline (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  ReportID INT NOT NULL,
  Username VARCHAR(50) NOT NULL,
  Message TEXT NOT NULL,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX IX_ReportTimeline_ReportID (ReportID),
  CONSTRAINT FK_ReportTimeline_Report FOREIGN KEY (ReportID) REFERENCES MissingPersonReports(ReportID) ON DELETE CASCADE
)";
            await using (var create = new MySqlCommand(createSql, conn)) { await create.ExecuteNonQueryAsync(); }

            string username = User.Identity?.Name ?? "user";
            int newId;
            await using (var ins = new MySqlCommand("INSERT INTO ReportTimeline(ReportID,Username,Message) VALUES(@r,@u,@m); SELECT LAST_INSERT_ID();", conn))
            {
                ins.Parameters.AddWithValue("@r", dto.ReportId);
                ins.Parameters.AddWithValue("@u", username);
                ins.Parameters.AddWithValue("@m", trimmed);
                newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            }
            return Ok(new { id = newId, dto.ReportId, username, message = trimmed, createdAt = DateTime.UtcNow });
        }
    }
}
