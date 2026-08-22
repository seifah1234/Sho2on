import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class HolidayService {
  static const String baseUrl = ApiConfig.baseUrl;

  // في HolidayService.dart - إضافة الدوال التالية:

Future<Map<String, dynamic>> getManagerHolidaysByStatus({
  required int managerId,
  required String status,
  String? searchTerm,
  DateTime? fromDate,
  DateTime? toDate,
  int pageNumber = 1,
  int pageSize = 20,
}) async {
  try {
    String endpoint;
    
    // اختيار الـ endpoint المناسب حسب الحالة
    switch (status.toLowerCase()) {
      case 'pending':
        endpoint = 'GetPendingRequestsForManager';
        break;
      case 'approved':
        endpoint = 'GetApprovedRequestsForManager';
        break;
      case 'rejected':
        endpoint = 'GetRejectedRequestsForManager';
        break;
      default:
        endpoint = 'GetPendingRequestsForManager';
    }
    
    String url = '$baseUrl/HolidayRequests/$endpoint/$managerId?'
        'pageNumber=$pageNumber&pageSize=$pageSize';

    if (searchTerm != null && searchTerm.isNotEmpty) {
      url += '&searchTerm=$searchTerm';
    }
    if (fromDate != null) {
      url += '&fromDate=${fromDate.toIso8601String()}';
    }
    if (toDate != null) {
      url += '&toDate=${toDate.toIso8601String()}';
    }

    final response = await http.get(
      Uri.parse(url),
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
          'totalRecords': data['totalRecords'] ?? 0,
        };
      } else {
        return {
          'success': false,
          'message': data['message'],
        };
      }
    } else {
      throw Exception('فشل في تحميل طلبات الإجازة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> approveHoliday(int requestId) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/HolidayRequests/ApproveHoliday/$requestId'),
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
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'],
          'errors': data['errors'],
        };
      }
    } else {
      throw Exception('فشل في الموافقة على طلب الإجازة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> rejectHoliday(int requestId, String reason) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/HolidayRequests/RejectHoliday/$requestId'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer YOUR_TOKEN',
      },
      body: json.encode({'reason': reason}),
    );

    if (response.statusCode == 200) {
      final data = json.decode(response.body);
      if (data['success']) {
        return {
          'success': true,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'],
          'errors': data['errors'],
        };
      }
    } else {
      throw Exception('فشل في رفض طلب الإجازة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> getManagerHolidayStats({
  required int managerId,
  DateTime? fromDate,
  DateTime? toDate,
}) async {
  try {
    String url = '$baseUrl/HolidayRequests/GetManagerHolidayStats/$managerId';

    final uri = Uri.parse(url).replace(
      queryParameters: {
        if (fromDate != null) 'fromDate': fromDate.toIso8601String(),
        if (toDate != null) 'toDate': toDate.toIso8601String(),
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
      throw Exception('فشل في تحميل الإحصائيات');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

  // في HolidayService.dart، تأكد من وجود الدالة:
Future<Map<String, dynamic>> getEmployeeRequests(
  int employeeId, {
  int? status,
  DateTime? fromDate,
  DateTime? toDate,
  int pageNumber = 1,
  int pageSize = 20,
}) async {
  try {
    // بناء URL مع معاملات التصفية
    String url = '$baseUrl/HolidayRequests/GetEmployeeRequests/$employeeId?'
        'pageNumber=$pageNumber&pageSize=$pageSize';
    
    if (status != null) {
      url += '&status=$status';
    }
    
    if (fromDate != null) {
      url += '&fromDate=${fromDate.toIso8601String()}';
    }
    
    if (toDate != null) {
      url += '&toDate=${toDate.toIso8601String()}';
    }
    
    final response = await http.get(
      Uri.parse(url),
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
      throw Exception('فشل في تحميل طلبات الإجازة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

  Future<Map<String, dynamic>> getLeaveTypes() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/HolidayRequests/GetLeaveTypes'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN', // أضف التوثيق
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
        throw Exception('فشل في تحميل أنواع الإجازات');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  Future<Map<String, dynamic>> getLeaveBalance(int employeeId, int leaveTypeId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/HolidayRequests/GetLeaveBalance/$employeeId/$leaveTypeId'),
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
        throw Exception('فشل في تحميل رصيد الإجازة');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  Future<Map<String, dynamic>> getManagers({int? jobTitleId}) async {
    try {
      String url = '$baseUrl/HolidayRequests/GetManagers';
      if (jobTitleId != null) {
        url += '?jobTitleId=$jobTitleId';
      }

      final response = await http.get(
        Uri.parse(url),
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
        throw Exception('فشل في تحميل المديرين');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  Future<Map<String, dynamic>> checkDateConflicts(
    int employeeId, 
    DateTime startDate, 
    DateTime endDate
  ) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/CheckDateConflicts'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'startDate': startDate.toIso8601String(),
          'endDate': endDate.toIso8601String(),
        }),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'hasConflicts': data['data']['hasConflicts'],
          'conflicts': data['data']['conflicts'],
          'message': data['message'],
        };
      } else {
        throw Exception('فشل في التحقق من التعارض');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  Future<Map<String, dynamic>> submitHolidayRequest({
    required int employeeId,
    required int leaveTypeId,
    required DateTime startDate,
    required DateTime endDate,
    required int duration,
    required String reason,
    int? approvingManagerId,
    bool saveAsDraft = false,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/SubmitRequest'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'leaveTypeId': leaveTypeId,
          'startDate': startDate.toIso8601String(),
          'endDate': endDate.toIso8601String(),
          'duration': duration,
          'reason': reason,
          'approvingManagerId': approvingManagerId,
          'saveAsDraft': saveAsDraft,
        }),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        if (data['success']) {
          return {
            'success': true,
            'data': data['data'],
            'message': data['message'],
          };
        } else {
          return {
            'success': false,
            'message': data['message'],
            'errors': data['errors'],
          };
        }
      } else {
        throw Exception('فشل في تقديم الطلب');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

    
}