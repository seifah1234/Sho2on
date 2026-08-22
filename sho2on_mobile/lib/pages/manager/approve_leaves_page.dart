import 'package:flutter/material.dart';
import '../../services/holiday_service.dart';

class ApproveHolidaysPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const ApproveHolidaysPage({super.key, required this.user});

  @override
  _ApproveHolidaysPageState createState() => _ApproveHolidaysPageState();
}

class _ApproveHolidaysPageState extends State<ApproveHolidaysPage> {
  final HolidayService _holidayService = HolidayService();
  
  bool _isLoading = false;
  bool _isRefreshing = false;
  
  List<dynamic> _pendingHolidays = [];
  List<dynamic> _approvedHolidays = [];
  List<dynamic> _rejectedHolidays = [];
  
  final String _searchTerm = '';
  String _selectedStatus = 'pending';
  DateTime? _fromDate;
  DateTime? _toDate;
  
  int _totalPending = 0;
  int _totalApproved = 0;
  int _totalRejected = 0;
  int _totalDaysPending = 0;
  int _totalDaysApproved = 0;
  int _totalDaysRejected = 0;
  
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
      _loadHolidays();
      _loadStatistics();
    }
  }
  
  Future<void> _loadHolidays() async {
    if (!(widget.user['isManager'] ?? false)) return;
    
    if (_currentPage == 1) {
      setState(() => _isLoading = true);
    }
    
    try {
      final result = await _holidayService.getManagerHolidaysByStatus(
        managerId: widget.user['id'],
        status: _selectedStatus,
        searchTerm: _searchTerm.isNotEmpty ? _searchTerm : null,
        fromDate: _fromDate,
        toDate: _toDate,
        pageNumber: _currentPage,
        pageSize: _pageSize,
      );
      
      if (result['success']) {
        final List<dynamic> newHolidays = result['data'] ?? [];
        _totalRecords = result['totalRecords'] ?? 0;
        
        setState(() {
          if (_currentPage == 1) {
            switch (_selectedStatus) {
              case 'pending':
                _pendingHolidays = newHolidays;
                break;
              case 'approved':
                _approvedHolidays = newHolidays;
                break;
              case 'rejected':
                _rejectedHolidays = newHolidays;
                break;
            }
          } else {
            switch (_selectedStatus) {
              case 'pending':
                _pendingHolidays.addAll(newHolidays);
                break;
              case 'approved':
                _approvedHolidays.addAll(newHolidays);
                break;
              case 'rejected':
                _rejectedHolidays.addAll(newHolidays);
                break;
            }
          }
          _hasMore = newHolidays.length == _pageSize;
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل طلبات الإجازة');
      }
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  List<dynamic> _getCurrentHolidays() {
    switch (_selectedStatus) {
      case 'pending':
        return _pendingHolidays;
      case 'approved':
        return _approvedHolidays;
      case 'rejected':
        return _rejectedHolidays;
      default:
        return _pendingHolidays;
    }
  }
  
  Future<void> _loadStatistics() async {
    try {
      final result = await _holidayService.getManagerHolidayStats(
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
          _totalDaysPending = stats['totalDaysPending'] ?? 0;
          _totalDaysApproved = stats['totalDaysApproved'] ?? 0;
          _totalDaysRejected = stats['totalDaysRejected'] ?? 0;
        });
      }
    } catch (e) {
      print('Error loading statistics: $e');
    }
  }
  
  Future<void> _approveHoliday(int requestId) async {
    try {
      final confirmed = await _showConfirmationDialog(
        'موافقة على طلب الإجازة',
        'هل أنت متأكد من الموافقة على طلب الإجازة؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _holidayService.approveHoliday(requestId);
      
      if (result['success']) {
        _currentPage = 1;
        await _loadHolidays();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تمت الموافقة على طلب الإجازة بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في الموافقة على طلب الإجازة');
      }
    } catch (e) {
      _showError('خطأ في الموافقة: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _rejectHoliday(int requestId) async {
    try {
      final reason = await _showRejectionDialog();
      if (reason == null || reason.isEmpty) return;
      
      final confirmed = await _showConfirmationDialog(
        'رفض طلب الإجازة',
        'هل أنت متأكد من رفض طلب الإجازة؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _holidayService.rejectHoliday(requestId, reason);
      
      if (result['success']) {
        _currentPage = 1;
        await _loadHolidays();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تم رفض طلب الإجازة بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في رفض طلب الإجازة');
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
                hintText: 'أدخل سبب رفض طلب الإجازة',
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
  
  Future<void> _showHolidayDetails(dynamic holiday) async {
    await showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Row(
            children: [
              Icon(Icons.info, color: primaryColor),
              SizedBox(width: 10),
              Text('تفاصيل طلب الإجازة'),
            ],
          ),
          content: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              mainAxisSize: MainAxisSize.min,
              children: [
                _buildDetailRow('رقم الطلب:', holiday['requestNumber'] ?? 'N/A'),
                _buildDetailRow('الموظف:', holiday['employeeName'] ?? 'N/A'),
                _buildDetailRow('الرقم الوظيفي:', holiday['employeeCode'] ?? 'N/A'),
                _buildDetailRow('القسم:', holiday['departmentName'] ?? 'N/A'),
                _buildDetailRow('المسمى الوظيفي:', holiday['jobTitleName'] ?? 'N/A'),
                _buildDetailRow('نوع الإجازة:', holiday['leaveTypeName'] ?? 'N/A'),
                _buildDetailRow('من تاريخ:', _formatDate(holiday['startDate'])),
                _buildDetailRow('إلى تاريخ:', _formatDate(holiday['endDate'])),
                _buildDetailRow('المدة:', '${holiday['duration'] ?? 0} يوم'),
                _buildDetailRow('السبب:', holiday['reason'] ?? ''),
                _buildDetailRow('الحالة:', holiday['status'] ?? 'N/A'),
                _buildDetailRow('تاريخ الطلب:', _formatDate(holiday['requestDate'])),
                if (holiday['isCancelled'] ?? false)
                  _buildDetailRow('ملاحظة:', 'تم إلغاء الطلب'),
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
  
  String _getStatusText(String status) {
    return status;
  }
  
  Color _getStatusColor(String status) {
    switch (status) {
      case 'قيد الانتظار':
        return pendingColor;
      case 'موافق':
        return approvedColor;
      case 'مرفوض':
        return rejectedColor;
      default:
        return Colors.grey;
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
      _loadHolidays(),
      _loadStatistics(),
    ]);
    
    setState(() => _isRefreshing = false);
  }
  
  void _loadMore() {
    if (_hasMore && !_isLoading) {
      setState(() => _currentPage++);
      _loadHolidays();
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
              'إحصائيات طلبات الإجازة',
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
                _buildStatCircle('قيد الانتظار', _totalPending, pendingColor, _totalDaysPending),
                _buildStatCircle('موافق', _totalApproved, approvedColor, _totalDaysApproved),
                _buildStatCircle('مرفوض', _totalRejected, rejectedColor, _totalDaysRejected),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildStatCircle(String label, int count, Color color, int days) {
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
                )
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
                      _loadHolidays();
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
                        _loadHolidays();
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
                        _loadHolidays();
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
                    _loadHolidays();
                  },
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.red[50],
                    foregroundColor: Colors.red,
                  ),
                  child: Icon(Icons.clear, size: 20),
                ),
              ],
            ),],
        ),
      ),
    );
  }
  
  Widget _buildHolidayCard(dynamic holiday) {
    final statusColor = _getStatusColor(holiday['status']);
    
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
            _getHolidayIcon(holiday['leaveTypeName']),
            color: statusColor,
          ),
        ),
        title: Text(
          holiday['employeeName'] ?? 'غير معروف',
          textDirection: TextDirection.rtl,
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'رقم الطلب: ${holiday['requestNumber'] ?? 'N/A'}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'نوع الإجازة: ${holiday['leaveTypeName'] ?? 'N/A'}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'من: ${_formatDate(holiday['startDate'])}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'المدة: ${holiday['duration'] ?? 0} يوم',
              textDirection: TextDirection.rtl,
            ),
            Chip(
              label: Text(
                holiday['status'] ?? 'N/A',
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
                    _approveHoliday(holiday['id']);
                  } else if (value == 'reject') {
                    _rejectHoliday(holiday['id']);
                  } else if (value == 'details') {
                    _showHolidayDetails(holiday);
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
                onPressed: () => _showHolidayDetails(holiday),
              ),
        onTap: () => _showHolidayDetails(holiday),
      ),
    );
  }
  
  IconData _getHolidayIcon(String leaveType) {
    if (leaveType.contains('سنوية') || leaveType.contains('عادية')) {
      return Icons.beach_access;
    } else if (leaveType.contains('مرضية') || leaveType.contains('طبية')) {
      return Icons.medical_services;
    } else if (leaveType.contains('زواج') || leaveType.contains('عائلية')) {
      return Icons.family_restroom;
    } else if (leaveType.contains('طارئة') || leaveType.contains('خاصة')) {
      return Icons.emergency;
    } else {
      return Icons.event_note;
    }
  }
  
  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.beach_access,
            size: 80,
            color: Colors.grey[300],
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد طلبات إجازة',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
            ),
          ),
          SizedBox(height: 8),
          Text(
            'لم يتم العثور على طلبات إجازة تتوافق مع الفلاتر المحددة',
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
            'اعتماد طلبات الإجازة',
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
                      ..._getCurrentHolidays().map((holiday) => 
                          _buildHolidayCard(holiday)),
                      if (_isLoading && _currentPage > 1) _buildLoadingIndicator(),
                      if (_getCurrentHolidays().isEmpty && !_isLoading) _buildEmptyState(),
                      SizedBox(height: 16),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}