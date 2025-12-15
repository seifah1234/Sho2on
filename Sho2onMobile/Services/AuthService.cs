using Microsoft.EntityFrameworkCore;
using Sho2on.Database;
using Sho2on.Database.Models;
using System.Security.Cryptography;
using System.Text;

public class AuthService
{
    public static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    public async Task<User?> LoginAsync(string id, string password, string deviceMac)
    {
        using var db = new AppDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == id);

        if (user == null || user.PasswordHash == null)
            return null;

        if (user.PasswordHash != HashPassword(password))
            return null;

        // منع اي حد يدخل من جهاز تاني
        if (user.RegisteredDeviceId != deviceMac)
            return null;

        return user;
    }

    public async Task<string> RegisterAsync(string id, string password, string deviceMac)
    {
        using var db = new AppDbContext();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id.ToString() == id);

        if (user == null)
            return "الموظف غير موجود";

        if (!user.IsMobileUser.HasValue || !user.IsMobileUser.Value)
            return "غير مسموح لك باستخدام التطبيق";

        if (!string.IsNullOrEmpty(user.PasswordHash))
            return "أنت مسجل بالفعل";

        // الحد المسموح للمستخدمين
        var settings = await db.Settings.FirstAsync();
        int usedUsers = await db.Users.CountAsync(x => x.PasswordHash != null);

        if (usedUsers >= settings.MaxMobileUsers)
            return "عدد المستخدمين المسموح به ممتلئ";

        user.PasswordHash = HashPassword(password);
        user.RegisteredDeviceId = deviceMac;

        db.SaveChanges();

        return "success";
    }
}
