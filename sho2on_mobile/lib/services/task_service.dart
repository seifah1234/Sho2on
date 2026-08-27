import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class TaskService {
  static const String baseUrl = ApiConfig.baseUrl;

  Map<String, String> _getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN',
    };
  }

  // الحصول على المهام المسندة لي
  Future<Map<String, dynamic>> getAssignedToMe(int userId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Tasks/assigned-to/$userId'),
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'data': data is List ? data : [],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل تحميل المهام',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على المهام التي كلفت بها
  Future<Map<String, dynamic>> getAssignedByMe(int userId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Tasks/assigned-by/$userId'),
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'data': data is List ? data : [],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل تحميل المهام',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // إنشاء مهمة جديدة
  Future<Map<String, dynamic>> createTask({
    required int assignedByUserId,
    required int assignedToUserId,
    required String description,
    required String type, // "task" أو "order"
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Tasks/create'),
        headers: _getHeaders(),
        body: json.encode({
          'assignedByUserId': assignedByUserId,
          'assignedToUserId': assignedToUserId,
          'description': description,
          'type': type,
        }),
      ).timeout(Duration(seconds: 15));

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['sucess'] == true) {
        return {
          'success': true,
          'data': data['data'],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل إنشاء المهمة',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

// في TaskService.dart - إضافة دالة الحذف
Future<Map<String, dynamic>> deleteTask(int taskId) async {
  try {
    final response = await http.delete(
      Uri.parse('$baseUrl/Tasks/$taskId'),
      headers: _getHeaders(),
    ).timeout(Duration(seconds: 15));

    if (response.statusCode == 200) {
      final data = json.decode(response.body);
      return {
        'success': true,
        'message': data['message'] ?? 'تم حذف المهمة',
      };
    } else {
      return {
        'success': false,
        'message': 'فشل حذف المهمة',
      };
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

  // تحديث حالة المهمة
  Future<Map<String, dynamic>> updateStatus(int taskId, int newStatus) async {
    try {
      final response = await http.put(
        Uri.parse('$baseUrl/Tasks/$taskId/status'),
        headers: _getHeaders(),
        body: json.encode(newStatus),
      ).timeout(Duration(seconds: 15));

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'message': data['message'] ?? 'تم تحديث الحالة',
        };
      } else {
        return {
          'success': false,
          'message': 'فشل تحديث الحالة',
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