import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class ManagerService {
  static const String baseUrl = ApiConfig.baseUrl;

  // الحصول على إحصائيات فريق المدير
  Future<Map<String, dynamic>> getManagerTeamStats({
    required int managerId,
    DateTime? date,
  }) async {
    try {
      String url = '$baseUrl/Manager/GetTeamStats/$managerId';

      final uri = Uri.parse(url).replace(
        queryParameters: {
          if (date != null) 'date': date.toIso8601String(),
        },
      );

      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
          };
        }
      } else {
        throw Exception('فشل في تحميل إحصائيات الفريق');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على أعضاء فريق المدير
  Future<Map<String, dynamic>> getManagerTeamMembers({
    required int managerId,
    DateTime? date,
  }) async {
    try {
      String url = '$baseUrl/Manager/GetTeamMembers/$managerId';

      final uri = Uri.parse(url).replace(
        queryParameters: {
          if (date != null) 'date': date.toIso8601String(),
        },
      );

      final response = await http.get(
        uri,
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
          };
        }
      } else {
        throw Exception('فشل في تحميل أعضاء الفريق');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على الموظفين الذين عملوا CheckIn اليوم
  Future<Map<String, dynamic>> getTodayCheckIns({
    required int managerId,
  }) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Manager/GetTodayCheckIns/$managerId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
          };
        }
      } else {
        throw Exception('فشل في تحميل بيانات الحضور');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على الطلبات المعلقة للمدير
  Future<Map<String, dynamic>> getPendingApprovals({
    required int managerId,
  }) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Manager/GetPendingApprovals/$managerId'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
          };
        }
      } else {
        throw Exception('فشل في تحميل الطلبات المعلقة');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }
}