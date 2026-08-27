import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class BreakService {
  static const String baseUrl = ApiConfig.baseUrl;

  Map<String, String> _headers() => {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      };

  Future<Map<String, dynamic>> getMyBreakType(int userId) async {
    final url = Uri.parse('$baseUrl/Break/my-break-type/$userId');
    final res = await http.get(url, headers: _headers()).timeout(const Duration(seconds: 20));
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل تحميل بيانات نظام الاستراحة');
  }

  Future<Map<String, dynamic>> getActiveBreak(int userId) async {
    final url = Uri.parse('$baseUrl/Break/active/$userId');
    final res = await http.get(url, headers: _headers()).timeout(const Duration(seconds: 20));
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل التحقق من حالة الاستراحة');
  }

  Future<Map<String, dynamic>> startBreak(int userId) async {
    final url = Uri.parse('$baseUrl/Break/start');
    final res = await http
        .post(url, headers: _headers(), body: jsonEncode({'userId': userId}))
        .timeout(const Duration(seconds: 20));
    final data = jsonDecode(res.body);
    if (res.statusCode == 200) return data;
    throw Exception(data is String ? data : (data['message'] ?? 'فشل بدء الاستراحة'));
  }

  Future<Map<String, dynamic>> endBreak(int userId) async {
    final url = Uri.parse('$baseUrl/Break/end/$userId');
    final res = await http.post(url, headers: _headers()).timeout(const Duration(seconds: 20));
    final data = jsonDecode(res.body);
    if (res.statusCode == 200) return data;
    throw Exception(data is String ? data : (data['message'] ?? 'فشل إنهاء الاستراحة'));
  }

  Future<List<dynamic>> getTodayBreaks(int userId) async {
    final url = Uri.parse('$baseUrl/Break/today/$userId');
    final res = await http.get(url, headers: _headers()).timeout(const Duration(seconds: 20));
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل تحميل سجل استراحات اليوم');
  }
}