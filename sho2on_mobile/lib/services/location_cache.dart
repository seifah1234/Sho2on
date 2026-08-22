import 'package:shared_preferences/shared_preferences.dart';

class LocationCache {
  static const _key = 'last_location_name';

  static Future<void> save(String locationName) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_key, locationName);
  }

  static Future<String?> get() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_key);
  }
}
