using Microsoft.Extensions.Logging;
using Sho2onMobile.Services;

namespace Sho2onMobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

#if ANDROID
            builder.Services.AddSingleton<IDeviceService, Sho2onMobile.Platforms.Android.DeviceService>();
#elif IOS
        builder.Services.AddSingleton<IDeviceService, Sho2onMobile.Platforms.iOS.DeviceService>();
#else
        builder.Services.AddSingleton<IDeviceService, Sho2onMobile.Services.MockDeviceService>();
#endif


            return builder.Build();
        }
    }
}
