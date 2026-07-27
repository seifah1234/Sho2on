using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sho2on.Web.Services;
using System.Security.Claims;

namespace Sho2on.Web.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/account/login", async (HttpContext http, AuthService authService, ILogger<Program> logger) =>
            {
                var form = await http.Request.ReadFormAsync();
                var username = form["username"].ToString();
                var password = form["password"].ToString();

                try
                {
                    var (success, user, error, roles, permissions) = await authService.LoginAsync(username, password);

                    if (!success)
                        return Results.Redirect("/login?error=invalid");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim("FullName", user.FullName),
                        new Claim("UserId", user.Id.ToString())
                    };
                    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
                    claims.AddRange(permissions.Select(p => new Claim("perm", p)));

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                    return Results.Redirect("/");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "خطأ أثناء محاولة تسجيل الدخول للمستخدم {Username}", username);
                    return Results.Redirect("/login?error=connection");
                }
            });

            app.MapPost("/account/logout", async (HttpContext http) =>
            {
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            });
        }
    }
}
