import 'package:flutter/material.dart';
import '../services/holiday_service.dart';

class LeaveHistoryPage extends StatefulWidget {
  final Map user;
  const LeaveHistoryPage({super.key, required this.user});

  @override
  _LeaveHistoryPageState createState() => _LeaveHistoryPageState();
}

class _LeaveHistoryPageState extends State<LeaveHistoryPage> {
  final HolidayService _holidayService = HolidayService();
  
  // قائمة طلبات الإجازة
  List<dynamic> _leaveRequests = [];
  
  // حالات التصفية
  String _selectedFilter = 'الكل';
  final List<String> _filterOptions = ['الكل', 'قيد الانتظار', 'موافق', 'مرفوض', 'مسودة'];
  final Map<String, int> _statusMap = {
    'الكل': -1,
    'قيد الانتظار': 1,
    'موافق': 2,
    'مرفوض': 3,
    'مسودة': 0,
  };
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isRefreshing = false;
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color secondaryColor = Color(0xFF42A5F5);
  final Color backgroundColor = Color(0xFFF5F7FA);
  final Color pendingColor = Color(0xFFFF9800);
  final Color approvedColor = Color(0xFF4CAF50);
  final Color rejectedColor = Color(0xFFF44336);
  final Color draftColor = Color(0xFF9E9E9E);
  
  @override
  void initState() {
    super.initState();
    _loadLeaveRequests();
  }
  
