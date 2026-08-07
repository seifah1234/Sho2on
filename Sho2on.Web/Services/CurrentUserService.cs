using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;

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
            return await _db.Users.Include(u => u.JobTitle).FirstOrDefaultAsync(u => u.Id == userId.Value);
        }
    }
}