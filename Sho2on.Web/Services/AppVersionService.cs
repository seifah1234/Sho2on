using Microsoft.EntityFrameworkCore;
using Sho2on.Database;

namespace Sho2on.Web.Services
{
    public class AppVersionService
    {
        private readonly IWebHostEnvironment _env;

        public AppVersionService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<AppVersionInfo> GetLatestVersionAsync()
        {
            var downloadsFolder = Path.Combine(_env.WebRootPath, "downloads");
            var androidFile = Path.Combine(downloadsFolder, "rakz-app.apk");

            var info = new AppVersionInfo
            {
                AndroidUrl = "/downloads/rakz-app.apk", // الرابط ثابت دائماً
                AndroidVersion = "1.0.0",
                AndroidAvailable = File.Exists(androidFile) && new FileInfo(androidFile).Length > 0,
                IosUrl = "",
                IosVersion = "",
                IosAvailable = false
            };

            return info;
        }
    }

    public class AppVersionInfo
    {
        public string AndroidUrl { get; set; } = "";
        public string AndroidVersion { get; set; } = "";
        public bool AndroidAvailable { get; set; }
        public string IosUrl { get; set; } = "";
        public string IosVersion { get; set; } = "";
        public bool IosAvailable { get; set; }
    }
}