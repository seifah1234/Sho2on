using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class UserRoleService
    {
        private readonly AppDbContext _db;
        public UserRoleService(AppDbContext db) => _db = db;

        public async Task<List<(int Id, string Name)>> GetAllRolesAsync() =>
            await _db.Roles.OrderBy(r => r.RoleName).Select(r => new ValueTuple<int, string>(r.RoleID, r.RoleName)).ToListAsync();

        public async Task<List<int>> GetUserRoleIdsAsync(int userId) =>
            await _db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync();

        public async Task SaveUserRolesAsync(int userId, List<int> roleIds)
        {
            var existing = await _db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();
            _db.UserRoles.RemoveRange(existing);

            foreach (var rid in roleIds)
                _db.UserRoles.Add(new UserRole { UserId = userId, RoleId = rid });

            await _db.SaveChangesAsync();
        }

        public async Task<(bool Success, string Message)> SetPasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                return (false, "كلمة المرور يجب ألا تقل عن 6 أحرف");

            var user = await _db.Users.FindAsync(userId);
            if (user == null) return (false, "المستخدم غير موجود");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            return (true, $"تم تعيين كلمة مرور جديدة لـ {user.FullName} بنجاح");
        }

        public async Task<string?> GetUsernameAsync(int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            return user?.Username;
        }
    }
}