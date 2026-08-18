using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

namespace Sho2on.Web.Services
{
    public class RoleListItem
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = "";
        public int PermissionsCount { get; set; }
        public int UsersCount { get; set; }
    }

    public class RoleService
    {
        private readonly AppDbContext _db;
        public RoleService(AppDbContext db) => _db = db;

        public async Task<List<RoleListItem>> GetAllAsync()
        {
            return await _db.Roles
                .Select(r => new RoleListItem
                {
                    Id = r.RoleID,
                    RoleName = r.RoleName,
                    PermissionsCount = r.RolePermissions.Count,
                    UsersCount = r.UserRoles.Count
                })
                .OrderBy(r => r.RoleName)
                .ToListAsync();
        }

        public async Task<List<Permission>> GetAllPermissionsAsync() =>
            await _db.Permissions.OrderBy(p => p.PermissionName).ToListAsync();

        public async Task<List<int>> GetRolePermissionIdsAsync(int roleId) =>
            await _db.RolePermissions.Where(rp => rp.RoleID == roleId).Select(rp => rp.PermissionID).ToListAsync();

        public async Task<(bool Success, string Message)> SaveRoleAsync(int? roleId, string roleName, List<int> permissionIds)
        {
            // منع تكرار الاسم
            var nameExists = await _db.Roles.AnyAsync(r => r.RoleName == roleName && r.RoleID != (roleId ?? 0));
            if (nameExists) return (false, "يوجد Role بنفس الاسم بالفعل");

            Role role;
            if (roleId.HasValue)
            {
                role = await _db.Roles.FindAsync(roleId.Value) ?? throw new Exception("Role غير موجود");
                role.RoleName = roleName;

                var oldPermissions = await _db.RolePermissions.Where(rp => rp.RoleID == roleId.Value).ToListAsync();
                _db.RolePermissions.RemoveRange(oldPermissions);
            }
            else
            {
                role = new Role { RoleName = roleName };
                _db.Roles.Add(role);
                await _db.SaveChangesAsync(); // عشان ناخد الـ Id قبل ما نضيف الصلاحيات
            }

            foreach (var pid in permissionIds)
                _db.RolePermissions.Add(new RolePermission { RoleID = role.RoleID, PermissionID = pid });

            await _db.SaveChangesAsync();
            return (true, "تم الحفظ بنجاح");
        }

        public async Task<(bool Success, string Message)> DeleteRoleAsync(int roleId)
        {
            var usersCount = await _db.UserRoles.CountAsync(ur => ur.RoleId == roleId);
            if (usersCount > 0)
                return (false, $"لا يمكن حذف هذا الـ Role لأنه مستخدم فعليًا بواسطة {usersCount} مستخدم. أزل الربط أولًا من شاشة صلاحيات المستخدم.");

            var role = await _db.Roles.FindAsync(roleId) ?? throw new Exception("Role غير موجود");
            var permissions = await _db.RolePermissions.Where(rp => rp.RoleID == roleId).ToListAsync();
            _db.RolePermissions.RemoveRange(permissions);
            _db.Roles.Remove(role);
            await _db.SaveChangesAsync();
            return (true, "تم الحذف بنجاح");
        }
    }
}