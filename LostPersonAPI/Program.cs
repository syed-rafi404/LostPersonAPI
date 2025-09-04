using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MySqlConnector;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Seed minimal auth tables (lowercase names to match existing DB) + default admin
async Task SeedAuthAsync()
{
    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    const string ddl = @"CREATE TABLE IF NOT EXISTS users (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Username VARCHAR(50) NOT NULL UNIQUE,
  Email VARCHAR(100),
  PasswordHash VARCHAR(255) NOT NULL,
  RegisteredAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);
CREATE TABLE IF NOT EXISTS roles (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Name VARCHAR(50) NOT NULL UNIQUE
);
CREATE TABLE IF NOT EXISTS userroles (
  UserId INT NOT NULL,
  RoleId INT NOT NULL,
  PRIMARY KEY(UserId,RoleId),
  FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE,
  FOREIGN KEY (RoleId) REFERENCES roles(Id) ON DELETE CASCADE
);
CREATE TABLE IF NOT EXISTS notifications (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  Type VARCHAR(40) NOT NULL,
  Message VARCHAR(500) NOT NULL,
  ReportID INT NULL,
  IsRead TINYINT(1) NOT NULL DEFAULT 0,
  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX IX_Notifications_UserId_IsRead (UserId, IsRead),
  FOREIGN KEY (UserId) REFERENCES users(Id) ON DELETE CASCADE
);";
    await using (var cmd = new MySqlCommand(ddl, conn))
        await cmd.ExecuteNonQueryAsync();

    async Task Exec(string sql, Action<MySqlCommand>? bind = null)
    {
        await using var c = new MySqlCommand(sql, conn);
        bind?.Invoke(c);
        await c.ExecuteNonQueryAsync();
    }

    await Exec("INSERT IGNORE INTO roles(Name) VALUES ('Admin'),('User')");

    var adminUser = (builder.Configuration["DefaultAdmin:Username"] ?? "rafitheflash").Trim();
    var adminEmail = builder.Configuration["DefaultAdmin:Email"] ?? "admin@example.com";
    var adminPass = builder.Configuration["DefaultAdmin:Password"] ?? "Rafi@123";

    int exists;
    await using (var check = new MySqlCommand("SELECT COUNT(*) FROM users WHERE Username=@u", conn))
    { check.Parameters.AddWithValue("@u", adminUser); exists = Convert.ToInt32(await check.ExecuteScalarAsync()); }

    var peppered = SimplePasswordHasher.Hash(adminPass);
    // legacy hash without pepper
    string legacy;
    using(var sha = SHA256.Create()) legacy = Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(adminPass)));

    if (exists == 0)
    {
        int uid;
        await using (var ins = new MySqlCommand("INSERT INTO users(Username,Email,PasswordHash) VALUES(@u,@e,@p); SELECT LAST_INSERT_ID();", conn))
        {
            ins.Parameters.AddWithValue("@u", adminUser);
            ins.Parameters.AddWithValue("@e", adminEmail);
            ins.Parameters.AddWithValue("@p", peppered); // always store peppered
            uid = Convert.ToInt32(await ins.ExecuteScalarAsync());
        }
        await Exec("INSERT IGNORE INTO userroles(UserId,RoleId) SELECT @id, Id FROM roles WHERE Name='Admin'", c => c.Parameters.AddWithValue("@id", uid));
    }
    else
    {
        // Upgrade legacy hash if needed
        await using (var sel = new MySqlCommand("SELECT Id, PasswordHash FROM users WHERE Username=@u", conn))
        {
            sel.Parameters.AddWithValue("@u", adminUser);
            await using var r = await sel.ExecuteReaderAsync();
            if(await r.ReadAsync())
            {
                int id = r.GetInt32(0); var stored = r.GetString(1);
                if(string.Equals(stored, legacy, StringComparison.OrdinalIgnoreCase))
                {
                    r.Close();
                    await using var up = new MySqlCommand("UPDATE users SET PasswordHash=@p WHERE Id=@i", conn);
                    up.Parameters.AddWithValue("@p", peppered);
                    up.Parameters.AddWithValue("@i", id);
                    await up.ExecuteNonQueryAsync();
                }
            }
        }
        await Exec("INSERT IGNORE INTO userroles(UserId,RoleId) SELECT u.Id, r.Id FROM users u JOIN roles r ON r.Name='Admin' WHERE u.Username=@u", c => c.Parameters.AddWithValue("@u", adminUser));
    }
}
await SeedAuthAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static class SimplePasswordHasher
{
    private const string Pepper = "__DEMO_STATIC_PEPPER__"; // demo only
    public static string Hash(string password)
    {
        password = password?.TrimEnd('\r','\n') ?? string.Empty;
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(password + Pepper)));
    }
    public static bool Verify(string password, string hash) => Hash(password).Equals(hash, StringComparison.OrdinalIgnoreCase);
}