  Future<void> _loadLeaveRequests({String? filter}) async {
    setState(() => _isLoading = true);
    
    try {
      final result = await _holidayService.getEmployeeRequests(
        widget.user['id'],
        status: filter != null && filter != 'الكل' ? _statusMap[filter] : null,
      );
      
      if (result['success']) {
        setState(() {
          _leaveRequests = result['data'] ?? [];
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل البيانات');
      }
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() {
        _isLoading = false;
        _isRefreshing = false;
      });
    }
  }
  
  Future<void> _refreshData() async {
    setState(() => _isRefreshing = true);
    await _loadLeaveRequests();
  }
  
  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        backgroundColor: Colors.red,
        duration: Duration(seconds: 3),
      ),
    );
  }

  String _formatDate(String? dateString) {
    if (dateString == null || dateString.isEmpty) return 'غير محدد';
    
    try {
      // معالجة التاريخ يدوياً بدون intl package
      DateTime date = DateTime.parse(dateString);
      return '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}';
    } catch (e) {
      return dateString;
    }
  }
  
  Color _getStatusColor(String status) {
    switch (status) {
      case 'قيد الانتظار':
        return pendingColor;
      case 'موافق':
        return approvedColor;
      case 'مرفوض':
        return rejectedColor;
      case 'مسودة':
        return draftColor;
      default:
        return Colors.grey;
    }
  }
  
  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'قيد الانتظار':
        return Icons.access_time;
      case 'موافق':
        return Icons.check_circle;
      case 'مرفوض':
        return Icons.cancel;
      case 'مسودة':
        return Icons.drafts;
      default:
        return Icons.help;
    }
  }
  
  Widget _buildStatusChip(String status) {
    return Chip(
      label: Text(
        status,
        style: TextStyle(
          fontSize: 12,
          fontWeight: FontWeight.bold,
          color: Colors.white,
          fontFamily: 'Tajawal',
        ),
      ),
      backgroundColor: _getStatusColor(status),
      avatar: Icon(
        _getStatusIcon(status),
        size: 16,
        color: Colors.white,
      ),
    );
  }

  // يمكنك إضافة تأثيرات حركية
  Widget _buildLeaveRequestCardWithAnimation(Map<String, dynamic> request, int index) {
    return AnimatedContainer(
      duration: Duration(milliseconds: 300 + (index * 100)),
      curve: Curves.easeInOut,
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: _buildLeaveRequestCard(request),
    );
  }
  
  Widget _buildLeaveRequestCard(Map<String, dynamic> request) {
    
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Container(
        decoration: BoxDecoration(
          border: Border(
            left: BorderSide(
              color: _getStatusColor(request['status']),
              width: 4,
            ),
          ),
        ),
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // الصف العلوي: معلومات الطلب الأساسية
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                textDirection: TextDirection.rtl,
                children: [
                  // رقم الطلب
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'رقم الطلب',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          request['requestNumber'] ?? 'HR-000000',
                          textDirection: TextDirection.ltr,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: primaryColor,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                  ),
                  
                  // نوع الإجازة
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'نوع الإجازة',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          request['leaveTypeName'] ?? 'غير محدد',
                          textDirection: TextDirection.rtl,
                          overflow: TextOverflow.ellipsis,
                          maxLines: 1,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.black,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                  ),

                  SizedBox(width: 30),
                  
                  // حالة الطلب
                  _buildStatusChip(request['status']),
                ],
              ),
              
              SizedBox(height: 16),
              
              // معلومات الفترة
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                  textDirection: TextDirection.rtl,
                  children: [
                    Column(
                      children: [
                        Text(
                          'من تاريخ',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          _formatDate(request['startDate']),
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.black,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                    
                    Icon(Icons.arrow_forward, color: primaryColor),
                    
                    Column(
                      children: [
                        Text(
                          'إلى تاريخ',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          _formatDate(request['endDate']),
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.black,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                    
                    Column(
                      children: [
                        Text(
                          'المدة',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          '${request['duration']} يوم',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            color: Colors.black,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              
              SizedBox(height: 16),
              
              // سبب الإجازة
              if (request['reason'] != null && request['reason'].isNotEmpty)
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      'السبب',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey[600],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      request['reason'],
                      textDirection: TextDirection.rtl,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        fontSize: 14,
                        color: Colors.black87,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              
              SizedBox(height: 16),
              
              // معلومات إضافية
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                textDirection: TextDirection.rtl,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        'تاريخ الطلب',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontSize: 11,
                          color: Colors.grey[600],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        _formatDate(request['requestDate']),
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.black,
                          fontFamily: 'Tajawal',
                        ),
                      ),
                    ],
                  ),
                  
                  if (request['approvedByName'] != null)
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'الموافق',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 11,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          request['approvedByName']!,
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: primaryColor,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                  
                  if (request['approvedDate'] != null)
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'تاريخ الموافقة',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 11,
                            color: Colors.grey[600],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                           _formatDate(request['approvedDate']),
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.black,
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
  
  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(
            Icons.beach_access,
            size: 80,
            color: Colors.grey[400],
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد طلبات إجازة',
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 18,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 8),
          Text(
            'يمكنك تقديم طلب إجازة جديد\nمن خلال زر "طلب إجازة"',
            textDirection: TextDirection.rtl,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14,
              color: Colors.grey[500],
              fontFamily: 'Tajawal',
            ),
          ),
        ],
      ),
    );
  }
  
  Widget _buildFilterChips() {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      color: Colors.white,
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        reverse: true, // لجعل العناصر تظهر من اليمين
        child: Row(
          mainAxisAlignment: MainAxisAlignment.end,
          textDirection: TextDirection.rtl,
          children: _filterOptions.map((filter) {
            return Padding(
              padding: const EdgeInsets.symmetric(horizontal: 4),
              child: FilterChip(
                label: Text(
                  filter,
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    color: _selectedFilter == filter ? Colors.white : Colors.black,
                  ),
                ),
                selected: _selectedFilter == filter,
                selectedColor: primaryColor,
                backgroundColor: Colors.grey[200],
                checkmarkColor: Colors.white,
                onSelected: (selected) {
                  setState(() {
                    _selectedFilter = filter;
                    _loadLeaveRequests(filter: filter);
                  });
                },
              ),
            );
          }).toList(),
        ),
      ),
    );
  }
  
  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        appBar: AppBar(
          title: Text(
            'سجل الإجازات',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontFamily: 'Tajawal',
            ),
          ),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
          leading: IconButton(
            icon: Icon(Icons.arrow_back),
            onPressed: () => Navigator.pop(context),
          ),
          actions: [
            IconButton(
              icon: Icon(Icons.refresh),
              onPressed: _refreshData,
            ),
          ],
        ),
        body: Column(
          children: [
            // رأس الصفحة مع الإحصائيات
            Container(
              color: Colors.white,
              padding: EdgeInsets.all(16),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                textDirection: TextDirection.rtl,
                children: [
                  // إجمالي الطلبات
                  _buildStatItem(
                    'إجمالي الطلبات',
                    _leaveRequests.length.toString(),
                    Icons.list,
                    primaryColor,
                  ),
                  
                  // الطلبات المعلقة
                  _buildStatItem(
                    'قيد الانتظار',
                    _leaveRequests
                        .where((r) => r['status'] == 'قيد الانتظار')
                        .length
                        .toString(),
                    Icons.access_time,
                    pendingColor,
                  ),
                  
                  // الطلبات الموافق عليها
                  _buildStatItem(
                    'الموافق عليها',
                    _leaveRequests
                        .where((r) => r['status'] == 'موافق')
                        .length
                        .toString(),
                    Icons.check_circle,
                    approvedColor,
                  ),
                ],
              ),
            ),
            
            // شريط التصفية
            _buildFilterChips(),
            
            SizedBox(height: 8),
            
            // قائمة طلبات الإجازة
            Expanded(
              child: _isLoading && !_isRefreshing
                  ? Center(
                      child: CircularProgressIndicator(
                        color: primaryColor,
                      ),
                    )
                  : _leaveRequests.isEmpty
                      ? _buildEmptyState()
                      : RefreshIndicator(
                          onRefresh: _refreshData,
                          color: primaryColor,
                          child: ListView.builder(
                            padding: EdgeInsets.only(bottom: 16),
                            itemCount: _leaveRequests.length,
                            itemBuilder: (context, index) {
                              return _buildLeaveRequestCardWithAnimation(
                                _leaveRequests[index],
                                index,
                              );
                            },
                          ),
                        ),
            ),
          ],
        ),
        floatingActionButton: FloatingActionButton(
          onPressed: () {
            // العودة للصفحة الرئيسية أو فتح صفحة طلب جديد
            Navigator.pop(context);
          },
          backgroundColor: primaryColor,
          child: Icon(Icons.home),
        ),
      ),
    );
  }
  
  Widget _buildStatItem(String title, String value, IconData icon, Color color) {
    return Column(
      children: [
        Container(
          padding: EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.1),
            shape: BoxShape.circle,
          ),
          child: Icon(
            icon,
            color: color,
            size: 20,
          ),
        ),
        SizedBox(height: 8),
        Text(
          value,
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: Colors.black,
            fontFamily: 'Tajawal',
          ),
        ),
        SizedBox(height: 4),
        Text(
          title,
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontSize: 12,
            color: Colors.grey[600],
            fontFamily: 'Tajawal',
          ),
        ),
      ],
    );
  }
}