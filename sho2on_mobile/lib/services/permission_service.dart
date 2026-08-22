import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class PermissionService {
  static const String baseUrl = ApiConfig.baseUrl;

  // الحصول على أنواع الإذن
  Future<Map<String, dynamic>> getPermissionTypes() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Permissions/GetPermissionTypes'),
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
        throw Exception('فشل في تحميل أنواع الإذن');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // التحقق من تعارض الوقت
  Future<Map<String, dynamic>> checkTimeConflict({
    required int employeeId,
    required DateTime startDateTime,
    required DateTime endDateTime,
  }) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Permissions/CheckTimeConflict?'
            'employeeId=$employeeId&'
            'startDateTime=${startDateTime.toIso8601String()}&'
            'endDateTime=${endDateTime.toIso8601String()}'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        return {
          'success': true,
          'data': data['data'],
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

  // في PermissionService.dart - إضافة الدوال التالية:

Future<Map<String, dynamic>> getManagerPermissionsByStatus({
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
        endpoint = 'GetPendingPermissionsForManager';
        break;
      case 'approved':
        endpoint = 'GetApprovedPermissionsForManager';
        break;
      case 'rejected':
        endpoint = 'GetRejectedPermissionsForManager';
        break;
      default:
        endpoint = 'GetPendingPermissionsForManager';
    }
    
    String url = '$baseUrl/Permissions/$endpoint/$managerId?'
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
      throw Exception('فشل في تحميل طلبات الإذن');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> approvePermission(int permissionId) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/Permissions/ApprovePermission/$permissionId'),
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
      throw Exception('فشل في الموافقة على الإذن');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> rejectPermission(int permissionId, String reason) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/Permissions/RejectPermission/$permissionId'),
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
      throw Exception('فشل في رفض الإذن');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

Future<Map<String, dynamic>> getManagerPermissionStats({
  required int managerId,
  DateTime? fromDate,
  DateTime? toDate,
}) async {
  try {
    String url = '$baseUrl/Permissions/GetManagerPermissionStats/$managerId';

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

  // حساب الخصم
  Future<Map<String, dynamic>> calculateDeduction({
    required int employeeId,
    required DateTime startDateTime,
    required DateTime endDateTime,
    required bool deductFromSalary,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Permissions/CalculateDeduction'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'startDateTime': startDateTime.toIso8601String(),
          'endDateTime': endDateTime.toIso8601String(),
          'deductFromSalary': deductFromSalary,
        }),
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
        throw Exception('فشل في حساب الخصم');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على المديرين للموافقة
  Future<Map<String, dynamic>> getManagersForApproval() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Permissions/GetManagersForApproval'),
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

  // تقديم طلب إذن
  Future<Map<String, dynamic>> submitPermissionRequest({
    required int employeeId,
    required int permissionTypeId,
    required DateTime startDateTime,
    required DateTime endDateTime,
    required String reason,
    required int approvingManagerId,
    String notes = '',
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Permissions/SubmitRequest'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'permissionTypeId': permissionTypeId,
          'startDateTime': startDateTime.toIso8601String(),
          'endDateTime': endDateTime.toIso8601String(),
          'reason': reason,
          'notes': notes,
          'approvingManagerId': approvingManagerId,
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

  // الحصول على سجل الإذن للموظف
  Future<Map<String, dynamic>> getEmployeePermissions(
    int employeeId, {
    String? status,
    DateTime? fromDate,
    DateTime? toDate,
    String? permissionType,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      String url = '$baseUrl/Permissions/GetEmployeePermissions/$employeeId?'
          'pageNumber=$pageNumber&pageSize=$pageSize';

      if (status != null && status.isNotEmpty) {
        url += '&status=$status';
      }
      if (fromDate != null) {
        url += '&fromDate=${fromDate.toIso8601String()}';
      }
      if (toDate != null) {
        url += '&toDate=${toDate.toIso8601String()}';
      }
      if (permissionType != null && permissionType.isNotEmpty) {
        url += '&permissionType=$permissionType';
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
        throw Exception('فشل في تحميل سجل الإذن');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على تفاصيل إذن معين
  Future<Map<String, dynamic>> getPermissionDetails(int permissionId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Permissions/GetPermissionDetails/$permissionId'),
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
        throw Exception('فشل في تحميل تفاصيل الإذن');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }
}