using LostPersonAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Security.Claims;

namespace LostPersonAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")] 
    [ApiController]
    public class MissingPersonReportsController : ControllerBase
    {
        private readonly string _cs;
        public MissingPersonReportsController(IConfiguration configuration)
        {
            _cs = configuration.GetConnectionString("DefaultConnection")!;
        }

        private async Task<MySqlConnection> OpenAsync(){ var conn=new MySqlConnection(_cs); await conn.OpenAsync(); return conn; }
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string CurrentUsername => User.Identity?.Name ?? "";

        private async Task<int[]> GetAdminUserIds(MySqlConnection conn)
        {
            var ids = new List<int>();
            await using var cmd = new MySqlCommand(@"SELECT u.Id FROM Users u JOIN UserRoles ur ON ur.UserId=u.Id JOIN Roles r ON r.Id=ur.RoleId WHERE r.Name='Admin'", conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync()) ids.Add(r.GetInt32(0));
            return ids.ToArray();
        }
        private async Task AddNotification(MySqlConnection conn, int userId, string type, string message, int? reportId=null)
        {
            await using var cmd = new MySqlCommand("INSERT INTO Notifications(UserId,Type,Message,ReportID) VALUES(@u,@t,@m,@r)", conn);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@t", type);
            cmd.Parameters.AddWithValue("@m", message);
            cmd.Parameters.AddWithValue("@r", (object?)reportId ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        private static MissingPersonReport MapReport(MySqlDataReader r) => new()
        {
            ReportID = r.GetInt32("ReportID"),
            ReportedBy = r.ColumnExists("ReportedBy") && !r.IsDBNull(r.GetOrdinal("ReportedBy")) ? r.GetInt32("ReportedBy") : 0,
            ApprovedBy = r.ColumnExists("ApprovedBy") && !r.IsDBNull(r.GetOrdinal("ApprovedBy")) ? r.GetInt32("ApprovedBy") : null,
            ApprovedAt = r.ColumnExists("ApprovedAt") && !r.IsDBNull(r.GetOrdinal("ApprovedAt")) ? r.GetDateTime("ApprovedAt") : null,
            Name = r.GetString("Name"),
            Age = r.GetInt32("Age"),
            Gender = r.ColumnExists("Gender") && !r.IsDBNull(r.GetOrdinal("Gender")) ? r.GetString("Gender") : null,
            Height = r.ColumnExists("Height") && !r.IsDBNull(r.GetOrdinal("Height")) ? r.GetDouble("Height") : null,
            Weight = r.ColumnExists("Weight") && !r.IsDBNull(r.GetOrdinal("Weight")) ? r.GetDouble("Weight") : null,
            SkinColor = r.ColumnExists("SkinColor") && !r.IsDBNull(r.GetOrdinal("SkinColor")) ? r.GetString("SkinColor") : null,
            Clothing = r.ColumnExists("Clothing") && !r.IsDBNull(r.GetOrdinal("Clothing")) ? r.GetString("Clothing") : null,
            MedicalCondition = r.ColumnExists("MedicalCondition") && !r.IsDBNull(r.GetOrdinal("MedicalCondition")) ? r.GetString("MedicalCondition") : null,
            LastSeenDate = r.GetDateTime("LastSeenDate"),
            LastSeenLatitude = r.ColumnExists("LastSeenLatitude") && !r.IsDBNull(r.GetOrdinal("LastSeenLatitude")) ? r.GetDouble("LastSeenLatitude") : null,
            LastSeenLongitude = r.ColumnExists("LastSeenLongitude") && !r.IsDBNull(r.GetOrdinal("LastSeenLongitude")) ? r.GetDouble("LastSeenLongitude") : null,
            Status = r.ColumnExists("Status") && !r.IsDBNull(r.GetOrdinal("Status")) ? r.GetString("Status") : null,
            ReportingDate = r.GetDateTime("ReportingDate"),
            PhotoUrl = r.SafeGetString("PhotoUrl"),
            EyeColor = r.SafeGetString("EyeColor"),
            HairColor = r.SafeGetString("HairColor"),
            HasGlasses = r.ColumnExists("HasGlasses") && !r.IsDBNull(r.GetOrdinal("HasGlasses")) && r.GetBoolean("HasGlasses"),
            ScarsMarks = r.SafeGetString("ScarsMarks"),
            UniqueCharacteristics = r.SafeGetString("UniqueCharacteristics")
        };

        private static async Task<bool> ColumnExistsAsync(MySqlConnection conn, string table, string column)
        {
            await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME=@t AND COLUMN_NAME=@c", conn);
            cmd.Parameters.AddWithValue("@t", table);
            cmd.Parameters.AddWithValue("@c", column);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return count > 0;
        }

        // GET: api/MissingPersonReports  (Raw SQL with optional filters + pagination)
        [HttpGet]
        public async Task<IActionResult> GetReports(
            [FromQuery] string? name,
            [FromQuery] string? status,
            [FromQuery] int? minAge,
            [FromQuery] int? maxAge,
            [FromQuery] string? gender,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 8)
        {
            bool isAdmin = User.IsInRole("Admin");
            var where = new StringBuilder();
            where.Append(isAdmin ? " WHERE 1=1 " : " WHERE Status <> 'Pending' ");
            var parameters = new List<MySqlParameter>();
            void AddCond(string sql, MySqlParameter p){ where.Append(sql); parameters.Add(p); }
            if (!string.IsNullOrWhiteSpace(name)) AddCond(" AND Name LIKE @Name", new("@Name", $"%{name}%"));
            if (!string.IsNullOrWhiteSpace(status)) AddCond(" AND Status = @Status", new("@Status", status));
            if (minAge.HasValue) AddCond(" AND Age >= @MinAge", new("@MinAge", minAge.Value));
            if (maxAge.HasValue) AddCond(" AND Age <= @MaxAge", new("@MaxAge", maxAge.Value));
            if (!string.IsNullOrWhiteSpace(gender)) AddCond(" AND Gender = @Gender", new("@Gender", gender));

            int offset = (pageNumber - 1) * pageSize;

            string sqlData = $"SELECT * FROM MissingPersonReports {where} ORDER BY ReportID DESC LIMIT @Offset,@Take";
            string sqlCount = $"SELECT COUNT(*) FROM MissingPersonReports {where}";
            parameters.Add(new("@Offset", offset));
            parameters.Add(new("@Take", pageSize));

            var list = new List<MissingPersonReport>();
            int total;
            await using (var conn = await OpenAsync())
            {
                await using (var countCmd = new MySqlCommand(sqlCount, conn))
                {
                    countCmd.Parameters.AddRange(parameters.Where(p=>p.ParameterName!="@Offset" && p.ParameterName!="@Take").ToArray());
                    total = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }
                await using (var cmd = new MySqlCommand(sqlData, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync()) list.Add(MapReport((MySqlDataReader)r));
                }
            }

            return Ok(new { totalRecords = total, pageSize, currentPage = pageNumber, totalPages = (int)Math.Ceiling(total / (double)pageSize), reports = list });
        }

        // GET single
        [HttpGet("{id}")]
        public async Task<IActionResult> GetReportById(int id)
        {
            bool isAdmin = User.IsInRole("Admin");
            const string sql = "SELECT * FROM MissingPersonReports WHERE ReportID=@Id";
            MissingPersonReport? report = null;
            await using (var conn = await OpenAsync())
            await using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Id", id);
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync()) report = MapReport((MySqlDataReader)r);
            }
            if (report == null) return NotFound();
            if (!isAdmin && string.Equals(report.Status, "Pending", StringComparison.OrdinalIgnoreCase)) return NotFound();
            return Ok(report);
        }

        // INSERT only columns that definitely exist in current schema to avoid unknown column errors
        [HttpPost]
        public async Task<IActionResult> PostMissingPersonReport(MissingPersonReportDto dto)
        {
            var lat = double.IsNaN(dto.LastSeenLatitude) ? (object)DBNull.Value : dto.LastSeenLatitude;
            var lon = double.IsNaN(dto.LastSeenLongitude) ? (object)DBNull.Value : dto.LastSeenLongitude;
            try
            {
                await using var conn = await OpenAsync();
                // Detect optional columns
                bool photoColumn = await ColumnExistsAsync(conn, "MissingPersonReports", "PhotoUrl");
                bool reportedByColumn = await ColumnExistsAsync(conn, "MissingPersonReports", "ReportedBy");
                bool eyeCol = await ColumnExistsAsync(conn, "MissingPersonReports", "EyeColor");
                bool hairCol = await ColumnExistsAsync(conn, "MissingPersonReports", "HairColor");
                bool glassesCol = await ColumnExistsAsync(conn, "MissingPersonReports", "HasGlasses");
                bool scarsCol = await ColumnExistsAsync(conn, "MissingPersonReports", "ScarsMarks");
                bool uniqCol = await ColumnExistsAsync(conn, "MissingPersonReports", "UniqueCharacteristics");

                var cols = new StringBuilder("Name,Age,Gender,Height,Weight,SkinColor,Clothing,MedicalCondition,LastSeenDate,LastSeenLatitude,LastSeenLongitude,Status,ReportingDate");
                var vals = new StringBuilder("@Name,@Age,@Gender,@Height,@Weight,@SkinColor,@Clothing,@MedicalCondition,@LastSeenDate,@LastSeenLatitude,@LastSeenLongitude,'Pending',UTC_TIMESTAMP()");
                if (photoColumn){ cols.Append(",PhotoUrl"); vals.Append(",@PhotoUrl"); }
                if (reportedByColumn){ cols.Append(",ReportedBy"); vals.Append(",@ReportedBy"); }
                if (eyeCol){ cols.Append(",EyeColor"); vals.Append(",@EyeColor"); }
                if (hairCol){ cols.Append(",HairColor"); vals.Append(",@HairColor"); }
                if (glassesCol){ cols.Append(",HasGlasses"); vals.Append(",@HasGlasses"); }
                if (scarsCol){ cols.Append(",ScarsMarks"); vals.Append(",@ScarsMarks"); }
                if (uniqCol){ cols.Append(",UniqueCharacteristics"); vals.Append(",@UniqueCharacteristics"); }

                string insertSql = $"INSERT INTO MissingPersonReports ({cols}) VALUES ({vals});";

                int affected;
                await using (var ins = new MySqlCommand(insertSql, conn))
                {
                    ins.Parameters.AddWithValue("@Name", (object?)dto.Name ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Age", dto.Age);
                    ins.Parameters.AddWithValue("@Gender", (object?)dto.Gender ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Height", dto.Height);
                    ins.Parameters.AddWithValue("@Weight", dto.Weight);
                    ins.Parameters.AddWithValue("@SkinColor", (object?)dto.SkinColor ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@Clothing", (object?)dto.Clothing ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@MedicalCondition", (object?)dto.MedicalCondition ?? DBNull.Value);
                    ins.Parameters.AddWithValue("@LastSeenDate", dto.LastSeenDate == default ? DateTime.UtcNow : dto.LastSeenDate);
                    ins.Parameters.AddWithValue("@LastSeenLatitude", lat);
                    ins.Parameters.AddWithValue("@LastSeenLongitude", lon);
                    if (photoColumn) ins.Parameters.AddWithValue("@PhotoUrl", (object?)dto.PhotoUrl ?? DBNull.Value);
                    if (reportedByColumn) ins.Parameters.AddWithValue("@ReportedBy", CurrentUserId);
                    if (eyeCol) ins.Parameters.AddWithValue("@EyeColor", (object?)dto.EyeColor ?? DBNull.Value);
                    if (hairCol) ins.Parameters.AddWithValue("@HairColor", (object?)dto.HairColor ?? DBNull.Value);
                    if (glassesCol) ins.Parameters.AddWithValue("@HasGlasses", dto.HasGlasses ? 1 : 0);
                    if (scarsCol) ins.Parameters.AddWithValue("@ScarsMarks", (object?)dto.ScarsMarks ?? DBNull.Value);
                    if (uniqCol) ins.Parameters.AddWithValue("@UniqueCharacteristics", (object?)dto.UniqueCharacteristics ?? DBNull.Value);
                    affected = await ins.ExecuteNonQueryAsync();
                }
                if (affected == 0) return StatusCode(500, new { message = "Insert failed (no rows)" });
                long newId;
                await using (var idCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", conn))
                { newId = Convert.ToInt64(await idCmd.ExecuteScalarAsync()); }

                // Notify admins of new pending case
                var adminIds = await GetAdminUserIds(conn);
                foreach(var aid in adminIds){ if(aid!=CurrentUserId) await AddNotification(conn, aid, "NewReport", $"New report submitted: {dto.Name}", (int)newId); }

                MissingPersonReport? report = null;
                await using (var sel = new MySqlCommand("SELECT * FROM MissingPersonReports WHERE ReportID=@id", conn))
                {
                    sel.Parameters.AddWithValue("@id", newId);
                    await using var r = await sel.ExecuteReaderAsync();
                    if (await r.ReadAsync()) report = MapReport((MySqlDataReader)r);
                }
                if (report == null)
                {
                    report = new MissingPersonReport { ReportID = (int)newId, Name = dto.Name, Age = dto.Age, Status = "Pending", PhotoUrl = dto.PhotoUrl };
                }
                return Created($"/api/MissingPersonReports/{report.ReportID}", report);
            }
            catch (MySqlException ex)
            {
                return StatusCode(500, new { message = "Insert failed (MySQL)", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Insert failed (Server)", detail = ex.Message });
            }
        }

        // Approve
        [HttpPost("{id}/approve")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> ApproveReport(int id)
        {
            const string sql = "UPDATE MissingPersonReports SET Status='Active', ApprovedAt=UTC_TIMESTAMP(), ApprovedBy=@Admin WHERE ReportID=@Id AND Status='Pending'";
            await using var conn = await OpenAsync();
            int rows; int? reportedBy=null;
            await using (var get = new MySqlCommand("SELECT ReportedBy, Name FROM MissingPersonReports WHERE ReportID=@i", conn))
            { get.Parameters.AddWithValue("@i", id); await using var r = await get.ExecuteReaderAsync(); if(await r.ReadAsync()){ if(!r.IsDBNull(0)) reportedBy = r.GetInt32(0); } }
            await using (var cmd = new MySqlCommand(sql, conn)) { cmd.Parameters.AddWithValue("@Id", id); cmd.Parameters.AddWithValue("@Admin", CurrentUserId); rows = await cmd.ExecuteNonQueryAsync(); }
            if (rows == 0) return NotFound();
            if(reportedBy.HasValue) await AddNotification(conn, reportedBy.Value, "Approved", $"Your report #{id} was approved", id);
            return Ok(new { message = "Approved", id });
        }

        // Mark as Found
        [HttpPost("{id}/found")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> MarkFound(int id)
        {
            const string sql = "UPDATE MissingPersonReports SET Status='Found' WHERE ReportID=@Id AND Status IN ('Active')";
            await using var conn = await OpenAsync();
            int rows; int? reportedBy=null;
            await using (var get = new MySqlCommand("SELECT ReportedBy FROM MissingPersonReports WHERE ReportID=@i", conn)) { get.Parameters.AddWithValue("@i", id); var o= await get.ExecuteScalarAsync(); if(o!=null && o!=DBNull.Value) reportedBy=Convert.ToInt32(o); }
            await using var cmd = new MySqlCommand(sql, conn); cmd.Parameters.AddWithValue("@Id", id); rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return BadRequest(new { message = "Cannot mark as Found (must be Active)." });
            if(reportedBy.HasValue) await AddNotification(conn, reportedBy.Value, "StatusChange", $"Report #{id} marked Found", id);
            return Ok(new { message = "Marked Found", id });
        }

        // Mark as Closed
        [HttpPost("{id}/close")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> MarkClosed(int id)
        {
            const string sql = "UPDATE MissingPersonReports SET Status='Closed' WHERE ReportID=@Id AND Status IN ('Active','Found')";
            await using var conn = await OpenAsync();
            int rows; int? reportedBy=null;
            await using (var get = new MySqlCommand("SELECT ReportedBy FROM MissingPersonReports WHERE ReportID=@i", conn)) { get.Parameters.AddWithValue("@i", id); var o= await get.ExecuteScalarAsync(); if(o!=null && o!=DBNull.Value) reportedBy=Convert.ToInt32(o); }
            await using var cmd = new MySqlCommand(sql, conn); cmd.Parameters.AddWithValue("@Id", id); rows = await cmd.ExecuteNonQueryAsync();
            if (rows == 0) return BadRequest(new { message = "Cannot close (must be Active or Found)." });
            if(reportedBy.HasValue) await AddNotification(conn, reportedBy.Value, "StatusChange", $"Report #{id} marked Closed", id);
            return Ok(new { message = "Marked Closed", id });
        }

        // Decline (pending only)
        [HttpPost("{id}/decline")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> DeclineReport(int id)
        {
            const string sql = "DELETE FROM MissingPersonReports WHERE ReportID=@Id AND Status='Pending'";
            await using var conn = await OpenAsync();
            int? reportedBy=null; string name="";
            await using (var get = new MySqlCommand("SELECT ReportedBy, Name FROM MissingPersonReports WHERE ReportID=@i", conn)) { get.Parameters.AddWithValue("@i", id); await using var r= await get.ExecuteReaderAsync(); if(await r.ReadAsync()){ if(!r.IsDBNull(0)) reportedBy=r.GetInt32(0); name=r.GetString(1);} }
            int rows; await using (var cmd = new MySqlCommand(sql, conn)) { cmd.Parameters.AddWithValue("@Id", id); rows = await cmd.ExecuteNonQueryAsync(); }
            if (rows == 0) return BadRequest("Not pending or not found");
            if(reportedBy.HasValue) await AddNotification(conn, reportedBy.Value, "Declined", $"Your report '{name}' was declined", id);
            return Ok(new { message = "Deleted" });
        }

        // Full delete any status (also remove dependents)
        [HttpDelete("{id}")]
        [Authorize(Roles="Admin")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            await using var conn = await OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                async Task Exec(string s){ await using var c = new MySqlCommand(s, conn, (MySqlTransaction)tx); c.Parameters.AddWithValue("@Id", id); await c.ExecuteNonQueryAsync(); }
                await Exec("DELETE FROM Alerts WHERE ReportID=@Id");
                await Exec("DELETE FROM Medias WHERE ReportID=@Id");
                await Exec("DELETE FROM AssignedVolunteers WHERE ReportID=@Id");
                await Exec("DELETE FROM ReportTimeline WHERE ReportID=@Id");
                await Exec("DELETE FROM MissingPersonReports WHERE ReportID=@Id");
                await tx.CommitAsync();
                return Ok(new { message = "Deleted", id });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { message = "Delete failed", detail = ex.Message });
            }
        }
    }
}

internal static class DataReaderExtensions
{
    public static bool ColumnExists(this MySqlDataReader r, string name)
    {
        for (int i = 0; i < r.FieldCount; i++)
            if (string.Equals(r.GetName(i), name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
    public static string? SafeGetString(this MySqlDataReader r, string name)
    {
        if (!ColumnExists(r, name)) return null;
        var ord = r.GetOrdinal(name);
        return r.IsDBNull(ord) ? null : r.GetString(ord);
    }
}
