import 'package:flutter/material.dart';
import '../../services/permission_service.dart';

class ApprovePermissionsPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const ApprovePermissionsPage({super.key, required this.user});

  @override
  _ApprovePermissionsPageState createState() => _ApprovePermissionsPageState();
}

class _ApprovePermissionsPageState extends State<ApprovePermissionsPage> {
  final PermissionService _permissionService = PermissionService();
  
  bool _isLoading = false;
  bool _isRefreshing = false;
  
  List<dynamic> _pendingPermissions = [];
  List<dynamic> _approvedPermissions = [];
  List<dynamic> _rejectedPermissions = [];
  
  final String _searchTerm = '';
  String _selectedStatus = 'pending';
  DateTime? _fromDate;
  DateTime? _toDate;
  
  int _totalPending = 0;
  int _totalApproved = 0;
  int _totalRejected = 0;
  double _totalHoursPending = 0;
  double _totalHoursApproved = 0;
  double _totalHoursRejected = 0;
  double _totalDeductedAmount = 0;
  
  int _currentPage = 1;
  final int _pageSize = 10;
  int _totalRecords = 0;
  bool _hasMore = true;
  
  final Color primaryColor = Color(0xFF673AB7);
  final Color pendingColor = Color(0xFFFF9800);
  final Color approvedColor = Color(0xFF4CAF50);
  final Color rejectedColor = Color(0xFFF44336);
  final Color backgroundColor = Color(0xFFF5F7FA);
  
  @override
  void initState() {
    super.initState();
    if (widget.user['isManager'] ?? false) {
      _loadPermissions();
      _loadStatistics();
    }
  }
  
