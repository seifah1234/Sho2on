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

  List<dynamic> _leaveRequests = [];
  String _selectedFilter = 'الكل';
  final List<String> _filterOptions = ['الكل', 'قيد الانتظار', 'موافق', 'مرفوض', 'مسودة'];
  final Map<String, int> _statusMap = {
    'الكل': -1,
    'قيد الانتظار': 1,
    'موافق': 2,
    'مرفوض': 3,
    'مسودة': 0,
  };

  bool _isLoading = false;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color pendingColor = Color(0xFFF59E0B);
  final Color approvedColor = Color(0xFF10B981);
  final Color rejectedColor = Color(0xFFEF4444);
  final Color draftColor = Color(0xFF64748B);

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

      if (result['success'] && mounted) {
        setState(() {
          _leaveRequests = result['data'] ?? [];
        });
      } else if (mounted) {
        _showError(result['message'] ?? 'فشل في تحميل البيانات');
      }
    } catch (e) {
      if (mounted) _showError('خطأ في تحميل البيانات');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, textDirection: TextDirection.rtl),
        backgroundColor: rejectedColor,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  String _formatDate(String? dateString) {
    if (dateString == null || dateString.isEmpty) return '—';
    try {
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
        return Icons.hourglass_empty;
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

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        appBar: AppBar(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'سجل الإجازات',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              if (_leaveRequests.isNotEmpty)
                Text(
                  '${_leaveRequests.length} طلب',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8)),
                ),
            ],
          ),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(bottom: Radius.circular(20))),
        ),
        body: Column(
          children: [
            // Stats
            if (!_isLoading && _leaveRequests.isNotEmpty)
              Container(
                padding: EdgeInsets.all(16),
                child: Row(
                  children: [
                    Expanded(
                      child: _buildStatCard('الطلبات', _leaveRequests.length.toString(), Icons.list_alt, primaryBlue),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: _buildStatCard(
                        'قيد الانتظار',
                        _leaveRequests.where((r) => r['status'] == 'قيد الانتظار').length.toString(),
                        Icons.hourglass_empty,
                        pendingColor,
                      ),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: _buildStatCard(
                        'موافق',
                        _leaveRequests.where((r) => r['status'] == 'موافق').length.toString(),
                        Icons.check_circle,
                        approvedColor,
                      ),
                    ),
                  ],
                ),
              ),

            // Filter chips
            Container(
              height: 55,
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 6),
              child: ListView(
                scrollDirection: Axis.horizontal,
                reverse: true,
                children: _filterOptions.map((filter) {
                  final isSelected = _selectedFilter == filter;
                  final color = filter == 'الكل' ? primaryBlue : _getStatusColor(filter);

                  return Padding(
                    padding: EdgeInsets.only(left: 8),
                    child: GestureDetector(
                      onTap: () {
                        setState(() => _selectedFilter = filter);
                        _loadLeaveRequests(filter: filter);
                      },
                      child: Container(
                        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                        decoration: BoxDecoration(
                          color: isSelected ? color : cardColor,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: isSelected ? color : Color(0xFFE5E7EB)),
                          boxShadow: isSelected
                              ? [BoxShadow(color: color.withValues(alpha: 0.3), blurRadius: 6, offset: Offset(0, 2))]
                              : null,
                        ),
                        child: Text(
                          filter,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: isSelected ? Colors.white : Colors.grey[600],
                          ),
                        ),
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),

            // List
            Expanded(
              child: _isLoading
                  ? Center(
                      child: CircularProgressIndicator(color: primaryBlue),
                    )
                  : _leaveRequests.isEmpty
                      ? _buildEmptyState()
                      : RefreshIndicator(
                          onRefresh: () => _loadLeaveRequests(filter: _selectedFilter),
                          color: primaryBlue,
                          child: ListView.separated(
                            padding: EdgeInsets.all(16),
                            itemCount: _leaveRequests.length,
                            separatorBuilder: (context, i) => SizedBox(height: 12),
                            itemBuilder: (context, index) {
                              return _buildLeaveCard(_leaveRequests[index]);
                            },
                          ),
                        ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatCard(String label, String value, IconData icon, Color color) {
    return Container(
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: color.withValues(alpha: 0.15)),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.03), blurRadius: 5, offset: Offset(0, 2))],
      ),
      child: Column(
        children: [
          Container(
            padding: EdgeInsets.all(8),
            decoration: BoxDecoration(color: color.withValues(alpha: 0.1), shape: BoxShape.circle),
            child: Icon(icon, color: color, size: 20),
          ),
          SizedBox(height: 6),
          Text(
            value,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue),
          ),
          SizedBox(height: 2),
          Text(
            label,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500]),
          ),
        ],
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(color: lightBlue, shape: BoxShape.circle),
            child: Icon(Icons.beach_access, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد طلبات إجازة',
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.grey[700]),
          ),
          SizedBox(height: 6),
          Text(
            'يمكنك تقديم طلب إجازة جديد من الصفحة الرئيسية',
            textAlign: TextAlign.center,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500]),
          ),
        ],
      ),
    );
  }

  Widget _buildLeaveCard(Map<String, dynamic> request) {
    final status = request['status'] ?? '';
    final statusColor = _getStatusColor(status);
    final statusIcon = _getStatusIcon(status);

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                Container(
                  padding: EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(statusIcon, color: statusColor, size: 24),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        request['leaveTypeName'] ?? 'إجازة',
                        style: TextStyle(fontFamily: 'Tajawal', fontSize: 15, fontWeight: FontWeight.bold, color: darkBlue),
                      ),
                      SizedBox(height: 2),
                      Text(
                        request['requestNumber'] ?? '',
                        style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                  ),
                  child: Text(
                    status,
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 11,
                      fontWeight: FontWeight.bold,
                      color: statusColor,
                    ),
                  ),
                ),
              ],
            ),

            SizedBox(height: 14),

            // Date range
            Container(
              padding: EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Color(0xFFF8FAFC),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Color(0xFFE5E7EB)),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: _buildDateItem('من', _formatDate(request['startDate']), Icons.calendar_today, approvedColor),
                  ),
                  Icon(Icons.arrow_forward, color: Colors.grey[400], size: 16),
                  Expanded(
                    child: _buildDateItem('إلى', _formatDate(request['endDate']), Icons.calendar_today, rejectedColor),
                  ),
                  Container(width: 1, height: 30, color: Color(0xFFE5E7EB)),
                  Padding(
                    padding: EdgeInsets.symmetric(horizontal: 8),
                    child: Column(
                      children: [
                        Text('${request['duration'] ?? 0}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue)),
                        Text('يوم', style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500])),
                      ],
                    ),
                  ),
                ],
              ),
            ),

            if (request['reason'] != null && request['reason'].toString().isNotEmpty) ...[
              SizedBox(height: 12),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline, size: 16, color: Colors.grey[400]),
                  SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      request['reason'].toString(),
                      style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, color: Colors.grey[500]),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ],

            SizedBox(height: 10),

            // Footer
            Row(
              children: [
                Icon(Icons.event, size: 13, color: Colors.grey[400]),
                SizedBox(width: 4),
                Text(
                  'تاريخ الطلب: ${_formatDate(request['requestDate'])}',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
                ),
                Spacer(),
                if (request['approvedByName'] != null) ...[
                  Icon(Icons.person_outline, size: 13, color: Colors.grey[400]),
                  SizedBox(width: 4),
                  Text(
                    request['approvedByName'].toString(),
                    style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
                  ),
                ],
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildDateItem(String label, String value, IconData icon, Color color) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Text(
          label,
          style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500]),
        ),
        SizedBox(height: 3),
        Text(
          value,
          textAlign: TextAlign.center,
          style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, fontWeight: FontWeight.bold, color: Colors.grey[700]),
        ),
      ],
    );
  }
}