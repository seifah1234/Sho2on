import 'package:flutter/material.dart';
import 'package:sho2on_mobile/pages/login_page.dart';
import '../../services/manager_service.dart';
import '../../utils/local_storage.dart';
import 'approve_loans_page.dart';
import 'approve_leaves_page.dart';
import 'approve_permissions_page.dart';
import 'team_reports_page.dart';

class ManagerDashboard extends StatefulWidget {
  final Map user;
  const ManagerDashboard(this.user, {super.key});

  @override
  State<ManagerDashboard> createState() => _ManagerDashboardState();
}

class _ManagerDashboardState extends State<ManagerDashboard> {
  final ManagerService _managerService = ManagerService();
  
  // إحصائيات الفريق
  Map<String, dynamic> _teamStats = {
    'totalEmployees': 0,
    'presentToday': 0,
    'onLeaveToday': 0,
    'lateToday': 0,
    'absentToday': 0,
    'pendingLoanApprovals': 0,
    'pendingLeaveApprovals': 0,
    'pendingPermissionApprovals': 0,
    'totalPendingApprovals': 0,
  };

  // قائمة الموظفين تحت إشراف المدير
  List<dynamic> _teamMembers = [];

  // قائمة الموظفين الذين عملوا CheckIn اليوم
  List<dynamic> _todayCheckIns = [];

  // الطلبات المعلقة
  Map<String, dynamic> _pendingApprovals = {
    'totalPending': 0,
    'pendingLoans': 0,
    'pendingLeaves': 0,
    'pendingPermissions': 0,
    'items': [],
  };

  // حالة التحميل
  bool _isLoading = false;

  // ألوان التصميم
  final Color primaryColor = Color(0xFF673AB7);
  final Color secondaryColor = Color(0xFF9C27B0);
  final Color accentColor = Color(0xFF2196F3);
  final Color successColor = Color(0xFF4CAF50);
  final Color warningColor = Color(0xFFFF9800);
  final Color errorColor = Color(0xFFF44336);
  final Color backgroundColor = Color(0xFFF5F7FA);

  @override
  void initState() {
    super.initState();
    _loadTeamData();
  }

