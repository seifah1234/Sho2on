// services/chat_history_service.dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'api_config.dart';

class ChatHistoryService {
  Future<List<dynamic>> getConversations(int userId) async {
    final res = await http.get(Uri.parse('${ApiConfig.baseUrl}/Chat/conversations/$userId'));
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل تحميل المحادثات');
  }

  Future<List<dynamic>> getDirectMessages(int currentUserId, int otherUserId) async {
    final res = await http.get(Uri.parse('${ApiConfig.baseUrl}/Chat/messages/$currentUserId/$otherUserId'));
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل تحميل الرسائل');
  }

  // إضافة دالة البحث عن مستخدمين
  Future<List<dynamic>> searchUsers(String searchTerm) async {
    final res = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Chat/SearchUsers?searchTerm=$searchTerm'),
    );
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل البحث عن المستخدمين');
  }

  // إضافة دالة الحصول على جميع المستخدمين
  Future<List<dynamic>> getAllUsers() async {
    final res = await http.get(
      Uri.parse('${ApiConfig.baseUrl}/Chat/GetAllUsers'),
    );
    if (res.statusCode == 200) return jsonDecode(res.body);
    throw Exception('فشل تحميل المستخدمين');
  }
}