import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class HolidayService {
  static const String baseUrl = ApiConfig.baseUrl;

  // الحصول على الهيدرز مع التوكن
  Map<String, String> _getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer YOUR_TOKEN',
    };
  }

  // البحث عن الموظفين
  Future<Map<String, dynamic>> searchEmployees({
    String? searchTerm,
    int? departmentId,
    int? jobTitleId,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      final queryParams = <String, String>{
        'pageNumber': pageNumber.toString(),
        'pageSize': pageSize.toString(),
      };

      if (searchTerm != null && searchTerm.isNotEmpty) {
        queryParams['searchTerm'] = searchTerm;
      }
      if (departmentId != null && departmentId > 0) {
        queryParams['departmentId'] = departmentId.toString();
      }
      if (jobTitleId != null && jobTitleId > 0) {
        queryParams['jobTitleId'] = jobTitleId.toString();
      }

      final uri = Uri.parse('$baseUrl/HolidayRequests/SearchEmployees')
          .replace(queryParameters: queryParams);

      final response = await http.get(uri, headers: _getHeaders());

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل في البحث عن الموظفين',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على أنواع الإجازات
  Future<Map<String, dynamic>> getLeaveTypes() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/HolidayRequests/GetLeaveTypes'),
        headers: _getHeaders(),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل في تحميل أنواع الإجازات',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على رصيد الإجازة
  Future<Map<String, dynamic>> getLeaveBalance(int employeeId, int leaveTypeId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/HolidayRequests/GetLeaveBalance/$employeeId/$leaveTypeId'),
        headers: _getHeaders(),
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        final data = json.decode(response.body);
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في تحميل رصيد الإجازة',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على المديرين
  Future<Map<String, dynamic>> getManagers({int? jobTitleId, int? departmentId}) async {
    try {
      final queryParams = <String, String>{};

      if (jobTitleId != null && jobTitleId > 0) {
        queryParams['jobTitleId'] = jobTitleId.toString();
      }
      if (departmentId != null && departmentId > 0) {
        queryParams['departmentId'] = departmentId.toString();
      }

      final uri = Uri.parse('$baseUrl/HolidayRequests/GetManagers')
          .replace(queryParameters: queryParams.isEmpty ? null : queryParams);

      final response = await http.get(uri, headers: _getHeaders());

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل في تحميل المديرين',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // التحقق من تعارض التواريخ
  Future<Map<String, dynamic>> checkDateConflicts(
    int employeeId, 
    DateTime startDate, 
    DateTime endDate
  ) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/CheckDateConflicts'),
        headers: _getHeaders(),
        body: json.encode({
          'employeeId': employeeId,
          'startDate': startDate.toIso8601String(),
          'endDate': endDate.toIso8601String(),
        }),
      );

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'hasConflicts': data['data']['hasConflicts'] ?? false,
          'conflicts': data['data']['conflicts'] ?? [],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في التحقق من التعارض',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // تقديم طلب إجازة
  Future<Map<String, dynamic>> submitHolidayRequest({
    required int employeeId,
    required int leaveTypeId,
    required DateTime startDate,
    required DateTime endDate,
    required int duration,
    required String reason,
    String? notes,
    int? approvingManagerId,
    int? replacementUserId,
    bool saveAsDraft = false,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/SubmitRequest'),
        headers: _getHeaders(),
        body: json.encode({
          'employeeId': employeeId,
          'leaveTypeId': leaveTypeId,
          'startDate': startDate.toIso8601String(),
          'endDate': endDate.toIso8601String(),
          'duration': duration,
          'reason': reason,
          'notes': notes,
          'approvingManagerId': approvingManagerId,
          'replacementUserId': replacementUserId,
          'saveAsDraft': saveAsDraft,
        }),
      );

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في تقديم الطلب',
          'errors': data['errors'],
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على طلبات الموظف
  Future<Map<String, dynamic>> getEmployeeRequests(
    int employeeId, {
    int? status,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      final queryParams = <String, String>{
        'pageNumber': pageNumber.toString(),
        'pageSize': pageSize.toString(),
      };

      if (status != null) {
        queryParams['status'] = status.toString();
      }
      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/HolidayRequests/GetEmployeeRequests/$employeeId')
          .replace(queryParameters: queryParams);

      final response = await http.get(uri, headers: _getHeaders());

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': data['success'] ?? false,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': 'فشل في تحميل طلبات الإجازة',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على طلبات المدير حسب الحالة
  Future<Map<String, dynamic>> getManagerHolidaysByStatus({
    required int managerId,
    required String status, // pending, approved, rejected
    String? searchTerm,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      String endpoint;
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

      final queryParams = <String, String>{
        'pageNumber': pageNumber.toString(),
        'pageSize': pageSize.toString(),
      };

      if (searchTerm != null && searchTerm.isNotEmpty) {
        queryParams['searchTerm'] = searchTerm;
      }
      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/HolidayRequests/$endpoint/$managerId')
          .replace(queryParameters: queryParams);

      final response = await http.get(uri, headers: _getHeaders());

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'data': data['data'],
          'totalRecords': data['totalRecords'] ?? 0,
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في تحميل طلبات الإجازة',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الموافقة على طلب إجازة
  Future<Map<String, dynamic>> approveHoliday(int requestId) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/ApproveHoliday/$requestId'),
        headers: _getHeaders(),
      );

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في الموافقة على طلب الإجازة',
          'errors': data['errors'],
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // رفض طلب إجازة
  Future<Map<String, dynamic>> rejectHoliday(int requestId, String reason) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/HolidayRequests/RejectHoliday/$requestId'),
        headers: _getHeaders(),
        body: json.encode({'reason': reason}),
      );

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في رفض طلب الإجازة',
          'errors': data['errors'],
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على إحصائيات المدير
  Future<Map<String, dynamic>> getManagerHolidayStats({
    required int managerId,
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    try {
      final queryParams = <String, String>{};

      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/HolidayRequests/GetManagerHolidayStats/$managerId')
          .replace(queryParameters: queryParams.isEmpty ? null : queryParams);

      final response = await http.get(uri, headers: _getHeaders());

      final data = json.decode(response.body);
      if (response.statusCode == 200 && data['success'] == true) {
        return {
          'success': true,
          'data': data['data'],
          'message': data['message'],
        };
      } else {
        return {
          'success': false,
          'message': data['message'] ?? 'فشل في تحميل الإحصائيات',
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