class LeaveType {
  final int id;
  final String name;
  final String description;
  final int? maxConsecutiveDays;
  final bool requiresApproval;
  final bool isActive;
  final bool deductFromBalance;
  final int? defaultBalance;
  
  LeaveType({
    required this.id,
    required this.name,
    required this.description,
    this.maxConsecutiveDays,
    required this.requiresApproval,
    required this.isActive,
    required this.deductFromBalance,
    this.defaultBalance,
  });
  
  factory LeaveType.fromJson(Map<String, dynamic> json) {
    return LeaveType(
      id: json['id'],
      name: json['name'],
      description: json['description'] ?? '',
      maxConsecutiveDays: json['maxConsecutiveDays'],
      requiresApproval: json['requiresApproval'] ?? true,
      isActive: json['isActive'] ?? true,
      deductFromBalance: json['deductFromBalance'] ?? true,
      defaultBalance: json['defaultBalance'],
    );
  }
}

class LeaveBalance {
  final int leaveTypeId;
  final String leaveTypeName;
  final int totalBalance;
  final int usedBalance;
  final int remainingBalance;
  final double percentageUsed;
  
  LeaveBalance({
    required this.leaveTypeId,
    required this.leaveTypeName,
    required this.totalBalance,
    required this.usedBalance,
    required this.remainingBalance,
    required this.percentageUsed,
  });
  
  factory LeaveBalance.fromJson(Map<String, dynamic> json) {
    return LeaveBalance(
      leaveTypeId: json['leaveTypeId'],
      leaveTypeName: json['leaveTypeName'],
      totalBalance: json['totalBalance'],
      usedBalance: json['usedBalance'],
      remainingBalance: json['remainingBalance'],
      percentageUsed: json['percentageUsed'] ?? 0.0,
    );
  }
}

class Manager {
  final int id;
  final String fullName;
  final String departmentName;
  final String jobTitleName;
  final String email;
  final String phone;
  
  Manager({
    required this.id,
    required this.fullName,
    required this.departmentName,
    required this.jobTitleName,
    required this.email,
    required this.phone,
  });
  
  factory Manager.fromJson(Map<String, dynamic> json) {
    return Manager(
      id: json['id'],
      fullName: json['fullName'],
      departmentName: json['departmentName'],
      jobTitleName: json['jobTitleName'],
      email: json['email'] ?? '',
      phone: json['phone'] ?? '',
    );
  }
}

class HolidayRequest {
  final int employeeId;
  final int leaveTypeId;
  final DateTime startDate;
  final DateTime endDate;
  final int duration;
  final String reason;
  final int? approvingManagerId;
  final bool saveAsDraft;
  
  HolidayRequest({
    required this.employeeId,
    required this.leaveTypeId,
    required this.startDate,
    required this.endDate,
    required this.duration,
    required this.reason,
    this.approvingManagerId,
    this.saveAsDraft = false,
  });
  
  Map<String, dynamic> toJson() {
    return {
      'employeeId': employeeId,
      'leaveTypeId': leaveTypeId,
      'startDate': startDate.toIso8601String(),
      'endDate': endDate.toIso8601String(),
      'duration': duration,
      'reason': reason,
      'approvingManagerId': approvingManagerId,
      'saveAsDraft': saveAsDraft,
    };
  }
}

class HolidayRequestResponse {
  final int requestId;
  final String requestNumber;
  final DateTime requestDate;
  final String status;
  final String statusCode;
  final String message;
  final int? approvalManagerId;
  final String? approvalManagerName;
  
  HolidayRequestResponse({
    required this.requestId,
    required this.requestNumber,
    required this.requestDate,
    required this.status,
    required this.statusCode,
    required this.message,
    this.approvalManagerId,
    this.approvalManagerName,
  });
  
  factory HolidayRequestResponse.fromJson(Map<String, dynamic> json) {
    return HolidayRequestResponse(
      requestId: json['requestId'],
      requestNumber: json['requestNumber'],
      requestDate: DateTime.parse(json['requestDate']),
      status: json['status'],
      statusCode: json['statusCode'],
      message: json['message'],
      approvalManagerId: json['approvalManagerId'],
      approvalManagerName: json['approvalManagerName'],
    );
  }
}

class DateConflict {
  final DateTime conflictStartDate;
  final DateTime conflictEndDate;
  final String leaveTypeName;
  final String status;
  
  DateConflict({
    required this.conflictStartDate,
    required this.conflictEndDate,
    required this.leaveTypeName,
    required this.status,
  });
  
  factory DateConflict.fromJson(Map<String, dynamic> json) {
    return DateConflict(
      conflictStartDate: DateTime.parse(json['conflictStartDate']),
      conflictEndDate: DateTime.parse(json['conflictEndDate']),
      leaveTypeName: json['leaveTypeName'],
      status: json['status'],
    );
  }
}