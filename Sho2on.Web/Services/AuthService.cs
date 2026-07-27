using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        public AuthService(AppDbContext db) => _db = db;

        public async Task<(bool Success, User? User, string? Error, List<string>? roles, List<string>? rolePermissions)> LoginAsync(string username, string password)
        {
            var user = await _db.Users
                .Include(u => u.JobTitle)
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                return (false, null, "اسم المستخدم او كلمة المرور غير صحيحة", null, null);
            }

            var roles = await _db.UserRoles
    .Where(ur => ur.UserId == user.Id)
    .Select(ur => ur.Role.RoleName)
    .ToListAsync();

            var permissions = await _db.RolePermissions
                .Where(rp => roles.Contains(rp.Role.RoleName))
                .Select(rp => rp.Permission.PermissionName)
                .Distinct()
                .ToListAsync();

            bool isValid;

            if (user.PasswordHash.StartsWith("$2"))
            {
                isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }
            else
            {
                isValid = user.PasswordHash == password;

                if (isValid)
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                    await _db.SaveChangesAsync();
                }
            }

            if (!isValid)
            {
                return (false, null, "اسم المستخدم او كلمة المرور غير صحيحة", null, null);
            }

            return (true, user, null, roles, permissions);
        }

    }
}