  Future<void> _loadTeamData() async {
    setState(() => _isLoading = true);
    
    try {
      await Future.wait([
        _loadTeamStats(),
        _loadTeamMembers(),
        _loadTodayCheckIns(),
        _loadPendingApprovals(),
      ]);
      
    } catch (e) {
      print('Error loading team data: $e');
      _showError('فشل في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadTeamStats() async {
    try {
      final result = await _managerService.getManagerTeamStats(
        managerId: widget.user['id'],
      );
      
      if (result['success']) {
        setState(() {
          _teamStats = result['data'];
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل الإحصائيات');
      }
    } catch (e) {
      print('Error loading team stats: $e');
    }
  }

  Future<void> _loadTeamMembers() async {
    try {
      final result = await _managerService.getManagerTeamMembers(
        managerId: widget.user['id'],
      );
      
      if (result['success']) {
        setState(() {
          _teamMembers = result['data'] ?? [];
        });
      } else {
        print('Error loading team members: ${result['message']}');
      }
    } catch (e) {
      print('Error loading team members: $e');
    }
  }

  Future<void> _loadTodayCheckIns() async {
    try {
      final result = await _managerService.getTodayCheckIns(
        managerId: widget.user['id'],
      );
      
      if (result['success']) {
        setState(() {
          _todayCheckIns = result['data'] ?? [];
        });
      } else {
        print('Error loading today check-ins: ${result['message']}');
      }
    } catch (e) {
      print('Error loading today check-ins: $e');
    }
  }

  Future<void> _loadPendingApprovals() async {
    try {
      final result = await _managerService.getPendingApprovals(
        managerId: widget.user['id'],
      );
      
      if (result['success']) {
        setState(() {
          _pendingApprovals = result['data'] ?? {};
        });
      } else {
        print('Error loading pending approvals: ${result['message']}');
      }
    } catch (e) {
      print('Error loading pending approvals: $e');
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
        ),
        backgroundColor: errorColor,
        duration: Duration(seconds: 3),
      ),
    );
  }

  // دوال التنقل (نفسها موجودة)

  Widget _buildManagerHeader() {
    return Container(
      padding: EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [primaryColor, secondaryColor],
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
        ),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(24),
          bottomRight: Radius.circular(24),
        ),
        boxShadow: [
          BoxShadow(
            color: primaryColor.withValues(alpha: 0.3),
            blurRadius: 15,
            offset: Offset(0, 5),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            textDirection: TextDirection.rtl,
            children: [
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(
                    'مرحباً',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontSize: 16,
                      color: Colors.white70,
                      fontFamily: 'Tajawal',
                    ),
                  ),
                  SizedBox(height: 5),
                  Text(
                    widget.user['fullName'] ?? '',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                      fontFamily: 'Tajawal',
                    ),
                  ), ],
              ),
              CircleAvatar(
                radius: 30,
                backgroundColor: Colors.white,
                child: Icon(
                  Icons.admin_panel_settings,
                  size: 35,
                  color: primaryColor,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildTeamStats() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      margin: EdgeInsets.all(16),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'إحصائيات الفريق',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Icon(Icons.groups, color: primaryColor),
              ],
            ),
            SizedBox(height: 16),
            GridView(
              shrinkWrap: true,
              physics: NeverScrollableScrollPhysics(),
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                crossAxisSpacing: 5,
                mainAxisSpacing: 5,
                childAspectRatio: 0.9,
              ),
              children: [
                _buildStatCard(
                  'إجمالي الموظفين',
                  _teamStats['totalEmployees'].toString(),
                  Icons.people,
                  primaryColor,
                ),
                _buildStatCard(
                  'حاضرون اليوم',
                  _teamStats['presentToday'].toString(),
                  Icons.check_circle,
                  successColor,
                ),
                _buildStatCard(
                  'في إجازة',
                  _teamStats['onLeaveToday'].toString(),
                  Icons.beach_access,
                  warningColor,
                ),
                _buildStatCard(
                  'متأخرون',
                  _teamStats['lateToday'].toString(),
                  Icons.schedule,
                  errorColor,
                ),
                _buildStatCard(
                  'غياب',
                  _teamStats['absentToday'].toString(),
                  Icons.person_off,
                  Colors.grey,
                ),
                _buildStatCard(
                  'طلبات بانتظارك',
                  (_teamStats['totalPendingApprovals'] ?? 0).toString(),
                  Icons.pending_actions,
                  accentColor,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatCard(String title, String value, IconData icon, Color color) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.2), width: 1.5),
        boxShadow: [
          BoxShadow(
            color: Colors.black12,
            blurRadius: 6,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(2.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.center,
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(icon, color: color, size: 18),
                SizedBox(width: 4),
                Text(
                  title,
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 10,
                    color: Colors.grey[600],
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
            SizedBox(height: 6),
            Text(
              value,
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 20,
                fontWeight: FontWeight.bold,
                color: color,
                fontFamily: 'Tajawal',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTodayCheckIns() {
    if (_todayCheckIns.isEmpty) {
      return Card(
        elevation: 3,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
        margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                textDirection: TextDirection.rtl,
                children: [
                  Text(
                    'الحضور اليوم',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: primaryColor,
                      fontFamily: 'Tajawal',
                    ),
                  ),
                  Icon(Icons.check_circle_outline, color: primaryColor),
                ],
              ),
              SizedBox(height: 16),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Column(
                  children: [
                    Icon(
                      Icons.timelapse,
                      size: 50,
                      color: Colors.grey[300],
                    ),
                    SizedBox(height: 8),
                    Text(
                      'لم يتم تسجيل أي حضور حتى الآن',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        color: Colors.grey[600],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      );
    }

    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'الحضور اليوم (${_todayCheckIns.length})',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Icon(Icons.check_circle_outline, color: primaryColor),
              ],
            ),
            SizedBox(height: 16),
            ..._todayCheckIns.take(5).map((checkIn) => _buildCheckInCard(checkIn)),
            if (_todayCheckIns.length > 5)
              TextButton.icon(
                onPressed: () {
                  // يمكن إنشاء صفحة لعرض جميع الحضور
                },
                icon: Icon(Icons.arrow_left, size: 16),
                label: Text(
                  'عرض المزيد (${_todayCheckIns.length - 5})',
                  style: TextStyle(fontSize: 12),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildCheckInCard(Map<String, dynamic> checkIn) {
    bool isLate = checkIn['lateMinutes'] > 0;
    String checkInTime = _formatTime(checkIn['checkInTime']);
    String status = checkIn['status'] ?? 'حاضر';
    
    return Container(
      margin: EdgeInsets.only(bottom: 12),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isLate ? errorColor.withValues(alpha: 0.3) : successColor.withValues(alpha: 0.3),
          width: 1.5,
        ),
        boxShadow: [
          BoxShadow(
            color: Colors.black12,
            blurRadius: 3,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        textDirection: TextDirection.rtl,
        children: [
          CircleAvatar(
            radius: 20,
            backgroundColor: isLate ? errorColor.withValues(alpha: 0.1) : successColor.withValues(alpha: 0.1),
            child: Icon(
              isLate ? Icons.schedule : Icons.check_circle,
              size: 20,
              color: isLate ? errorColor : successColor,
            ),
          ),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  checkIn['employeeName'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'Tajawal',
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  checkIn['departmentName'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
          ),
          SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Icon(
                    Icons.login,
                    size: 14,
                    color: Colors.grey[600],
                  ),
                  SizedBox(width: 4),
                  Text(
                    checkInTime,
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: isLate ? errorColor : successColor,
                    ),
                  ),
                ],
              ),
              SizedBox(height: 4),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: isLate ? errorColor.withValues(alpha: 0.1) : successColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  status,
                  style: TextStyle(
                    fontSize: 10,
                    color: isLate ? errorColor : successColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              if (isLate && checkIn['lateMinutes'] > 0)
                SizedBox(height: 4),
              if (isLate && checkIn['lateMinutes'] > 0)
                Text(
                  '${checkIn['lateMinutes']} دقيقة',
                  style: TextStyle(
                    fontSize: 10,
                    color: errorColor,
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  String _formatTime(String timeString) {
    try {
      final time = DateTime.parse(timeString);
      return '${time.hour.toString().padLeft(2, '0')}:${time.minute.toString().padLeft(2, '0')}';
    } catch (e) {
      return timeString;
    }
  }

  Widget _buildQuickActions() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'إجراءات سريعة',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Icon(Icons.bolt, color: primaryColor),
              ],
            ),
            SizedBox(height: 16),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              alignment: WrapAlignment.center,
              children: [
                _buildActionButton(
                  'اعتماد السلف',
                  Icons.account_balance,
                  Colors.blue,
                  _navigateToApproveLoans,
                  badge: _pendingApprovals['pendingLoans'] ?? 0,
                ),
                _buildActionButton(
                  'اعتماد الإجازات',
                  Icons.beach_access,
                  Colors.green,
                  _navigateToApproveLeaves,
                  badge: _pendingApprovals['pendingLeaves'] ?? 0,
                ),
                _buildActionButton(
                  'اعتماد الإذن',
                  Icons.access_time,
                  Colors.orange,
                  _navigateToApprovePermissions,
                  badge: _pendingApprovals['pendingPermissions'] ?? 0,
                ),
                /*_buildActionButton(
                  'تقارير الفريق',
                  Icons.analytics,
                  Colors.purple,
                  _navigateToTeamReports,
                ),*/
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildActionButton(String title, IconData icon, Color color, VoidCallback onTap, {int badge = 0}) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 100,
        height: 100,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: color.withValues(alpha: 0.3), width: 1.5),
          boxShadow: [
            BoxShadow(
              color: color.withValues(alpha: 0.1),
              blurRadius: 6,
              offset: Offset(0, 3),
            ),
          ],
        ),
        child: Stack(
          alignment: Alignment.center,
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.center,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(icon, color: color, size: 22),
                ),
                SizedBox(height: 8),
                Text(
                  title,
                  textDirection: TextDirection.rtl,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: Colors.black87,
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
            if (badge > 0)
              Positioned(
                top: 8,
                left: 8,
                child: Container(
                  padding: EdgeInsets.all(4),
                  decoration: BoxDecoration(
                    color: Colors.red,
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    badge.toString(),
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildTeamMembers() {
    if (_teamMembers.isEmpty) {
      return SizedBox.shrink();
    }
    
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'أعضاء الفريق (${_teamMembers.length})',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Icon(Icons.group, color: primaryColor),
              ],
            ),
            SizedBox(height: 16),
            ..._teamMembers.take(3).map((member) => _buildTeamMemberCard(member)),
            if (_teamMembers.length > 3)
              TextButton.icon(
                onPressed: () {
                  // يمكن إنشاء صفحة لعرض جميع الأعضاء
                },
                icon: Icon(Icons.arrow_left, size: 16),
                label: Text(
                  'عرض جميع الأعضاء (${_teamMembers.length})',
                  style: TextStyle(fontSize: 12),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildTeamMemberCard(Map<String, dynamic> member) {
    String status = member['status'] ?? 'لم يحضر';
    String statusColorHex = member['statusColor'] ?? '#9E9E9E';
    Color statusColor = _hexToColor(statusColorHex);
    String statusIcon = member['statusIcon'] ?? 'help';
    
    return Container(
      margin: EdgeInsets.only(bottom: 12),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.grey[200]!),
        boxShadow: [
          BoxShadow(
            color: Colors.black12,
            blurRadius: 3,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Row(
        textDirection: TextDirection.rtl,
        children: [
          CircleAvatar(
            radius: 20,
            backgroundColor: primaryColor.withValues(alpha: 0.1),
            child: Text(
              member['fullName'].isNotEmpty ? member['fullName'][0] : '?',
              style: TextStyle(
                color: primaryColor,
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  member['fullName'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'Tajawal',
                  ),
                ),
                SizedBox(height: 4),
                Text(
                  member['jobTitleName'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                    fontFamily: 'Tajawal',
                  ),
                ),
                if (member['checkInTime'] != null)
                  SizedBox(height: 4),
                if (member['checkInTime'] != null)
                  Text(
                    'الحضور: ${_formatTime(member['checkInTime'])}',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontSize: 10,
                      color: Colors.grey[500],
                    ),
                  ),
              ],
            ),
          ),
          SizedBox(width: 12),
          Column(
            children: [
              Icon(
                _getStatusIcon(statusIcon),
                color: statusColor,
                size: 20,
              ),
              SizedBox(height: 4),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: statusColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  status,
                  style: TextStyle(
                    fontSize: 10,
                    color: statusColor,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  IconData _getStatusIcon(String iconName) {
    switch (iconName) {
      case 'check_circle':
        return Icons.check_circle;
      case 'beach_access':
        return Icons.beach_access;
      case 'schedule':
        return Icons.schedule;
      case 'help':
        return Icons.help;
      default:
        return Icons.help;
    }
  }

  Color _hexToColor(String hex) {
    hex = hex.replaceAll('#', '');
    if (hex.length == 6) {
      hex = 'FF$hex';
    }
    return Color(int.parse(hex, radix: 16));
  }

  Widget _buildPendingTasks() {
    int totalPending = _pendingApprovals['totalPending'] ?? 0;
    
    if (totalPending == 0) {
      return SizedBox.shrink();
    }

    List<dynamic> items = _pendingApprovals['items'] ?? [];
    
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'طلبات تحتاج الموافقة ($totalPending)',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Badge(
                  label: Text(totalPending.toString()),
                  backgroundColor: Colors.red,
                  child: Icon(Icons.notifications_active, color: primaryColor),
                ),
              ],
            ),
            SizedBox(height: 16),
            ...items.take(3).map((item) => _buildPendingItemCard(item)),
            if (items.length > 3)
              TextButton.icon(
                onPressed: () {
                  _navigateToAppropriatePage(items.isNotEmpty ? items[0]['type'] : 'سلفة');
                },
                icon: Icon(Icons.arrow_left, size: 16),
                label: Text(
                  'عرض جميع الطلبات (${items.length})',
                  style: TextStyle(fontSize: 12),
                ),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildPendingItemCard(Map<String, dynamic> item) {
    String type = item['type'] ?? '';
    Color color;
    
    switch (type) {
      case 'سلفة':
        color = Colors.blue;
        break;
      case 'إجازة':
        color = Colors.green;
        break;
      case 'إذن':
        color = Colors.orange;
        break;
      default:
        color = primaryColor;
    }
    
    return Container(
      margin: EdgeInsets.only(bottom: 12),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.05),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1.5),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            textDirection: TextDirection.rtl,
            children: [
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text(
                  type,
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 10,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              Text(
                item['employeeName'] ?? '',
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
            ],
          ),
          SizedBox(height: 8),
          Text(
            item['details'] ?? '',
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 12,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 8),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              ElevatedButton(
                onPressed: () => _navigateToAppropriatePage(type),
                style: ElevatedButton.styleFrom(
                  backgroundColor: color,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                ),
                child: Text(
                  'مراجعة',
                  style: TextStyle(fontSize: 12),
                ),
              ),
              Text(
                _formatDate(item['requestDate'] ?? ''),
                style: TextStyle(
                  fontSize: 10,
                  color: Colors.grey[500],
                ),
              ),
            ],
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

  void _navigateToAppropriatePage(String type) {
    switch (type) {
      case 'سلفة':
        _navigateToApproveLoans();
        break;
      case 'إجازة':
        _navigateToApproveLeaves();
        break;
      case 'إذن':
        _navigateToApprovePermissions();
        break;
      default:
        _navigateToApproveLoans();
    }
  }

  // دوال التنقل (نفسها)
  void _navigateToApproveLoans() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ApproveLoansPage(user: widget.user),
      ),
    );
  }

  void _navigateToLogin() async {
    await Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (context) => LoginPage(),
      ),
    );
  }

  void _navigateToApproveLeaves() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ApproveHolidaysPage(user: widget.user),
      ),
    );
  }

  void _navigateToApprovePermissions() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ApprovePermissionsPage(user: widget.user),
      ),
    );
  }

  void _navigateToTeamReports() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => TeamReportsPage(user: widget.user),
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
            'لوحة المدير',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontFamily: 'Tajawal',
            ),
          ),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
          actions: [
            IconButton(
              icon: Icon(Icons.refresh),
              onPressed: _isLoading ? null : _loadTeamData,
            ),
            IconButton(
              icon: Icon(Icons.logout),
              onPressed: () async {
                await LocalStorage.clearUser();
                _navigateToLogin();
              },
            ),
          ],
        ),
        body: _isLoading
            ? Center(
                child: CircularProgressIndicator(
                  color: primaryColor,
                ),
              )
            : RefreshIndicator(
                onRefresh: _loadTeamData,
                child: SingleChildScrollView(
                  child: Column(
                    children: [
                      // الهيدر
                      _buildManagerHeader(),
                      
                      // إحصائيات الفريق
                      _buildTeamStats(),
                      
                      // الحضور اليوم
                      _buildTodayCheckIns(),
                      
                      // إجراءات سريعة
                      _buildQuickActions(),
                      
                      // أعضاء الفريق
                      _buildTeamMembers(),
                      
                      // طلبات تحتاج الموافقة
                      _buildPendingTasks(),
                      
                      // مساحة إضافية
                      SizedBox(height: 30),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}