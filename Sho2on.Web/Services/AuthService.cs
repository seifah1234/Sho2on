using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        public AuthService(IDbContextFactory<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<(bool Success, User? User, string? Error, List<string>? roles, List<string>? rolePermissions, List<string>? editPermissions)> LoginAsync(string username, string password)
        {
            using var _db = await _dbFactory.CreateDbContextAsync();
            var user = await _db.Users
                .Include(u => u.JobTitle)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return (false, null, "اسم المستخدم او كلمة المرور غير صحيحة", null, null, null);
            }

            var roles = await _db.UserRoles
    .Where(ur => ur.UserId == user.Id)
    .Select(ur => ur.Role.RoleName)
    .ToListAsync();

            // لو المستخدم عنده أكتر من Role وأي واحد منهم عنده View على الصفحة، يبقى شايفها (Union)
            // بنستخدم & بدل HasFlag عشان تتترجم صح لـ SQL
            var permissions = await _db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.RoleName) && (rp.AccessLevel & AccessLevel.View) == AccessLevel.View)
                .Select(rp => rp.Permission.PermissionName)
                .Distinct()
                .ToListAsync();

            // نفس المنطق لصلاحية التعديل: لو أي Role من رولاته عنده Edit على الصفحة، يبقى يقدر يعدل فيها
            var editPermissions = await _db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.RoleName) && (rp.AccessLevel & AccessLevel.Edit) == AccessLevel.Edit)
                .Select(rp => rp.Permission.PermissionName)
                .Distinct()
                .ToListAsync();

            bool isValid;

            if (user.PasswordHash == null)
            {
                isValid = false;
            }
            else if (user.PasswordHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                if (isValid)
                {
                    user.PasswordHash = password;
                    await _db.SaveChangesAsync();

                }
            }
            else
            {
                isValid = user.PasswordHash == password;
            }

            if (!isValid)
            {
                return (false, null, "اسم المستخدم او كلمة المرور غير صحيحة", null, null, null);
            }

            return (true, user, null, roles, permissions, editPermissions);
        }

    }
}
