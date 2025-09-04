using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MySqlConnector;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LostPersonAPI.Models.Auth;
using System.Security.Cryptography;

namespace LostPersonAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _cs;
        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
            _cs = configuration.GetConnectionString("DefaultConnection")!;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            await using (var check = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Username=@u", conn))
            {
                check.Parameters.AddWithValue("@u", model.Username);
                if (Convert.ToInt32(await check.ExecuteScalarAsync()) > 0)
                    return Conflict(new { message = "User already exists" });
            }
            var hash = SimplePasswordHasher.Hash(model.Password);
            int userId;
            await using (var ins = new MySqlCommand("INSERT INTO Users(Username,Email,PasswordHash) VALUES(@u,@e,@p); SELECT LAST_INSERT_ID();", conn))
            {
                ins.Parameters.AddWithValue("@u", model.Username.Trim());
                ins.Parameters.AddWithValue("@e", model.Email ?? (object)DBNull.Value);
                ins.Parameters.AddWithValue("@p", hash);
                userId = Convert.ToInt32(await ins.ExecuteScalarAsync());
            }
            await using (var roleCmd = new MySqlCommand(@"INSERT IGNORE INTO UserRoles(UserId, RoleId)
SELECT @uid, r.Id FROM Roles r WHERE r.Name='User'", conn))
            {
                roleCmd.Parameters.AddWithValue("@uid", userId);
                await roleCmd.ExecuteNonQueryAsync();
            }
            return Ok(new { message = "User created" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var inputUsername = (model.Username ?? string.Empty).Trim();
            var inputPassword = model.Password ?? string.Empty;

            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            int userId = 0; string? storedHash = null; string? usernameExact = null;

            const string userSql = @"SELECT Id,PasswordHash,Username FROM Users 
WHERE Username=@u OR TRIM(Username)=@u OR LOWER(Username)=LOWER(@u) 
ORDER BY (Username=@u) DESC LIMIT 1";
            await using (var cmd = new MySqlCommand(userSql, conn))
            {
                cmd.Parameters.AddWithValue("@u", inputUsername);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    userId = r.GetInt32("Id");
                    storedHash = r.GetString("PasswordHash");
                    usernameExact = r.GetString("Username");
                }
            }
            if (storedHash == null) return Unauthorized();

            var peppered = SimplePasswordHasher.Hash(inputPassword);
            bool ok = string.Equals(peppered, storedHash, StringComparison.OrdinalIgnoreCase);

            if(!ok)
            {
                string legacyHash;
                using(var sha = SHA256.Create())
                {
                    legacyHash = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(inputPassword)));
                }
                if(string.Equals(legacyHash, storedHash, StringComparison.OrdinalIgnoreCase))
                {
                    await using var up = new MySqlCommand("UPDATE Users SET PasswordHash=@h, Username=TRIM(Username) WHERE Id=@id", conn);
                    up.Parameters.AddWithValue("@h", peppered);
                    up.Parameters.AddWithValue("@id", userId);
                    await up.ExecuteNonQueryAsync();
                    ok = true;
                }
            }
            if(!ok) return Unauthorized();

            var roles = new List<string>();
            await using (var roleCmd = new MySqlCommand(@"SELECT r.Name FROM Roles r JOIN UserRoles ur ON ur.RoleId=r.Id WHERE ur.UserId=@id", conn))
            {
                roleCmd.Parameters.AddWithValue("@id", userId);
                await using var r = await roleCmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) roles.Add(r.GetString(0));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, usernameExact ?? inputUsername)
            };
            foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.UtcNow.AddHours(3),
                claims: claims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                roles
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return Unauthorized();
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await using var conn = new MySqlConnection(_cs);
            await conn.OpenAsync();
            var roles = new List<string>();
            await using (var roleCmd = new MySqlCommand(@"SELECT r.Name FROM Roles r JOIN UserRoles ur ON ur.RoleId=r.Id WHERE ur.UserId=@id", conn))
            {
                roleCmd.Parameters.AddWithValue("@id", userId);
                await using var r = await roleCmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) roles.Add(r.GetString(0));
            }
            return Ok(new { username = User.Identity!.Name, roles });
        }
    }
}
