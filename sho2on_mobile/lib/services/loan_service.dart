import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class LoanService {
  static const String baseUrl = ApiConfig.baseUrl;

  // البحث عن الموظفين
  Future<Map<String, dynamic>> searchEmployees({
    String? searchTerm,
    int? departmentId,
    int? jobTitleId,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      String url = '$baseUrl/Loans/SearchEmployees?'
          'pageNumber=$pageNumber&pageSize=$pageSize';

      if (searchTerm != null && searchTerm.isNotEmpty) {
        url += '&searchTerm=$searchTerm';
      }
      if (departmentId != null && departmentId > 0) {
        url += '&departmentId=$departmentId';
      }
      if (jobTitleId != null && jobTitleId > 0) {
        url += '&jobTitleId=$jobTitleId';
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
        throw Exception('فشل في البحث عن الموظفين');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على بيانات الموظف
  Future<Map<String, dynamic>> getEmployee(int employeeId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Loans/GetEmployee/$employeeId'),
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
        throw Exception('فشل في تحميل بيانات الموظف');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على المديرين
  Future<Map<String, dynamic>> getManagers() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Loans/GetManagers'),
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

  // حساب القسط
  Future<Map<String, dynamic>> calculateInstallment({
    required int employeeId,
    required double loanAmount,
    required int installmentMonths,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/CalculateInstallment'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'loanAmount': loanAmount,
          'installmentMonths': installmentMonths,
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
        throw Exception('فشل في حساب القسط');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // تقديم طلب سلفة
  Future<Map<String, dynamic>> submitLoanRequest({
    required int employeeId,
    required double loanAmount,
    required DateTime loanDate,
    required DateTime expectedPaybackDate,
    required int installmentMonths,
    required String reason,
    required int approvingManagerId,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/SubmitRequest'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer YOUR_TOKEN',
        },
        body: json.encode({
          'employeeId': employeeId,
          'loanAmount': loanAmount,
          'loanDate': loanDate.toIso8601String(),
          'expectedPaybackDate': expectedPaybackDate.toIso8601String(),
          'installmentMonths': installmentMonths,
          'reason': reason,
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

  // الحصول على سجل السلف للموظف
  Future<Map<String, dynamic>> getEmployeeLoans(
    int employeeId, {
    String? status,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber = 1,
    int pageSize = 20,
  }) async {
    try {
      String url = '$baseUrl/Loans/GetEmployeeLoans/$employeeId?'
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
        throw Exception('فشل في تحميل سجل السلف');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على تفاصيل سلفة معينة
  Future<Map<String, dynamic>> getLoanDetails(int loanId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Loans/GetLoanDetails/$loanId'),
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
        throw Exception('فشل في تحميل تفاصيل السلفة');
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // دالة عامة للحصول على طلبات المدير حسب الحالة
Future<Map<String, dynamic>> getManagerLoansByStatus({
  required int managerId,
  required String status, // Pending, Approved, Rejected
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
        endpoint = 'GetPendingLoansForManager';
        break;
      case 'approved':
        endpoint = 'GetApprovedLoansForManager';
        break;
      case 'rejected':
        endpoint = 'GetRejectedLoansForManager';
        break;
      default:
        endpoint = 'GetPendingLoansForManager';
    }
    
    String url = '$baseUrl/Loans/$endpoint/$managerId?'
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
      throw Exception('فشل في تحميل طلبات السلف');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

  // الحصول على طلبات السلف للمدير
Future<Map<dynamic, dynamic>> getPendingLoansForManager({
  required int managerId,
  String? searchTerm,
  String? status,
  DateTime? fromDate,
  DateTime? toDate,
  int pageNumber = 1,
  int pageSize = 20,
}) async {
  try {
    String url = '$baseUrl/Loans/GetPendingLoansForManager/$managerId?'
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
    if (status != null && status.isNotEmpty) {
      url += '&status=$status';
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
      throw Exception('فشل في تحميل طلبات السلف');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

// الحصول على إحصائيات السلف للمدير
Future<Map<String, dynamic>> getManagerLoanStats({
  required int managerId,
  String? status,
  DateTime? fromDate,
  DateTime? toDate,
}) async {
  try {
    String url = '$baseUrl/Loans/GetAllManagerLoans/$managerId';

    final uri = Uri.parse(url).replace(
      queryParameters: {
        if (status != null && status.isNotEmpty) 'status': status,
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

// الموافقة على سلفة
Future<Map<String, dynamic>> approveLoan(int loanId) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/Loans/ApproveLoan/$loanId'),
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
      throw Exception('فشل في الموافقة على السلفة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}

// رفض سلفة
Future<Map<String, dynamic>> rejectLoan(int loanId, String reason) async {
  try {
    final response = await http.post(
      Uri.parse('$baseUrl/Loans/RejectLoan/$loanId'),
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
      throw Exception('فشل في رفض السلفة');
    }
  } catch (e) {
    return {
      'success': false,
      'message': 'خطأ: $e',
    };
  }
}
}