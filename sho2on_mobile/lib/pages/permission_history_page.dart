import 'package:flutter/material.dart';
import '../services/permission_service.dart';

class PermissionHistoryPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const PermissionHistoryPage({super.key, required this.user});
  @override
  _PermissionHistoryPageState createState() => _PermissionHistoryPageState();
}

class _PermissionHistoryPageState extends State<PermissionHistoryPage> {
  final PermissionService _permissionService = PermissionService();

  List<dynamic> _permissions = [];
  bool _isLoading = false;
  String _selectedStatus = 'All';
  final List<String> _statusOptions = ['All', 'Pending', 'Approved', 'Rejected'];

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color pendingColor = Color(0xFFF59E0B);
  final Color approvedColor = Color(0xFF10B981);
  final Color rejectedColor = Color(0xFFEF4444);

  @override
  void initState() {
    super.initState();
    _loadPermissions();
  }

  Future<void> _loadPermissions() async {
    setState(() => _isLoading = true);

    try {
      final result = await _permissionService.getEmployeePermissions(
        widget.user['id'],
        status: _selectedStatus == 'All' ? null : _selectedStatus,
      );

      if (result['success'] && mounted) {
        setState(() {
          _permissions = result['data'] ?? [];
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('خطأ في تحميل الأذونات', textDirection: TextDirection.rtl),
            backgroundColor: rejectedColor,
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.all(16),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Approved':
        return approvedColor;
      case 'Pending':
        return pendingColor;
      case 'Rejected':
        return rejectedColor;
      default:
        return Colors.grey;
    }
  }

  String _getStatusText(String status) {
    switch (status) {
      case 'Pending':
        return 'قيد الانتظار';
      case 'Approved':
        return 'موافق';
      case 'Rejected':
        return 'مرفوض';
      default:
        return status;
    }
  }

  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'Approved':
        return Icons.check_circle;
      case 'Pending':
        return Icons.hourglass_empty;
      case 'Rejected':
        return Icons.cancel;
      default:
        return Icons.help;
    }
  }

  String _formatDateTime(dynamic dateTime) {
    if (dateTime == null) return '—';
    try {
      final dt = DateTime.parse(dateTime.toString());
      final period = dt.hour >= 12 ? 'م' : 'ص';
      final hour12 = dt.hour % 12 == 0 ? 12 : dt.hour % 12;
      return '${dt.day}/${dt.month}/${dt.year} - $hour12:${dt.minute.toString().padLeft(2, '0')} $period';
    } catch (e) {
      return dateTime.toString();
    }
  }

  String _formatDate(dynamic date) {
    if (date == null) return '—';
    try {
      final dt = DateTime.parse(date.toString());
      return '${dt.day}/${dt.month}/${dt.year}';
    } catch (e) {
      return date.toString();
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
                'سجل الأذونات',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              if (_permissions.isNotEmpty)
                Text(
                  '${_permissions.length} إذن',
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
            // Filter chips
            Container(
              height: 60,
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: ListView(
                scrollDirection: Axis.horizontal,
                reverse: true,
                children: _statusOptions.map((status) {
                  final isSelected = _selectedStatus == status;
                  final label = status == 'All' ? 'الكل' : _getStatusText(status);
                  final color = status == 'All' ? primaryBlue : _getStatusColor(status);

                  return Padding(
                    padding: EdgeInsets.only(left: 8),
                    child: GestureDetector(
                      onTap: () {
                        setState(() => _selectedStatus = status);
                        _loadPermissions();
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
                          label,
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

            // Permissions list
            Expanded(
              child: _isLoading
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          CircularProgressIndicator(color: primaryBlue),
                          SizedBox(height: 16),
                          Text('جاري تحميل الأذونات...', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500])),
                        ],
                      ),
                    )
                  : _permissions.isEmpty
                      ? _buildEmptyState()
                      : RefreshIndicator(
                          onRefresh: _loadPermissions,
                          color: primaryBlue,
                          child: ListView.separated(
                            padding: EdgeInsets.all(16),
                            itemCount: _permissions.length,
                            separatorBuilder: (context, i) => SizedBox(height: 12),
                            itemBuilder: (context, index) {
                              return _buildPermissionCard(_permissions[index]);
                            },
                          ),
                        ),
            ),
          ],
        ),
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
            child: Icon(Icons.access_time, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد أذونات مسجلة',
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.grey[700]),
          ),
          SizedBox(height: 6),
          Text(
            'لم يتم تسجيل أي إذن حتى الآن',
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500]),
          ),
        ],
      ),
    );
  }

  Widget _buildPermissionCard(Map<String, dynamic> permission) {
    final status = permission['status'] ?? '';
    final statusColor = _getStatusColor(status);
    final statusText = _getStatusText(status);
    final statusIcon = _getStatusIcon(status);
    final duration = permission['duration'] ?? 0;
    final deductedAmount = permission['deductedAmount'] ?? 0;
    final permissionType = permission['permissionType'] ?? 'إذن';

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
                        permission['permissionNumber'] ?? 'إذن',
                        style: TextStyle(fontFamily: 'Tajawal', fontSize: 15, fontWeight: FontWeight.bold, color: darkBlue),
                      ),
                      SizedBox(height: 2),
                      Text(
                        permissionType,
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
                    statusText,
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

            SizedBox(height: 16),

            // Time range
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
                    child: _buildTimeItem('من', _formatDateTime(permission['startDateTime']), Icons.login, approvedColor),
                  ),
                  Container(
                    width: 1,
                    height: 30,
                    color: Color(0xFFE5E7EB),
                  ),
                  Expanded(
                    child: _buildTimeItem('إلى', _formatDateTime(permission['endDateTime']), Icons.logout, rejectedColor),
                  ),
                ],
              ),
            ),

            SizedBox(height: 12),

            // Duration and deduction
            Row(
              children: [
                Expanded(
                  child: _buildInfoItem('المدة', '${duration.toStringAsFixed(1)} ساعة', Icons.timer, primaryBlue),
                ),
                SizedBox(width: 10),
                Expanded(
                  child: _buildInfoItem('قيمة الخصم', '${deductedAmount.toStringAsFixed(2)} ج', Icons.money_off, rejectedColor),
                ),
              ],
            ),

            if (permission['reason'] != null && permission['reason'].toString().isNotEmpty) ...[
              SizedBox(height: 12),
              Divider(height: 1, color: Color(0xFFE5E7EB)),
              SizedBox(height: 12),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline, size: 16, color: Colors.grey[400]),
                  SizedBox(width: 6),
                  Expanded(
                    child: Text(
                      permission['reason'].toString(),
                      style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, color: Colors.grey[500]),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                ],
              ),
            ],

            SizedBox(height: 8),

            // Footer
            Row(
              children: [
                Icon(Icons.calendar_today, size: 13, color: Colors.grey[400]),
                SizedBox(width: 4),
                Text(
                  'تاريخ الطلب: ${_formatDate(permission['createdAt'])}',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
                ),
                Spacer(),
                if (permission['approvedByName'] != null) ...[
                  Icon(Icons.person_outline, size: 13, color: Colors.grey[400]),
                  SizedBox(width: 4),
                  Text(
                    'المدير: ${permission['approvedByName']}',
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

  Widget _buildTimeItem(String label, String value, IconData icon, Color color) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, size: 14, color: color),
            SizedBox(width: 4),
            Text(
              label,
              style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500]),
            ),
          ],
        ),
        SizedBox(height: 4),
        Text(
          value,
          textAlign: TextAlign.center,
          style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, fontWeight: FontWeight.bold, color: Colors.grey[700]),
        ),
      ],
    );
  }

  Widget _buildInfoItem(String label, String value, IconData icon, Color color) {
    return Container(
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 14, color: color),
              SizedBox(width: 4),
              Text(
                label,
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500]),
              ),
            ],
          ),
          SizedBox(height: 4),
          Text(
            value,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: color),
          ),
        ],
      ),
    );
  }
}