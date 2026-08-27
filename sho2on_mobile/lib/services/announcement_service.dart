import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class AnnouncementService {
  static const String baseUrl = ApiConfig.baseUrl;

  Map<String, String> _getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN',
    };
  }

  // الحصول على جميع الإعلانات
  Future<Map<String, dynamic>> getAnnouncements() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Announcements'),
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'data': data is List ? data : [],
        };
      } else {
        return {'success': false, 'message': 'فشل تحميل الإعلانات'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ: $e'};
    }
  }

  // الحصول على أنواع الإعلانات
  Future<Map<String, dynamic>> getAnnouncementTypes() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Announcements/types'),
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {'success': true, 'data': data is List ? data : []};
      } else {
        return {'success': false, 'message': 'فشل تحميل الأنواع'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ: $e'};
    }
  }

  // إنشاء إعلان جديد
  Future<Map<String, dynamic>> createAnnouncement({
    required String title,
    required String content,
    required int typeId,
    required int createdByUserId,
    DateTime? expireDate,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Announcements'),
        headers: _getHeaders(),
        body: json.encode({
          'title': title,
          'content': content,
          'announcementTypeId': typeId,
          'createdByUserId': createdByUserId,
          'expireDate': expireDate?.toIso8601String(),
        }),
      ).timeout(Duration(seconds: 15));

      final data = json.decode(response.body);
      if (response.statusCode == 200 || response.statusCode == 201) {
        return {'success': true, 'message': 'تم إنشاء الإعلان بنجاح'};
      } else {
        return {'success': false, 'message': data['message'] ?? 'فشل إنشاء الإعلان'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ: $e'};
    }
  }

  // حذف إعلان
  Future<Map<String, dynamic>> deleteAnnouncement(int id) async {
    try {
      final response = await http.delete(
        Uri.parse('$baseUrl/Announcements/$id'),
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        return {'success': true, 'message': 'تم حذف الإعلان'};
      } else {
        return {'success': false, 'message': 'فشل حذف الإعلان'};
      }
    } catch (e) {
      return {'success': false, 'message': 'خطأ: $e'};
    }
  }
}