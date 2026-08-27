using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sho2on.API.Dtos;
using Sho2on.Database;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public AuthController(AppDbContext db, IConfiguration config) { _db = db; _config = config; }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users
                .Include(u => u.Branch)
                .Include(u => u.JobTitle)
                .Include(u => u.Attendances)
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user == null)
                return BadRequest("الموظف غير موجود");

            if (!user.IsMobileUser.HasValue || !user.IsMobileUser.Value)
                return BadRequest("هذا الحساب ليس حساب موبايل");

            if (user.PasswordHash == null)
                return BadRequest("غير مسجل");

            if (user.PasswordHash != dto.Password)
                return BadRequest("بيانات غير صحيحة");

            //if (user.RegisteredDeviceId != dto.DeviceId)
            //    return BadRequest("الجهاز غير مسجل");

            var attendances = user.Attendances;

            var present = attendances.Count(a => !a.IsAbsence && !a.IsHoliday);
            var absent = attendances.Count(a => a.IsAbsence);
            var late = attendances.Count(a => !a.ExemptLate && a.Late > TimeSpan.Zero);
            var vacation = attendances.Count(a => a.LeaveId.HasValue);

            var today = DateTime.Today;
            var todayAttendance = attendances
                .FirstOrDefault(a => a.AttendanceDate.Date == today);

            string status = "لم يحضر";
            if (todayAttendance != null)
            {
                if (todayAttendance.CheckInTime != null && todayAttendance.CheckOutTime == null)
                    status = "حاضر";
                else if (todayAttendance.CheckOutTime != null)
                    status = "منصرف";
            }

            return Ok(new
            {
                id = user.Id,
                employeeId = user.Code,
                fullName = user.FullName,
                mainSalary = user.MainSalary,
                email = user.Email,
                phone = user.PhoneNumber,
                managerId = user.ManagerId,
                profileImageData = user.ProfileImageData,
                isManager = user.JobTitle.IsManager,
                branch = new
                {
                    id = user.Branch.Id,
                    name = user.Branch.Name
                },
                today = new
                {
                    checkIn = todayAttendance?.CheckInTime?.ToString(@"hh\:mm"),
                    checkOut = todayAttendance?.CheckOutTime?.ToString(@"hh\:mm"),
                    status
                },
                stats = new
                {
                    present,
                    absent,
                    late,
                    vacation
                },

                chatToken = GenerateChatToken(user.Id)
            });
        }

        private string GenerateChatToken(int userId)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Code == dto.Id);
            if (user == null) return BadRequest("الموظف غير موجود");
            if (user.PasswordHash != null) return BadRequest("أنت مسجل بالفعل");

            var settings = await _db.Settings.FirstAsync();
            int usedUsers = await _db.Users.CountAsync(x => x.IsMobileUser.HasValue && x.IsMobileUser.Value);
            if (usedUsers >= settings.MaxMobileUsers)
                return BadRequest("عدد المستخدمين المسموح به ممتلئ");

            user.PasswordHash = dto.Password;
            user.RegisteredDeviceId = dto.DeviceId;

            await _db.SaveChangesAsync();
            return Ok("success");
        }


    }
}
