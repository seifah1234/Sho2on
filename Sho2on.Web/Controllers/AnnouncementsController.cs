using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnnouncementsController : ControllerBase
    {
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ILogger<AnnouncementsController> _logger;

        public AnnouncementsController(IDbContextFactory<AppDbContext> dbFactory, ILogger<AnnouncementsController> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        // GET: api/Announcements
        [HttpGet]
        public async Task<IActionResult> GetAnnouncements()
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                // التحقق من الإعلانات المنتهية
                await CheckExpiredAnnouncements();

                var announcements = await _db.Announcements
                    .Include(a => a.AnnouncementType)
                    .Include(a => a.CreatedBy)
                    .Where(a => !a.IsDeleted)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Description,
                        a.AnnouncementTypeId,
                        TypeName = a.AnnouncementType != null ? a.AnnouncementType.Name : "عام",
                        Color = a.AnnouncementType != null ? a.AnnouncementType.Color : "#FF64748B",
                        a.CreatedAt,
                        a.ExpireDate,
                        a.IsDeleted,
                        CreatedByName = a.CreatedBy != null ? a.CreatedBy.FullName : ""
                    })
                    .ToListAsync();

                return Ok(announcements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل الإعلانات");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء تحميل الإعلانات" });
            }
        }

        // GET: api/Announcements/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnnouncement(int id)
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var announcement = await _db.Announcements
                    .Include(a => a.AnnouncementType)
                    .Include(a => a.CreatedBy)
                    .Where(a => a.Id == id && !a.IsDeleted)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Description,
                        a.AnnouncementTypeId,
                        TypeName = a.AnnouncementType != null ? a.AnnouncementType.Name : "عام",
                        Color = a.AnnouncementType != null ? a.AnnouncementType.Color : "#FF64748B",
                        a.CreatedAt,
                        a.ExpireDate,
                        a.IsDeleted,
                        CreatedByName = a.CreatedBy != null ? a.CreatedBy.FullName : ""
                    })
                    .FirstOrDefaultAsync();

                if (announcement == null)
                    return NotFound(new { success = false, message = "الإعلان غير موجود" });

                return Ok(announcement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل الإعلان {Id}", id);
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // GET: api/Announcements/types
        [HttpGet("types")]
        public async Task<IActionResult> GetAnnouncementTypes()
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var types = await _db.AnnouncementTypes
                    .OrderBy(t => t.Name)
                    .Select(t => new
                    {
                        t.Id,
                        t.Name
                    })
                    .ToListAsync();

                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل أنواع الإعلانات");
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // POST: api/Announcements
        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequest request)
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { success = false, message = "العنوان مطلوب" });

                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { success = false, message = "المحتوى مطلوب" });

                // التحقق من وجود النوع
                if (request.AnnouncementTypeId.HasValue)
                {
                    var typeExists = await _db.AnnouncementTypes.AnyAsync(t => t.Id == request.AnnouncementTypeId.Value);
                    if (!typeExists)
                        return BadRequest(new { success = false, message = "نوع الإعلان غير موجود" });
                }

                var announcement = new Announcement
                {
                    Title = request.Title.Trim(),
                    Description = request.Content.Trim(),
                    AnnouncementTypeId = request.AnnouncementTypeId.Value,
                    CreatedById = request.CreatedByUserId,
                    CreatedAt = DateTime.Now,
                    ExpireDate = request.ExpireDate,
                    IsDeleted = false
                };

                await _db.Announcements.AddAsync(announcement);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم إنشاء الإعلان بنجاح",
                    data = new { announcement.Id, announcement.Title }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إنشاء إعلان");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء إنشاء الإعلان" });
            }
        }

        // PUT: api/Announcements/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncement(int id, [FromBody] AnnouncementRequest request)
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var announcement = await _db.Announcements.FindAsync(id);
                if (announcement == null)
                    return NotFound(new { success = false, message = "الإعلان غير موجود" });

                if (string.IsNullOrWhiteSpace(request.Title))
                    return BadRequest(new { success = false, message = "العنوان مطلوب" });

                if (string.IsNullOrWhiteSpace(request.Content))
                    return BadRequest(new { success = false, message = "المحتوى مطلوب" });

                announcement.Title = request.Title.Trim();
                announcement.Description = request.Content.Trim();
                announcement.AnnouncementTypeId = request.AnnouncementTypeId.Value;
                announcement.ExpireDate = request.ExpireDate;

                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "تم تحديث الإعلان بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحديث الإعلان {Id}", id);
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء التحديث" });
            }
        }

        // DELETE: api/Announcements/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var announcement = await _db.Announcements.FindAsync(id);
                if (announcement == null)
                    return NotFound(new { success = false, message = "الإعلان غير موجود" });

                // حذف منطقي
                announcement.IsDeleted = true;

                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "تم حذف الإعلان بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف الإعلان {Id}", id);
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء الحذف" });
            }
        }

        // GET: api/Announcements/active
        [HttpGet("active")]
        public async Task<IActionResult> GetActiveAnnouncements()
        {
            try
            {
                await CheckExpiredAnnouncements();

        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var announcements = await _db.Announcements
                    .Include(a => a.AnnouncementType)
                    .Where(a => !a.IsDeleted && (!a.ExpireDate.HasValue || a.ExpireDate.Value >= DateTime.Now))
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new
                    {
                        a.Id,
                        a.Title,
                        a.Description,
                        a.AnnouncementTypeId,
                        TypeName = a.AnnouncementType != null ? a.AnnouncementType.Name : "عام",
                        a.CreatedAt,
                        a.ExpireDate
                    })
                    .ToListAsync();

                return Ok(announcements);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل الإعلانات النشطة");
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // GET: api/Announcements/count
        [HttpGet("count")]
        public async Task<IActionResult> GetActiveAnnouncementsCount()
        {
            try
            {
                await CheckExpiredAnnouncements();

        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var count = await _db.Announcements
                    .CountAsync(a => !a.IsDeleted && (!a.ExpireDate.HasValue || a.ExpireDate.Value >= DateTime.Now));

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء تحميل عدد الإعلانات");
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // POST: api/Announcements/types
        [HttpPost("types")]
        public async Task<IActionResult> CreateAnnouncementType([FromBody] AnnouncementTypeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    return BadRequest(new { success = false, message = "الاسم مطلوب" });

        using var _db = await _dbFactory.CreateDbContextAsync(); 
                // التحقق من عدم وجود نفس الاسم
                var exists = await _db.AnnouncementTypes.AnyAsync(t => t.Name == request.Name.Trim());
                if (exists)
                    return BadRequest(new { success = false, message = "هذا النوع موجود مسبقاً" });

                var type = new AnnouncementType
                {
                    Name = request.Name.Trim(),
                };

                await _db.AnnouncementTypes.AddAsync(type);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "تم إنشاء النوع بنجاح", data = type });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء إنشاء نوع إعلان");
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // DELETE: api/Announcements/types/{id}
        [HttpDelete("types/{id}")]
        public async Task<IActionResult> DeleteAnnouncementType(int id)
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var type = await _db.AnnouncementTypes.FindAsync(id);
                if (type == null)
                    return NotFound(new { success = false, message = "النوع غير موجود" });

                _db.AnnouncementTypes.Remove(type);
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "تم حذف النوع بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء حذف نوع الإعلان {Id}", id);
                return StatusCode(500, new { success = false, message = "حدث خطأ" });
            }
        }

        // ✅ التحقق من الإعلانات المنتهية
        private async Task CheckExpiredAnnouncements()
        {
            try
            {
        using var _db = await _dbFactory.CreateDbContextAsync(); 
                var expiredAnnouncements = await _db.Announcements
                    .Where(a => !a.IsDeleted && a.ExpireDate.HasValue && a.ExpireDate.Value < DateTime.Now)
                    .ToListAsync();

                if (expiredAnnouncements.Any())
                {
                    foreach (var announcement in expiredAnnouncements)
                    {
                        announcement.IsDeleted = true;
                    }
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ أثناء التحقق من الإعلانات المنتهية");
            }
        }
    }

    // ==================== DTOs ====================
    public class AnnouncementRequest
    {
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public int? AnnouncementTypeId { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime? ExpireDate { get; set; }
    }

    public class AnnouncementTypeRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }
}