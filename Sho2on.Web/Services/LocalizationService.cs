using System.Text.Json;
using Microsoft.JSInterop;

namespace Sho2on.Web.Services
{
    public class LocalizationService
    {
        private readonly Dictionary<string, string> _arToEn;
        private readonly IJSRuntime _js;

        public string CurrentLanguage { get; private set; }
        public bool IsRtl => CurrentLanguage == "ar";
        public event Action? OnChange;

        public LocalizationService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, IJSRuntime js)
        {
            _js = js;

            var path = Path.Combine(env.WebRootPath, "i18n", "translations_ar_en.json");
            _arToEn = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;

            // نقرأ الاختيار المحفوظ من الكوكي وقت أول تحميل للصفحة (Circuit جديد ولا قديم، مش فارق)
            var cookieLang = httpContextAccessor.HttpContext?.Request.Cookies["sho2on_lang"];
            CurrentLanguage = cookieLang == "en" ? "en" : "ar";
        }

        public string T(string arabicText)
        {
            if (CurrentLanguage == "ar") return arabicText;
            return _arToEn.TryGetValue(arabicText, out var en) ? en : arabicText;
        }

        public async Task Toggle()
        {
            CurrentLanguage = CurrentLanguage == "ar" ? "en" : "ar";
            OnChange?.Invoke();
            await _js.InvokeVoidAsync("setLangCookie", CurrentLanguage);
        }
    }
}