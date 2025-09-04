using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;

namespace LostPersonAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly string _cs;
        public NotificationsController(IConfiguration configuration){ _cs = configuration.GetConnectionString("DefaultConnection")!; }
        private async Task<MySqlConnection> Open(){ var c=new MySqlConnection(_cs); await c.OpenAsync(); return c; }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] bool unreadOnly=false, [FromQuery] int limit=50)
        {
            if(limit<1|| limit>200) limit=50;
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await using var conn = await Open();
            string sql = unreadOnly?"SELECT Id,Type,Message,ReportID,IsRead,CreatedAt FROM Notifications WHERE UserId=@u AND IsRead=0 ORDER BY Id DESC LIMIT @lim" : "SELECT Id,Type,Message,ReportID,IsRead,CreatedAt FROM Notifications WHERE UserId=@u ORDER BY Id DESC LIMIT @lim";
            var list = new List<object>();
            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@u", userId);
                cmd.Parameters.AddWithValue("@lim", limit);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    int? repId = r.IsDBNull(r.GetOrdinal("ReportID")) ? (int?)null : r.GetInt32("ReportID");
                    list.Add(new { id=r.GetInt32("Id"), type=r.GetString("Type"), message=r.GetString("Message"), reportId = repId, isRead = r.GetBoolean("IsRead"), createdAt = r.GetDateTime("CreatedAt") });
                }
            }
            return Ok(list);
        }

        public class MarkDto{ public int[] ids {get;set;} = Array.Empty<int>(); }
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] MarkDto dto)
        {
            if(dto.ids.Length==0) return Ok();
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await using var conn = await Open();
            var idParams = string.Join(',', dto.ids.Select((_,i)=>"@p"+i));
            var sql = $"UPDATE Notifications SET IsRead=1 WHERE UserId=@u AND Id IN ({idParams})";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", userId);
            for(int i=0;i<dto.ids.Length;i++) cmd.Parameters.AddWithValue("@p"+i, dto.ids[i]);
            await cmd.ExecuteNonQueryAsync();
            return Ok();
        }

        [HttpGet("admin/summary")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> AdminSummary()
        {
            await using var conn = await Open();
            int pendingReports;
            await using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM MissingPersonReports WHERE Status='Pending'", conn))
                pendingReports = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            int unread;
            await using (var cmd2 = new MySqlCommand("SELECT COUNT(*) FROM Notifications WHERE UserId=@u AND IsRead=0", conn))
            { cmd2.Parameters.AddWithValue("@u", userId); unread = Convert.ToInt32(await cmd2.ExecuteScalarAsync()); }
            return Ok(new { pendingReports, unread });
        }
    }
}
