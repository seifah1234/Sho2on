using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.API.Dtos;
using Sho2on.Database;
using Sho2on.Database.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        // موظف يعتبر "أونلاين" لو آخر تحديث موقع وصل خلال آخر دقيقتين
        private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(2);

        public LocationController(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

        // POST: api/Location/Update
        // بيتنادى من موبايل الموظف كل فترة (مثلاً كل 30-60 ثانية) أثناء الوردية
        [HttpPost("Update")]
        public async Task<ActionResult<ApiResponse<string>>> UpdateLocation([FromBody] UpdateLocationDto dto)
        {
            try
            {
                if (dto.UserId <= 0)
                {
                    return BadRequest(new ApiResponse<string> { Success = false, Message = "بيانات الموظف غير صحيحة" });
                }

                using var _context = await _dbFactory.CreateDbContextAsync();

                var userExists = await _context.Users.AnyAsync(u => u.Id == dto.UserId);
                if (!userExists)
                {
                    return NotFound(new ApiResponse<string> { Success = false, Message = "المستخدم غير موجود" });
                }

                var existing = await _context.EmployeeLiveLocations
                    .FirstOrDefaultAsync(l => l.UserId == dto.UserId);

                if (existing == null)
                {
                    existing = new EmployeeLiveLocation { UserId = dto.UserId };
                    _context.EmployeeLiveLocations.Add(existing);
                }

                existing.Latitude = dto.Latitude;
                existing.Longitude = dto.Longitude;
                existing.Accuracy = dto.Accuracy;
                existing.IsOnShift = dto.IsOnShift;
                existing.UpdatedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new ApiResponse<string> { Success = true, Message = "تم تحديث الموقع" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<string> { Success = false, Message = $"خطأ: {ex.Message}" });
            }
        }

        // GET: api/Location/TeamLocations/{managerId}
        // بيتنادى من شاشة المدير: بيرجع آخر موقع معروف لكل موظف تابع له
        [HttpGet("TeamLocations/{managerId}")]
        public async Task<ActionResult<ApiResponse<List<EmployeeLocationDto>>>> GetTeamLocations(int managerId)
        {
            try
            {
                using var _context = await _dbFactory.CreateDbContextAsync();

                var manager = await _context.Users
                    .Include(u => u.JobTitle)
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    return NotFound(new ApiResponse<List<EmployeeLocationDto>> { Success = false, Message = "المستخدم غير موجود" });
                }

                if (manager.JobTitle == null || !manager.JobTitle.IsManager.HasValue || !manager.JobTitle.IsManager.Value)
                {
                    return BadRequest(new ApiResponse<List<EmployeeLocationDto>> { Success = false, Message = "المستخدم ليس لديه صلاحيات مدير" });
                }

                var now = DateTime.UtcNow;

                var teamLocations = await _context.Users
                    .Where(u => u.ManagerId == managerId)
                    .Include(u => u.JobTitle)
                    .Include(u => u.Department)
                    .Select(u => new
                    {
                        u.Id,
                        u.FullName,
                        JobTitleName = u.JobTitle != null ? u.JobTitle.Name : null,
                        DepartmentName = u.Department != null ? u.Department.Name : null,
                        Location = _context.EmployeeLiveLocations.FirstOrDefault(l => l.UserId == u.Id)
                    })
                    .Where(x => x.Location != null)
                    .ToListAsync();

                var result = teamLocations
                    .Select(x => new EmployeeLocationDto
                    {
                        UserId = x.Id,
                        FullName = x.FullName,
                        JobTitleName = x.JobTitleName,
                        DepartmentName = x.DepartmentName,
                        Latitude = x.Location!.Latitude,
                        Longitude = x.Location.Longitude,
                        Accuracy = x.Location.Accuracy,
                        UpdatedAtUtc = x.Location.UpdatedAtUtc,
                        IsOnShift = x.Location.IsOnShift,
                        IsRecentlyActive = (now - x.Location.UpdatedAtUtc) <= OnlineThreshold,
                    })
                    .OrderByDescending(x => x.IsRecentlyActive)
                    .ThenByDescending(x => x.UpdatedAtUtc)
                    .ToList();

                return Ok(new ApiResponse<List<EmployeeLocationDto>>
                {
                    Success = true,
                    Data = result,
                    TotalRecords = result.Count,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<EmployeeLocationDto>> { Success = false, Message = $"خطأ: {ex.Message}" });
            }
        }
    }
}
