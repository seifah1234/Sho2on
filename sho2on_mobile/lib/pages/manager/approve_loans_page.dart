import 'package:flutter/material.dart';
import '../../services/loan_service.dart';

class ApproveLoansPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const ApproveLoansPage({super.key, required this.user});

  @override
  _ApproveLoansPageState createState() => _ApproveLoansPageState();
}

class _ApproveLoansPageState extends State<ApproveLoansPage> {
  final LoanService _loanService = LoanService();
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isRefreshing = false;
  
  // قائمة الطلبات
  List<dynamic> _pendingLoans = [];
  List<dynamic> _approvedLoans = [];
  List<dynamic> _rejectedLoans = [];
  
  // فلاتر
  final String _searchTerm = '';
  String _selectedStatus = 'pending'; // pending, approved, rejected
  DateTime? _fromDate;
  DateTime? _toDate;

  // إحصائيات
  int _totalPending = 0;
  int _totalApproved = 0;
  int _totalRejected = 0;
  
  // التصفح
  int _currentPage = 1;
  final int _pageSize = 10;
  int _totalRecords = 0;
  bool _hasMore = true;
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF673AB7);
  final Color pendingColor = Color(0xFFFF9800);
  final Color approvedColor = Color(0xFF4CAF50);
  final Color rejectedColor = Color(0xFFF44336);
  final Color backgroundColor = Color(0xFFF5F7FA);
  
  @override
  void initState() {
    super.initState();
    if (widget.user['isManager'] ?? false) {
      _loadLoans();
      _loadStatistics();
    }
  }
  
Future<void> _loadLoans() async {
  if (!(widget.user['isManager'] ?? false)) return;
  
  if (_currentPage == 1) {
    setState(() => _isLoading = true);
  }
  
  try {
    String status;
    switch (_selectedStatus) {
      case 'pending':
        status = 'Pending';
        break;
      case 'approved':
        status = 'Approved';
        break;
      case 'rejected':
        status = 'Rejected';
        break;
      default:
        status = 'Pending';
    }
    
    // استخدام الدالة الجديدة
    final result = await _loanService.getManagerLoansByStatus(
      managerId: widget.user['id'],
      status: status,
      searchTerm: _searchTerm.isNotEmpty ? _searchTerm : null,
      fromDate: _fromDate,
      toDate: _toDate,
      pageNumber: _currentPage,
      pageSize: _pageSize,
    );
    
    if (result['success']) {
      final List<dynamic> newLoans = result['data'] ?? [];
      _totalRecords = result['totalRecords'] ?? 0;
      
      setState(() {
        if (_currentPage == 1) {
          // إعادة تعيين القائمة المناسبة
          switch (_selectedStatus) {
            case 'pending':
              _pendingLoans = newLoans;
              break;
            case 'approved':
              _approvedLoans = newLoans;
              break;
            case 'rejected':
              _rejectedLoans = newLoans;
              break;
          }
        } else {
          // إضافة إلى القائمة المناسبة
          switch (_selectedStatus) {
            case 'pending':
              _pendingLoans.addAll(newLoans);
              break;
            case 'approved':
              _approvedLoans.addAll(newLoans);
              break;
            case 'rejected':
              _rejectedLoans.addAll(newLoans);
              break;
          }
        }
        _hasMore = newLoans.length == _pageSize;
      });
    } else {
      _showError(result['message'] ?? 'فشل في تحميل الطلبات');
    }
  } catch (e) {
    _showError('خطأ في تحميل البيانات: $e');
  } finally {
    setState(() => _isLoading = false);
  }
}

