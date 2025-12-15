using Sho2onMobile.Services;

namespace Sho2onMobile.Platforms.Windows
{
    public class DeviceService : IDeviceService
    {
        public string GetDeviceId() => "windows-device";
    }
}
