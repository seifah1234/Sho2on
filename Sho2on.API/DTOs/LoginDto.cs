namespace Sho2on.API.DTOs
{
    public class LoginDto
    {
        public string Id { get; set; } = null!;  // ممكن يكون رقم الموظف أو أي معرف
        public string Password { get; set; } = null!;
        public string DeviceId { get; set; } = null!;  // الجهاز اللي هيستخدم التطبيق
    }
}
