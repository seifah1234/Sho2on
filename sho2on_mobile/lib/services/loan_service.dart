import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:sho2on_mobile/services/api_config.dart';

class LoanService {
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

      final uri = Uri.parse('$baseUrl/Loans/SearchEmployees')
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

  // الحصول على بيانات الموظف
  Future<Map<String, dynamic>> getEmployee(int employeeId) async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Loans/GetEmployee/$employeeId'),
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
          'message': data['message'] ?? 'فشل في تحميل بيانات الموظف',
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
  Future<Map<String, dynamic>> getManagers() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl/Loans/GetManagers'),
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

  // حساب القسط
  Future<Map<String, dynamic>> calculateInstallment({
    required int employeeId,
    required double loanAmount,
    required int installmentMonths,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/CalculateInstallment'),
        headers: _getHeaders(),
        body: json.encode({
          'employeeId': employeeId,
          'loanAmount': loanAmount,
          'installmentMonths': installmentMonths,
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
          'message': data['message'] ?? 'فشل في حساب القسط',
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

  // تقديم طلب سلفة
  Future<Map<String, dynamic>> submitLoanRequest({
    required int employeeId,
    required double loanAmount,
    required DateTime loanDate,
    required DateTime expectedPaybackDate,
    required int installmentMonths,
    required String reason,
    String? notes,
    required int approvingManagerId,
  }) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/SubmitRequest'),
        headers: _getHeaders(),
        body: json.encode({
          'employeeId': employeeId,
          'loanAmount': loanAmount,
          'loanDate': loanDate.toIso8601String(),
          'expectedPaybackDate': expectedPaybackDate.toIso8601String(),
          'installmentMonths': installmentMonths,
          'reason': reason,
          'notes': notes,
          'approvingManagerId': approvingManagerId,
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
      final queryParams = <String, String>{
        'pageNumber': pageNumber.toString(),
        'pageSize': pageSize.toString(),
      };

      if (status != null && status.isNotEmpty) {
        queryParams['status'] = status;
      }
      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/Loans/GetEmployeeLoans/$employeeId')
          .replace(queryParameters: queryParams);

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
          'message': data['message'] ?? 'فشل في تحميل سجل السلف',
        };
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
          'message': data['message'] ?? 'فشل في تحميل تفاصيل السلفة',
        };
      }
    } catch (e) {
      return {
        'success': false,
        'message': 'خطأ: $e',
      };
    }
  }

  // الحصول على طلبات السلف للمدير حسب الحالة
  Future<Map<String, dynamic>> getManagerLoansByStatus({
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

      final uri = Uri.parse('$baseUrl/Loans/$endpoint/$managerId')
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
          'message': data['message'] ?? 'فشل في تحميل طلبات السلف',
        };
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
      final queryParams = <String, String>{};

      if (status != null && status.isNotEmpty) {
        queryParams['status'] = status;
      }
      if (fromDate != null) {
        queryParams['fromDate'] = fromDate.toIso8601String();
      }
      if (toDate != null) {
        queryParams['toDate'] = toDate.toIso8601String();
      }

      final uri = Uri.parse('$baseUrl/Loans/GetAllManagerLoans/$managerId')
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

  // الموافقة على سلفة
  Future<Map<String, dynamic>> approveLoan(int loanId) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/ApproveLoan/$loanId'),
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
          'message': data['message'] ?? 'فشل في الموافقة على السلفة',
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

  // رفض سلفة
  Future<Map<String, dynamic>> rejectLoan(int loanId, String reason) async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl/Loans/RejectLoan/$loanId'),
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
          'message': data['message'] ?? 'فشل في رفض السلفة',
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
}