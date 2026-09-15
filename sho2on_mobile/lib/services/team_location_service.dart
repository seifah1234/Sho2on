import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class TeamLocationService {
  static const String baseUrl = ApiConfig.baseUrl;

  // الحصول على آخر مواقع معروفة لكل موظفين المدير
  Future<Map<String, dynamic>> getTeamLocations({
    required int managerId,
  }) async {
    try {
      final response = await http
          .get(
            Uri.parse('$baseUrl/Location/TeamLocations/$managerId'),
            headers: {
              'Content-Type': 'application/json',
              'Authorization': 'Bearer YOUR_TOKEN',
            },
          )
          .timeout(const Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'] ?? [],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
          };
        }
      } else {
        return {
          'success': false,
          'message': 'فشل في تحميل مواقع الموظفين (${response.statusCode})',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }
}