  Future<void> _loadPermissions() async {
    if (!(widget.user['isManager'] ?? false)) return;
    
    if (_currentPage == 1) {
      setState(() => _isLoading = true);
    }
    
    try {
      final result = await _permissionService.getManagerPermissionsByStatus(
        managerId: widget.user['id'],
        status: _selectedStatus,
        searchTerm: _searchTerm.isNotEmpty ? _searchTerm : null,
        fromDate: _fromDate,
        toDate: _toDate,
        pageNumber: _currentPage,
        pageSize: _pageSize,
      );
      
      if (result['success']) {
        final List<dynamic> newPermissions = result['data'] ?? [];
        _totalRecords = result['totalRecords'] ?? 0;
        
        setState(() {
          if (_currentPage == 1) {
            switch (_selectedStatus) {
              case 'pending':
                _pendingPermissions = newPermissions;
                break;
              case 'approved':
                _approvedPermissions = newPermissions;
                break;
              case 'rejected':
                _rejectedPermissions = newPermissions;
                break;
            }
          } else {
            switch (_selectedStatus) {
              case 'pending':
                _pendingPermissions.addAll(newPermissions);
                break;
              case 'approved':
                _approvedPermissions.addAll(newPermissions);
                break;
              case 'rejected':
                _rejectedPermissions.addAll(newPermissions);
                break;
            }
          }
          _hasMore = newPermissions.length == _pageSize;
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل طلبات الإذن');
      }
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  List<dynamic> _getCurrentPermissions() {
    switch (_selectedStatus) {
      case 'pending':
        return _pendingPermissions;
      case 'approved':
        return _approvedPermissions;
      case 'rejected':
        return _rejectedPermissions;
      default:
        return _pendingPermissions;
    }
  }
  
  Future<void> _loadStatistics() async {
    try {
      final result = await _permissionService.getManagerPermissionStats(
        managerId: widget.user['id'],
        fromDate: _fromDate,
        toDate: _toDate,
      );
      
      if (result['success']) {
        final stats = result['data'];
        setState(() {
          _totalPending = stats['totalPending'] ?? 0;
          _totalApproved = stats['totalApproved'] ?? 0;
          _totalRejected = stats['totalRejected'] ?? 0;
          _totalHoursPending = (stats['totalHoursPending'] ?? 0).toDouble();
          _totalHoursApproved = (stats['totalHoursApproved'] ?? 0).toDouble();
          _totalHoursRejected = (stats['totalHoursRejected'] ?? 0).toDouble();
          _totalDeductedAmount = (stats['totalAmountDeducted'] ?? 0).toDouble();
        });
      }
    } catch (e) {
      print('Error loading statistics: $e');
    }
  }
  
  Future<void> _approvePermission(int permissionId) async {
    try {
      final confirmed = await _showConfirmationDialog(
        'موافقة على طلب الإذن',
        'هل أنت متأكد من الموافقة على طلب الإذن؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _permissionService.approvePermission(permissionId);
      
      if (result['success']) {
        _currentPage = 1;
        await _loadPermissions();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تمت الموافقة على طلب الإذن بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في الموافقة على طلب الإذن');
      }
    } catch (e) {
      _showError('خطأ في الموافقة: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _rejectPermission(int permissionId) async {
    try {
      final reason = await _showRejectionDialog();
      if (reason == null || reason.isEmpty) return;
      
      final confirmed = await _showConfirmationDialog(
        'رفض طلب الإذن',
        'هل أنت متأكد من رفض طلب الإذن؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _permissionService.rejectPermission(permissionId, reason);
      
      if (result['success']) {
        _currentPage = 1;
        await _loadPermissions();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تم رفض طلب الإذن بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في رفض طلب الإذن');
      }
    } catch (e) {
      _showError('خطأ في الرفض: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<bool> _showConfirmationDialog(String title, String message) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Text(title),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: Text('إلغاء'),
            ),
            ElevatedButton(
              onPressed: () => Navigator.pop(context, true),
              style: ElevatedButton.styleFrom(
                backgroundColor: title.contains('رفض') ? rejectedColor : approvedColor,
              ),
              child: Text(title.contains('رفض') ? 'رفض' : 'موافقة'),
            ),
          ],
        ),
      ),
    );
    
    return result ?? false;
  }
  
  Future<String?> _showRejectionDialog() async {
    String reason = '';
    
    final result = await showDialog<String?>(
      context: context,
      builder: (context) => StatefulBuilder(
        builder: (context, setState) => Directionality(
          textDirection: TextDirection.rtl,
          child: AlertDialog(
            title: Text('سبب الرفض'),
            content: TextFormField(
              maxLines: 3,
              decoration: InputDecoration(
                hintText: 'أدخل سبب رفض طلب الإذن',
                border: OutlineInputBorder(),
              ),
              onChanged: (value) {
                setState(() => reason = value);
              },
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(context, null),
                child: Text('إلغاء'),
              ),
              ElevatedButton(
                onPressed: () => Navigator.pop(context, reason.isNotEmpty ? reason : null),
                style: ElevatedButton.styleFrom(
                  backgroundColor: rejectedColor,
                ),
                child: Text('رفض'),
              ),
            ],
          ),
        ),
      ),
    );
    
    return result;
  }
  
  Future<void> _showPermissionDetails(dynamic permission) async {
    await showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Row(
            children: [
              Icon(Icons.info, color: primaryColor),
              SizedBox(width: 10),
              Text('تفاصيل طلب الإذن'),
            ],
          ),
          content: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              mainAxisSize: MainAxisSize.min,
              children: [
                _buildDetailRow('رقم الطلب:', permission['permissionNumber'] ?? 'N/A'),
                _buildDetailRow('الموظف:', permission['employeeName'] ?? 'N/A'),
                _buildDetailRow('الرقم الوظيفي:', permission['employeeCode'] ?? 'N/A'),
                _buildDetailRow('القسم:', permission['departmentName'] ?? 'N/A'),
                _buildDetailRow('المسمى الوظيفي:', permission['jobTitleName'] ?? 'N/A'),
                _buildDetailRow('نوع الإذن:', permission['permissionType'] ?? 'N/A'),
                _buildDetailRow('وقت البداية:', _formatDateTime(permission['startDateTime'])),
                _buildDetailRow('وقت النهاية:', _formatDateTime(permission['endDateTime'])),
                _buildDetailRow('المدة:', '${(permission['duration'] ?? 0).toStringAsFixed(2)} ساعة'),
                if (permission['deductedAmount'] != null && permission['deductedAmount'] > 0)
                  _buildDetailRow('المبلغ المقتطع:', '${(permission['deductedAmount'] ?? 0).toStringAsFixed(2)} جنيه'),
                _buildDetailRow('السبب:', permission['reason'] ?? ''),
                _buildDetailRow('الحالة:', _getStatusText(permission['status'])),
                _buildDetailRow('تاريخ الطلب:', _formatDate(permission['createdAt'])),
              ],
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('إغلاق'),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildDetailRow(String label, String value) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        textDirection: TextDirection.rtl,
        children: [
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.right,
              style: TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
          SizedBox(width: 16),
          Text(
            label,
            style: TextStyle(color: Colors.grey[600]),
          ),
        ],
      ),
    );
  }
  
  String _formatDate(String dateString) {
    try {
      final date = DateTime.parse(dateString);
      return '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}';
    } catch (e) {
      return dateString;
    }
  }
  
  String _formatDateTime(String dateTimeString) {
    try {
      final dateTime = DateTime.parse(dateTimeString);
      return '${_formatDate(dateTimeString)} ${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
    } catch (e) {
      return dateTimeString;
    }
  }
  
  String _getStatusText(String status) {
    switch (status) {
      case 'Pending': return 'قيد الانتظار';
      case 'Approved': return 'موافق';
      case 'Rejected': return 'مرفوض';
      default: return status;
    }
  }
  
  Color _getStatusColor(String status) {
    switch (status) {
      case 'Pending': return pendingColor;
      case 'Approved': return approvedColor;
      case 'Rejected': return rejectedColor;
      default: return Colors.grey;
    }
  }
  
  void _showSuccessSnackBar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
        ),
        backgroundColor: approvedColor,
        duration: Duration(seconds: 2),
      ),
    );
  }
  
  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
        ),
        backgroundColor: rejectedColor,
        duration: Duration(seconds: 3),
      ),
    );
  }
  
  Future<void> _refreshData() async {
    setState(() {
      _currentPage = 1;
      _isRefreshing = true;
    });
    
    await Future.wait([
      _loadPermissions(),
      _loadStatistics(),
    ]);
    
    setState(() => _isRefreshing = false);
  }
  
  void _loadMore() {
    if (_hasMore && !_isLoading) {
      setState(() => _currentPage++);
      _loadPermissions();
    }
  }
  
  Widget _buildStatisticsCard() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'إحصائيات طلبات الإذن',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                _buildStatCircle('قيد الانتظار', _totalPending, pendingColor, _totalHoursPending),
                _buildStatCircle('موافق', _totalApproved, approvedColor, _totalHoursApproved),
                _buildStatCircle('مرفوض', _totalRejected, rejectedColor, _totalHoursRejected),
              ],
            ),
            ],
        ),
      ),
    );
  }
  
  Widget _buildStatCircle(String label, int count, Color color, double hours) {
    return Column(
      children: [
        Container(
          width: 70,
          height: 70,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            shape: BoxShape.circle,
            border: Border.all(color: color, width: 2),
          ),
          child: Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  count.toString(),
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: color,
                  ),
                ),
              ],
            ),
          ),
        ),
        SizedBox(height: 8),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            color: Colors.grey[600],
          ),
        ),
      ],
    );
  }
  
  Widget _buildFiltersCard() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'الفلاتر',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: primaryColor,
              ),
            ),
            SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: DropdownButtonFormField<String>(
                    initialValue: _selectedStatus,
                    items: [
                      DropdownMenuItem(value: 'pending', child: Text('قيد الانتظار')),
                      DropdownMenuItem(value: 'approved', child: Text('موافق')),
                      DropdownMenuItem(value: 'rejected', child: Text('مرفوض')),
                    ],
                    onChanged: (value) {
                      setState(() {
                        _selectedStatus = value!;
                        _currentPage = 1;
                      });
                      _loadPermissions();
                    },
                    decoration: InputDecoration(
                      labelText: 'الحالة',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () async {
                      final DateTime? picked = await showDatePicker(
                        context: context,
                        initialDate: _fromDate ?? DateTime.now(),
                        firstDate: DateTime(2020),
                        lastDate: DateTime.now(),
                        builder: (context, child) {
                          return Theme(
                            data: ThemeData.light().copyWith(
                              colorScheme: ColorScheme.light(
                                primary: primaryColor,
                              ),
                            ),
                            child: Directionality(
                              textDirection: TextDirection.rtl,
                              child: child!,
                            ),
                          );
                        },
                      );
                      if (picked != null) {
                        setState(() {
                          _fromDate = picked;
                          _currentPage = 1;
                        });
                        _loadPermissions();
                      }
                    },
                    icon: Icon(Icons.calendar_today, size: 16),
                    label: Text(
                      _fromDate != null 
                        ? 'من: ${_formatDate(_fromDate!.toIso8601String())}' 
                        : 'من تاريخ',
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.grey[100],
                      foregroundColor: Colors.grey[700],
                    ),
                  ),
                ),
                SizedBox(width: 8),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () async {
                      final DateTime? picked = await showDatePicker(
                        context: context,
                        initialDate: _toDate ?? DateTime.now(),
                        firstDate: _fromDate ?? DateTime(2020),
                        lastDate: DateTime.now(),
                        builder: (context, child) {
                          return Theme(
                            data: ThemeData.light().copyWith(
                              colorScheme: ColorScheme.light(
                                primary: primaryColor,
                              ),
                            ),
                            child: Directionality(
                              textDirection: TextDirection.rtl,
                              child: child!,
                            ),
                          );
                        },
                      );
                      if (picked != null) {
                        setState(() {
                          _toDate = picked;
                          _currentPage = 1;
                        });
                        _loadPermissions();
                      }
                    },
                    icon: Icon(Icons.calendar_today, size: 16),
                    label: Text(
                      _toDate != null 
                        ? 'إلى: ${_formatDate(_toDate!.toIso8601String())}' 
                        : 'إلى تاريخ',
                    ),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.grey[100],
                      foregroundColor: Colors.grey[700],
                    ),
                  ),
                ),
                SizedBox(width: 8),
                ElevatedButton(
                  onPressed: () {
                    setState(() {
                      _fromDate = null;
                      _toDate = null;
                      _currentPage = 1;
                    });
                    _loadPermissions();
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.red[50],
                    foregroundColor: Colors.red,
                  ),
                  child: Icon(Icons.clear, size: 20),
                ),
              ],
            ),
           ],
        ),
      ),
    );
  }
  
  Widget _buildPermissionCard(dynamic permission) {
    final statusColor = _getStatusColor(permission['status']);
    
    return Card(
      elevation: 2,
      margin: EdgeInsets.symmetric(vertical: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
        side: BorderSide(color: statusColor.withValues(alpha: 0.3), width: 1),
      ),
      child: ListTile(
        leading: CircleAvatar(
          backgroundColor: statusColor.withValues(alpha: 0.1),
          child: Icon(
            _getPermissionIcon(permission['permissionType']),
            color: statusColor,
          ),
        ),
        title: Text(
          permission['employeeName'] ?? 'غير معروف',
          textDirection: TextDirection.rtl,
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'رقم الطلب: ${permission['permissionNumber'] ?? 'N/A'}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'النوع: ${permission['permissionType'] ?? 'N/A'}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'الوقت: ${_formatDateTime(permission['startDateTime'])}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'المدة: ${(permission['duration'] ?? 0).toStringAsFixed(1)} ساعة',
              textDirection: TextDirection.rtl,
            ),
            if (permission['deductedAmount'] != null && permission['deductedAmount'] > 0)
              Text(
                'الخصم: ${(permission['deductedAmount'] ?? 0).toStringAsFixed(2)} جنيه',
                textDirection: TextDirection.rtl,
                style: TextStyle(color: Colors.red),
              ),
            Chip(
              label: Text(
                _getStatusText(permission['status']),
                style: TextStyle(fontSize: 12, color: Colors.white),
              ),
              backgroundColor: statusColor,
              padding: EdgeInsets.symmetric(horizontal: 8),
            ),
          ],
        ),
        trailing: _selectedStatus == 'pending'
            ? PopupMenuButton<String>(
                onSelected: (value) {
                  if (value == 'approve') {
                    _approvePermission(permission['id']);
                  } else if (value == 'reject') {
                    _rejectPermission(permission['id']);
                  } else if (value == 'details') {
                    _showPermissionDetails(permission);
                  }
                },
                itemBuilder: (context) => [
                  PopupMenuItem(
                    value: 'details',
                    child: Row(
                      children: [
                        Icon(Icons.info, color: primaryColor, size: 20),
                        SizedBox(width: 8),
                        Text('التفاصيل'),
                      ],
                    ),
                  ),
                  PopupMenuItem(
                    value: 'approve',
                    child: Row(
                      children: [
                        Icon(Icons.check_circle, color: approvedColor, size: 20),
                        SizedBox(width: 8),
                        Text('موافقة'),
                      ],
                    ),
                  ),
                  PopupMenuItem(
                    value: 'reject',
                    child: Row(
                      children: [
                        Icon(Icons.cancel, color: rejectedColor, size: 20),
                        SizedBox(width: 8),
                        Text('رفض'),
                      ],
                    ),
                  ),
                ],
                icon: Icon(Icons.more_vert),
              )
            : IconButton(
                icon: Icon(Icons.info),
                onPressed: () => _showPermissionDetails(permission),
              ),
        onTap: () => _showPermissionDetails(permission),
      ),
    );
  }
  
  IconData _getPermissionIcon(String permissionType) {
    switch (permissionType) {
      case 'مأمورية':
        return Icons.assignment;
      case 'إذن':
        return Icons.timer;
      case 'إذن طبي':
        return Icons.medical_services;
      case 'إذن عائلي':
        return Icons.family_restroom;
      case 'إذن طارئ':
        return Icons.emergency;
      default:
        return Icons.timer;
    }
  }
  
  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.timer_off,
            size: 80,
            color: Colors.grey[300],
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد طلبات إذن',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
            ),
          ),
          SizedBox(height: 8),
          Text(
            'لم يتم العثور على طلبات إذن تتوافق مع الفلاتر المحددة',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: Colors.grey[500],
            ),
          ),
        ],
      ),
    );
  }
  
  Widget _buildLoadingIndicator() {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 16),
      child: Center(
        child: CircularProgressIndicator(
          color: primaryColor,
        ),
      ),
    );
  }
  
  @override
  Widget build(BuildContext context) {
    if (!(widget.user['isManager'] ?? false)) {
      return Scaffold(
        appBar: AppBar(
          title: Text('صلاحيات غير كافية'),
        ),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.error, size: 64, color: Colors.red),
              SizedBox(height: 16),
              Text(
                'ليس لديك صلاحيات الوصول لهذه الصفحة',
                style: TextStyle(fontSize: 18),
              ),
              SizedBox(height: 16),
              ElevatedButton(
                onPressed: () => Navigator.pop(context),
                child: Text('العودة'),
              ),
            ],
          ),
        ),
      );
    }
    
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        appBar: AppBar(
          title: Text(
            'اعتماد طلبات الإذن',
            style: TextStyle(fontWeight: FontWeight.bold, fontFamily: 'Tajawal'),
          ),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          actions: [
            IconButton(
              icon: Icon(Icons.refresh),
              onPressed: _isRefreshing ? null : _refreshData,
            ),
          ],
        ),
        body: _isLoading && _currentPage == 1
            ? Center(
                child: CircularProgressIndicator(
                  color: primaryColor,
                ),
              )
            : RefreshIndicator(
                onRefresh: _refreshData,
                child: NotificationListener<ScrollNotification>(
                  onNotification: (scrollNotification) {
                    if (scrollNotification is ScrollEndNotification &&
                        scrollNotification.metrics.pixels ==
                            scrollNotification.metrics.maxScrollExtent &&
                        _hasMore &&
                        !_isLoading) {
                      _loadMore();
                    }
                    return false;
                  },
                  child: ListView(
                    padding: EdgeInsets.all(16),
                    children: [
                      _buildStatisticsCard(),
                      SizedBox(height: 16),
                      _buildFiltersCard(),
                      SizedBox(height: 24),
                      ..._getCurrentPermissions().map((permission) => 
                          _buildPermissionCard(permission)),
                      if (_isLoading && _currentPage > 1) _buildLoadingIndicator(),
                      if (_getCurrentPermissions().isEmpty && !_isLoading) _buildEmptyState(),
                      SizedBox(height: 16),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}