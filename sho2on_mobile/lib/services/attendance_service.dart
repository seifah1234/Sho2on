import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class AttendanceService {
  static const String baseUrl = ApiConfig.baseUrl;

  // الحصول على الهيدرز مع التوكن
  Map<String, String> _getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN',
    };
  }

  // تحميل التقرير الشهري - مطابق للويب
  Future<Map<String, dynamic>> getMonthlyReport({
    required int userId,
    required int year,
    required int month,
  }) async {
    try {
      final url = Uri.parse('$baseUrl/AttendanceReport/Monthly/$userId/$year/$month');
      
      print('Fetching monthly report from: $url');
      
      final response = await http.get(
        url,
        headers: _getHeaders(),
      ).timeout(Duration(seconds: 30));
      
      print('Response status: ${response.statusCode}');
      
      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        
        // التحقق من نجاح الاستجابة
        if (data['success'] == true) {
          return {
            'success': true,
            'data': data['data'],
            'message': data['message'] ?? '',
          };
        } else {
          return {
            'success': false,
            'message': data['message'] ?? 'فشل في تحميل التقرير',
          };
        }
      } else {
        print('API Error: ${response.statusCode} - ${response.body}');
        return {
          'success': false,
          'message': 'خطأ في الخادم (${response.statusCode})',
        };
      }
    } catch (e) {
      print('Exception in getMonthlyReport: $e');
      return {
        'success': false,
        'message': 'خطأ في الاتصال: $e',
      };
    }
  }

  // حفظ تعديلات اليوم
  Future<Map<String, dynamic>> saveDay({
    required int userId,
    required DateTime date,
    String? checkIn,
    String? checkOut,
    bool isAbsence = false,
    bool isHoliday = false,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/AttendanceReport/SaveDay'),
        headers: _getHeaders(),
        body: json.encode({
          'userId': userId,
          'date': date.toIso8601String(),
          'checkIn': checkIn,
          'checkOut': checkOut,
          'isAbsence': isAbsence,
          'isHoliday': isHoliday,
        }),
      );

      final data = json.decode(response.body);
      return {
        'success': data['success'] ?? false,
        'message': data['message'] ?? '',
      };
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // تحميل بيانات البريك للشهر
  Future<Map<String, dynamic>> getBreakReport({
    required int userId,
    required int month,
    required int year,
  }) async {
    try {
      final url = Uri.parse('$baseUrl/Break/GetBreakReport/$userId/$month/$year');
      
      final response = await http.get(url, headers: _getHeaders());
      
      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'] ?? '',
        };
      } else {
        return {
          'success': false,
          'message': 'فشل في تحميل بيانات البريك',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // تغيير الوردية
  Future<Map<String, dynamic>> changeShift({
    required int userId,
    required DateTime date,
    required int shiftId,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/AttendanceReport/ChangeShift'),
        headers: _getHeaders(),
        body: json.encode({
          'userId': userId,
          'date': date.toIso8601String(),
          'shiftId': shiftId,
        }),
      );

      final data = json.decode(response.body);
      return {
        'success': data['success'] ?? false,
        'message': data['message'] ?? '',
      };
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // باقي الدوال كما هي...
  Future<Map<String, dynamic>> checkIn({
    required int userId,
    required int branchId,
    double? lat,
    double? lon,
    String? locationName,
  }) async {
    return _record(
      userId: userId,
      branchId: branchId,
      status: 1,
      lat: lat,
      lon: lon,
      locationName: locationName,
    );
  }

  Future<Map<String, dynamic>> checkOut({
    required int userId,
    required int branchId,
    double? lat,
    double? lon,
    String? locationName,
  }) async {
    return _record(
      userId: userId,
      branchId: branchId,
      status: 0,
      lat: lat,
      lon: lon,
      locationName: locationName,
    );
  }

  Future<Map<String, dynamic>> _record({
  required int userId,
  required int branchId,
  required int status,
  double? lat,
  double? lon,
  String? locationName,
}) async {
  final url = Uri.parse('$baseUrl/attendance/record');

  final body = {
    "userId": userId,
    "branchId": branchId,
    "status": status,
    "latitude": lat,
    "longitude": lon,
    "locationName": locationName,
    "deviceTime": DateTime.now().toIso8601String(),
  };

  final res = await http.post(
    url,
    headers: {"Content-Type": "application/json"},
    body: jsonEncode(body),
  );

  if (res.statusCode == 200) {
    final data = jsonDecode(res.body);
    final message = data['message'] ?? 'فشل التسجيل';
    return {
      'success': true,
      'message': message,
    };
  } else {
    try {
      final data = jsonDecode(res.body);
      final message = data['message'] ?? 'فشل التسجيل';
      return {
        'success': false,
        'message': message,
      };
      print('Location error: $message');
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
    return {
      'success': false,
      'message': 'خطأ',
    };
  }
}

  Future<Map<String, dynamic>?> getToday(int userId) async {
    final url = Uri.parse('$baseUrl/attendance/today/$userId');

    final res = await http.get(url);
    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    }
    return null;
  }

  Future<List<dynamic>> getFingerprints(int userId) async {
    final url = Uri.parse('$baseUrl/attendance/fingerprints/today/$userId');
    final res = await http.get(url);
    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    }
    return [];
  }
}