// تحديث دالة _getCurrentLoans:
List<dynamic> _getCurrentLoans() {
  switch (_selectedStatus) {
    case 'pending':
      return _pendingLoans;
    case 'approved':
      return _approvedLoans;
    case 'rejected':
      return _rejectedLoans;
    default:
      return _pendingLoans;
  }
}

  Future<void> _loadStatistics() async {
    try {
      final result = await _loanService.getManagerLoanStats(
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
        });
      }
    } catch (e) {
      print('Error loading statistics: $e');
    }
  }
  
  Future<void> _approveLoan(int loanId) async {
    try {
      final confirmed = await _showConfirmationDialog(
        'موافقة على الطلب',
        'هل أنت متأكد من الموافقة على طلب السلفة؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _loanService.approveLoan(loanId);
      
      if (result['success']) {
        // إعادة تحميل البيانات
        _currentPage = 1;
        await _loadLoans();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تمت الموافقة على الطلب بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في الموافقة على الطلب');
      }
    } catch (e) {
      _showError('خطأ في الموافقة: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _rejectLoan(int loanId) async {
    try {
      final reason = await _showRejectionDialog();
      if (reason == null || reason.isEmpty) return;
      
      final confirmed = await _showConfirmationDialog(
        'رفض الطلب',
        'هل أنت متأكد من رفض طلب السلفة؟',
      );
      
      if (!confirmed) return;
      
      setState(() => _isLoading = true);
      
      final result = await _loanService.rejectLoan(loanId, reason);
      
      if (result['success']) {
        // إعادة تحميل البيانات
        _currentPage = 1;
        await _loadLoans();
        await _loadStatistics();
        
        _showSuccessSnackBar(result['message'] ?? 'تم رفض الطلب بنجاح');
      } else {
        _showError(result['message'] ?? 'فشل في رفض الطلب');
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
                hintText: 'أدخل سبب رفض الطلب',
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
  
  Future<void> _showLoanDetails(dynamic loan) async {
    await showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Row(
            children: [
              Icon(Icons.info, color: primaryColor),
              SizedBox(width: 10),
              Text('تفاصيل الطلب'),
            ],
          ),
          content: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              mainAxisSize: MainAxisSize.min,
              children: [
                _buildDetailRow('رقم الطلب:', loan['loanNumber'] ?? 'N/A'),
                _buildDetailRow('الموظف:', loan['employeeName'] ?? 'N/A'),
                _buildDetailRow('الرقم الوظيفي:', loan['employeeCode'] ?? 'N/A'),
                _buildDetailRow('القسم:', loan['departmentName'] ?? 'N/A'),
                _buildDetailRow('المسمى الوظيفي:', loan['jobTitleName'] ?? 'N/A'),
                _buildDetailRow('المبلغ:', '${(loan['loanAmount'] ?? 0).toStringAsFixed(2)} جنيه'),
                _buildDetailRow('المبلغ المتبقي:', '${(loan['remainingAmount'] ?? 0).toStringAsFixed(2)} جنيه'),
                _buildDetailRow('تاريخ الطلب:', _formatDate(loan['loanDate'])),
                if (loan['expectedPaybackDate'] != null)
                  _buildDetailRow('تاريخ السداد:', _formatDate(loan['expectedPaybackDate'])),
                _buildDetailRow('عدد الأشهر:', '${loan['installmentMonths'] ?? 0}'),
                _buildDetailRow('القسط الشهري:', '${(loan['monthlyInstallment'] ?? 0).toStringAsFixed(2)} جنيه'),
                _buildDetailRow('السبب:', loan['reason'] ?? ''),
                _buildDetailRow('الحالة:', _getStatusText(loan['status'])),
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
    switch (status) {
      case 'Pending': return 'قيد الانتظار';
      case 'Approved': return 'موافق';
      case 'Rejected': return 'مرفوض';
      case 'PartiallyPaid': return 'مسدد جزئياً';
      case 'Paid': return 'مسدد بالكامل';
      default: return status;
    }
  }
  
  Color _getStatusColor(String status) {
    switch (status) {
      case 'Pending': return pendingColor;
      case 'Approved': return approvedColor;
      case 'Rejected': return rejectedColor;
      case 'PartiallyPaid': return Colors.blue;
      case 'Paid': return Colors.green;
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
      _loadLoans(),
      _loadStatistics(),
    ]);
    
    setState(() => _isRefreshing = false);
  }
  
  void _loadMore() {
    if (_hasMore && !_isLoading) {
      setState(() => _currentPage++);
      _loadLoans();
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
              'إحصائيات الطلبات',
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
                _buildStatCircle('قيد الانتظار', _totalPending, pendingColor),
                _buildStatCircle('موافق', _totalApproved, approvedColor),
                _buildStatCircle('مرفوض', _totalRejected, rejectedColor),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildStatCircle(String label, int count, Color color) {
    return Column(
      children: [
        Container(
          width: 60,
          height: 60,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            shape: BoxShape.circle,
            border: Border.all(color: color, width: 2),
          ),
          child: Center(
            child: Text(
              count.toString(),
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: color,
              ),
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
                      _loadLoans();
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
                        _loadLoans();
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
                        _loadLoans();
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
                    _loadLoans();
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
  
  Widget _buildLoanCard(dynamic loan) {
    final statusColor = _getStatusColor(loan['status']);
    
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
            _getStatusIcon(loan['status']),
            color: statusColor,
          ),
        ),
        title: Text(
          loan['employeeName'] ?? 'غير معروف',
          textDirection: TextDirection.rtl,
          style: TextStyle(fontWeight: FontWeight.bold),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'رقم الطلب: ${loan['loanNumber'] ?? 'N/A'}',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'المبلغ: ${(loan['loanAmount'] ?? 0).toStringAsFixed(2)} جنيه',
              textDirection: TextDirection.rtl,
            ),
            Text(
              'التاريخ: ${_formatDate(loan['loanDate'])}',
              textDirection: TextDirection.rtl,
            ),
            Chip(
              label: Text(
                _getStatusText(loan['status']),
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
                    _approveLoan(loan['id']);
                  } else if (value == 'reject') {
                    _rejectLoan(loan['id']);
                  } else if (value == 'details') {
                    _showLoanDetails(loan);
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
                onPressed: () => _showLoanDetails(loan),
              ),
        onTap: () => _showLoanDetails(loan),
      ),
    );
  }
  
  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'Pending': return Icons.access_time;
      case 'Approved': return Icons.check_circle;
      case 'Rejected': return Icons.cancel;
      case 'PartiallyPaid': return Icons.payment;
      case 'Paid': return Icons.done_all;
      default: return Icons.help;
    }
  }
  
  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.account_balance_wallet,
            size: 80,
            color: Colors.grey[300],
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد طلبات سلف',
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
            ),
          ),
          SizedBox(height: 8),
          Text(
            'لم يتم العثور على طلبات سلف تتوافق مع الفلاتر المحددة',
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
    // التحقق من صلاحيات المدير
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
            'اعتماد طلبات السلف',
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
                      // إحصائيات
                      _buildStatisticsCard(),
                      
                      SizedBox(height: 16),
                      
                      // فلاتر
                      _buildFiltersCard(),
                      
                      SizedBox(height: 24),
                      
                      // قائمة الطلبات
                      ..._getCurrentLoans().map((loan) => _buildLoanCard(loan)),
                      
                      if (_isLoading && _currentPage > 1) _buildLoadingIndicator(),
                      
                      if (_getCurrentLoans().isEmpty && !_isLoading) _buildEmptyState(),
                      
                      SizedBox(height: 16),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}