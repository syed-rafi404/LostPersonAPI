using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace LostPersonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly string _cs;
        public AdminController(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DefaultConnection")!;
        }

        // POST api/admin/promote/{username}
        [HttpPost("promote/{username}")]
        public async Task<IActionResult> Promote(string username)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            int userId = 0;
            await using (var getUser = new MySqlCommand("SELECT Id FROM Users WHERE Username=@u", conn))
            {
                getUser.Parameters.AddWithValue("@u", username);
                var o = await getUser.ExecuteScalarAsync();
                if (o == null) return NotFound();
                userId = Convert.ToInt32(o);
            }
            // Ensure Admin role id
            int roleId = 0;
            await using (var getRole = new MySqlCommand("SELECT Id FROM Roles WHERE Name='Admin'", conn))
            {
                roleId = Convert.ToInt32(await getRole.ExecuteScalarAsync());
            }
            await using (var ins = new MySqlCommand("INSERT IGNORE INTO UserRoles(UserId,RoleId) VALUES(@u,@r)", conn))
            {
                ins.Parameters.AddWithValue("@u", userId);
                ins.Parameters.AddWithValue("@r", roleId);
                await ins.ExecuteNonQueryAsync();
            }
            return Ok(new { message = "Promoted" });
        }
    }
}
