using Sho2onMobile.Services;

namespace Sho2onMobile.Platforms.iOS
{
    public class DeviceService : IDeviceService
    {
        public string GetDeviceId() => "ios-device";
    }
}
