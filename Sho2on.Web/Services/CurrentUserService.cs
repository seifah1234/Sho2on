using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using Sho2on.Web.Constants;

namespace Sho2on.Web.Services
{
    public class CurrentUserService
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IDbContextFactory<AppDbContext> _contextFactory; 

        public CurrentUserService(AuthenticationStateProvider authProvider, IDbContextFactory<AppDbContext> contextFactory)
        {
            _authProvider = authProvider;
            _contextFactory = contextFactory;
        }

        public async Task<int?> GetCurrentUserIdAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity?.IsAuthenticated ?? true)
                return null;

            var userIdClaim = user.FindFirst("UserId");
            if (userIdClaim == null)
                return null;

            return int.TryParse(userIdClaim.Value, out int id) ? id : null;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (!userId.HasValue)
                return null;

            using var _db = await _contextFactory.CreateDbContextAsync();
            return await _db.Users.Include(u => u.JobTitle).Include(u => u.UserRoles).ThenInclude(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId.Value);
        }

        public async Task<bool> IsAdminAsync()
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();
            return authState.User.IsInRole(RoleNames.Admin);
        }

        // بيرجع true لو المستخدم شايف الصفحة/الفيتشر دي (View)، الأدمن دايمًا true.
        public async Task<bool> CanViewAsync(string permissionKey)
        {
            if (await IsAdminAsync()) return true;
            var authState = await _authProvider.GetAuthenticationStateAsync();
            return authState.User.Claims.Any(c => c.Type == "perm" && c.Value == permissionKey);
        }

        // بيرجع true لو المستخدم يقدر يعدل/يضيف/يحذف في الصفحة/الفيتشر دي، الأدمن دايمًا true.
        public async Task<bool> CanEditAsync(string permissionKey)
        {
            //if (await IsAdminAsync()) return true;
            var authState = await _authProvider.GetAuthenticationStateAsync();
            return authState.User.Claims.Any(c => c.Type == "perm-edit" && c.Value == permissionKey);
        }

        /// <summary>
        /// حارس أمان لعمليات الكتابة (Add/Edit/Delete). لازم يُنادى في بداية كل ميثود Save/Delete
        /// في الـ Services، عشان الحماية متبقاش بس إخفاء زراير في الواجهة (اللي ممكن يتلعب فيها)،
        /// وده يمنع أي طلب مباشر (API/فورم) من يوزر معاه View بس من غير Edit.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">لو المستخدم مالوش صلاحية Edit على الـ permissionKey ده</exception>
        public async Task RequireEditAsync(string permissionKey)
        {
            if (!await CanEditAsync(permissionKey))
                throw new UnauthorizedAccessException($"لا تملك صلاحية التعديل على: {permissionKey}");
        }
    }
}