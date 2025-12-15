using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.API.DTOs;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AuthController(AppDbContext db) { _db = db; }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == dto.Id);
            if (user == null) return BadRequest("الموظف غير موجود");
            if (user.PasswordHash == null) return BadRequest("غير مسجل");
            if (user.PasswordHash != HashPassword(dto.Password)) return BadRequest("بيانات غير صحيحة");
            if (user.RegisteredDeviceId != dto.DeviceId) return BadRequest("الجهاز غير مسجل");
            return Ok(new { user.Id, user.FullName, user.BranchId });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == dto.Id);
            if (user == null) return BadRequest("الموظف غير موجود");
            if (user.PasswordHash != null) return BadRequest("أنت مسجل بالفعل");
            var settings = await _db.Settings.FirstAsync();
            int usedUsers = await _db.Users.CountAsync(x => x.PasswordHash != null);
            if (usedUsers >= settings.MaxMobileUsers) return BadRequest("عدد المستخدمين المسموح به ممتلئ");

            user.PasswordHash = HashPassword(dto.Password);
            user.RegisteredDeviceId = dto.DeviceId;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Id = user.Id,
                FullName = user.FullName,
                BranchId = user.BranchId
            });
        }

        private string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
        }
    }

}
