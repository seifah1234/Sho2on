using Android.Provider;
using Android.App;
using Sho2onMobile.Services;
using Application = Android.App.Application;

namespace Sho2onMobile.Platforms.Android
{
    public class DeviceService : IDeviceService
    {
        public string GetDeviceId()
        {
            return Settings.Secure.GetString(
                Application.Context.ContentResolver,
                Settings.Secure.AndroidId);
        }
    }
}
