import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class AttendanceService {
  // دالة لتحميل التقرير الشهري
 Future<Map<String, dynamic>> getMonthlyReport({
  required int userId,
  required int year,
  required int month,
}) async {
  try {
    final url = Uri.parse("${ApiConfig.baseUrl}/AttendanceReport/Monthly/$userId/$year/$month");
    
    print('Fetching monthly report from: $url');
    
    final response = await http.get(
      url,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ).timeout(Duration(seconds: 30));
    
    print('Response status: ${response.statusCode}');
    
    if (response.statusCode == 200) {
      final data = json.decode(response.body);
      print('Response data type: ${data.runtimeType}');
      print('Response data: $data');
      
      // تحقق من هيكل البيانات
      if (data is Map<String, dynamic>) {
        if (data.containsKey('success') && data['success'] == true) {
          // تحقق مما إذا كانت البيانات قائمة أو خريطة
          dynamic responseData = data['data'];
          
          if (responseData is List) {
            return {
              'success': true,
              'data': responseData,
              'message': data['message'] ?? '',
            };
          } else if (responseData is Map) {
            // إذا كانت خريطة، حولها إلى قائمة
            return {
              'success': true,
              'data': [responseData], // ضعها داخل قائمة
              'message': data['message'] ?? '',
            };
          } else {
            // إذا كانت null أو نوع آخر
            return {
              'success': true,
              'data': [],
              'message': data['message'] ?? '',
            };
          }
        } else {
          return {
            'success': false,
            'message': data['message'] ?? 'فشل في تحميل التقرير',
          };
        }
      } else if (data is List) {
        // إذا كانت الاستجابة مباشرة قائمة
        return {
          'success': true,
          'data': data,
          'message': 'تم تحميل التقرير بنجاح',
        };
      } else {
        return {
          'success': false,
          'message': 'هيكل البيانات غير متوقع',
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
  // دالة مساعدة للحصول على التوكن (تعدلها حسب نظام المصادقة لديك)
  Future<String?> _getToken() async {
    // TODO: استرجع التوكن من SharedPreferences أو مكان تخزين آخر
    return null;
  }

  // بقية الدوال كما هي...
  Future<bool> checkIn({
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

  Future<bool> checkOut({
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

  Future<bool> _record({
    required int userId,
    required int branchId,
    required int status,
    double? lat,
    double? lon,
    String? locationName,
  }) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/attendance/record");

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

    return res.statusCode == 200;
  }

  Future<Map<String, dynamic>?> getToday(int userId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/attendance/today/$userId");

    final res = await http.get(url);
    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    }
    return null;
  }

  Future<List<dynamic>> getFingerprints(int userId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/attendance/fingerprints/today/$userId");
    final res = await http.get(url);
    if (res.statusCode == 200) {
      return jsonDecode(res.body);
    }
    return [];
  }

  Future<bool> deleteLast(int userId) async {
    final url = Uri.parse("${ApiConfig.baseUrl}/attendance/fingerprint/last/$userId");

    final res = await http.delete(url);
    return res.statusCode == 200;
  }
}