import 'package:device_info_plus/device_info_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:uuid/uuid.dart';

class DeviceHelper {
  static const _key = 'sho2on_device_id';

  static Future<String> getDeviceId() async {
    final prefs = await SharedPreferences.getInstance();
    var id = prefs.getString(_key);
    if (id != null && id.isNotEmpty) return id;

    final deviceInfo = DeviceInfoPlugin();
    try {
      final info = await deviceInfo.deviceInfo;
      // try to create deterministic id from device info (fallback to uuid)
      final map = info.data;
      id = map['id']?.toString() ?? map['androidId']?.toString() ?? map['identifierForVendor']?.toString();
    } catch (_) {
      // ignored
    }

    id ??= const Uuid().v4();
    await prefs.setString(_key, id);
    return id;
  }
}
