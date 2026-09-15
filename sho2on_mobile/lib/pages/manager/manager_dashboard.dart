import 'package:flutter/material.dart';
import 'package:sho2on_mobile/pages/login_page.dart';
import '../../services/manager_service.dart';
import '../../utils/local_storage.dart';
import 'approve_loans_page.dart';
import 'approve_leaves_page.dart';
import 'approve_permissions_page.dart';
import 'team_reports_page.dart';
import 'team_location_page.dart';
import '../main_page.dart'; // ✅ إضافة import للصفحة الرئيسية

class ManagerDashboard extends StatefulWidget {
  final Map user;
  const ManagerDashboard(this.user, {super.key});

  @override
  State<ManagerDashboard> createState() => _ManagerDashboardState();
}

class _ManagerDashboardState extends State<ManagerDashboard> {
  final ManagerService _managerService = ManagerService();

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

  List<dynamic> _teamMembers = [];
  List<dynamic> _todayCheckIns = [];

  Map<String, dynamic> _pendingApprovals = {
    'totalPending': 0,
    'pendingLoans': 0,
    'pendingLeaves': 0,
    'pendingPermissions': 0,
    'items': [],
  };

  bool _isLoading = false;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color successColor = Color(0xFF10B981);
  final Color warningColor = Color(0xFFF59E0B);
  final Color errorColor = Color(0xFFEF4444);
  final Color purpleColor = Color(0xFF8B5CF6);

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
      _showError('فشل في تحميل البيانات');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadTeamStats() async {
    try {
      final result = await _managerService.getManagerTeamStats(
        managerId: widget.user['id'],
      );
      if (result['success'] && mounted) {
        setState(() => _teamStats = result['data']);
      }
    } catch (e) {}
  }

  Future<void> _loadTeamMembers() async {
    try {
      final result = await _managerService.getManagerTeamMembers(
        managerId: widget.user['id'],
      );
      if (result['success'] && mounted) {
        setState(() => _teamMembers = result['data'] ?? []);
      }
    } catch (e) {}
  }

  Future<void> _loadTodayCheckIns() async {
    try {
      final result = await _managerService.getTodayCheckIns(
        managerId: widget.user['id'],
      );
      if (result['success'] && mounted) {
        setState(() => _todayCheckIns = result['data'] ?? []);
      }
    } catch (e) {}
  }

  Future<void> _loadPendingApprovals() async {
    try {
      final result = await _managerService.getPendingApprovals(
        managerId: widget.user['id'],
      );
      if (result['success'] && mounted) {
        setState(() => _pendingApprovals = result['data'] ?? {});
      }
    } catch (e) {}
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, textDirection: TextDirection.rtl),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
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

  String _formatDate(String dateString) {
    try {
      final date = DateTime.parse(dateString);
      return '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}';
    } catch (e) {
      return dateString;
    }
  }

  Color _hexToColor(String hex) {
    hex = hex.replaceAll('#', '');
    if (hex.length == 6) hex = 'FF$hex';
    return Color(int.parse(hex, radix: 16));
  }

  IconData _getStatusIcon(String iconName) {
    switch (iconName) {
      case 'check_circle':
        return Icons.check_circle;
      case 'beach_access':
        return Icons.beach_access;
      case 'schedule':
        return Icons.schedule;
      default:
        return Icons.help;
    }
  }

  // ==================== HEADER ====================
  Widget _buildHeader() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(24),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [darkBlue, primaryBlue],
        ),
        borderRadius: BorderRadius.circular(24),
        boxShadow: [
          BoxShadow(
            color: primaryBlue.withValues(alpha: 0.3),
            blurRadius: 20,
            offset: Offset(0, 8),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 50,
                height: 50,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.2),
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Colors.white.withValues(alpha: 0.3),
                    width: 2,
                  ),
                ),
                child: Icon(
                  Icons.admin_panel_settings,
                  color: Colors.white,
                  size: 28,
                ),
              ),
              SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'مرحباً بك 👋',
                      style: TextStyle(
                        fontSize: 13,
                        color: Colors.white.withValues(alpha: 0.8),
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      widget.user['fullName'] ?? '',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 2),
                    Text(
                      widget.user['jobTitle'] ?? '',
                      style: TextStyle(
                        fontSize: 11,
                        color: Colors.white.withValues(alpha: 0.6),
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          SizedBox(height: 20),
          Row(
            children: [
              _buildHeaderAction(Icons.home, 'الرئيسية', _navigateToMainPage),
              SizedBox(width: 10),
              _buildHeaderAction(Icons.refresh, 'تحديث', _loadTeamData),
              SizedBox(width: 10),
              _buildHeaderAction(Icons.logout, 'خروج', _logout),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderAction(IconData icon, String label, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: 0.15),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: Colors.white.withValues(alpha: 0.2)),
        ),
        child: Row(
          children: [
            Icon(icon, color: Colors.white, size: 16),
            SizedBox(width: 6),
            Text(
              label,
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontSize: 12,
                color: Colors.white,
                fontWeight: FontWeight.w600,
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ==================== STATS ====================
  Widget _buildTeamStats() {
    final stats = [
      {
        'label': 'إجمالي الموظفين',
        'value': _teamStats['totalEmployees'] ?? 0,
        'icon': Icons.people,
        'color': primaryBlue,
      },
      {
        'label': 'حاضرون اليوم',
        'value': _teamStats['presentToday'] ?? 0,
        'icon': Icons.check_circle,
        'color': successColor,
      },
      {
        'label': 'في إجازة',
        'value': _teamStats['onLeaveToday'] ?? 0,
        'icon': Icons.beach_access,
        'color': warningColor,
      },
      {
        'label': 'متأخرون',
        'value': _teamStats['lateToday'] ?? 0,
        'icon': Icons.schedule,
        'color': errorColor,
      },
      {
        'label': 'غياب',
        'value': _teamStats['absentToday'] ?? 0,
        'icon': Icons.person_off,
        'color': Colors.grey,
      },
      {
        'label': 'طلبات معلقة',
        'value': _teamStats['totalPendingApprovals'] ?? 0,
        'icon': Icons.pending_actions,
        'color': purpleColor,
      },
    ];

    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: lightBlue,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(Icons.groups, color: primaryBlue, size: 18),
              ),
              SizedBox(width: 10),
              Text(
                'إحصائيات الفريق',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                ),
              ),
            ],
          ),
          SizedBox(height: 16),
          GridView(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 3,
              crossAxisSpacing: 10,
              mainAxisSpacing: 10,
              childAspectRatio: 1.05,
            ),
            children: stats.map((stat) {
              final color = stat['color'] as Color;
              return Container(
                padding: EdgeInsets.all(2),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.05),
                  borderRadius: BorderRadius.circular(14),
                  border: Border.all(color: color.withValues(alpha: 0.15)),
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      padding: EdgeInsets.all(2),
                      decoration: BoxDecoration(
                        color: color.withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        stat['icon'] as IconData,
                        color: color,
                        size: 18,
                      ),
                    ),
                    SizedBox(height: 3),
                    Text(
                      '${stat['value']}',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: darkBlue,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 2),
                    Text(
                      stat['label']!,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontSize: 9,
                        color: Colors.grey[500],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              );
            }).toList(),
          ),
        ],
      ),
    );
  }

  // ==================== QUICK ACTIONS ====================
  Widget _buildQuickActions() {
    final actions = [
      {
        'title': 'اعتماد السلف',
        'icon': Icons.account_balance,
        'color': primaryBlue,
        'badge': _pendingApprovals['pendingLoans'] ?? 0,
        'onTap': _navigateToApproveLoans,
      },
      {
        'title': 'اعتماد الإجازات',
        'icon': Icons.beach_access,
        'color': successColor,
        'badge': _pendingApprovals['pendingLeaves'] ?? 0,
        'onTap': _navigateToApproveLeaves,
      },
      {
        'title': 'اعتماد الأذونات',
        'icon': Icons.access_time,
        'color': warningColor,
        'badge': _pendingApprovals['pendingPermissions'] ?? 0,
        'onTap': _navigateToApprovePermissions,
      },
      {
        'title': 'تتبع الموظفين',
        'icon': Icons.location_on,
        'color': purpleColor,
        'badge': 0,
        'onTap': _navigateToTeamLocation,
      },
    ];

    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: lightBlue,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(Icons.bolt, color: primaryBlue, size: 18),
              ),
              SizedBox(width: 10),
              Text(
                'إجراءات سريعة',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                ),
              ),
            ],
          ),
          SizedBox(height: 16),
          Column(
            children: [
              for (int i = 0; i < actions.length; i += 3)
                Padding(
                  padding: EdgeInsets.only(
                    bottom: i + 3 < actions.length ? 10 : 0,
                  ),
                  child: Row(
                    children: actions
                        .sublist(
                          i,
                          i + 3 > actions.length ? actions.length : i + 3,
                        )
                        .map((action) => _buildQuickActionCard(action))
                        .toList(),
                  ),
                ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildQuickActionCard(Map<String, dynamic> action) {
    final color = action['color'] as Color;
    final badge = action['badge'] as int;
    return Expanded(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 4),
        child: GestureDetector(
          onTap: action['onTap'] as VoidCallback,
          child: Container(
            padding: EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.05),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: color.withValues(alpha: 0.15)),
            ),
            child: Stack(
              alignment: Alignment.center,
              children: [
                Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Container(
                      width: 40,
                      height: 40,
                      decoration: BoxDecoration(
                        color: color.withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        action['icon'] as IconData,
                        color: color,
                        size: 20,
                      ),
                    ),
                    SizedBox(height: 6),
                    Text(
                      action['title']!,
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        fontFamily: 'Tajawal',
                        fontSize: 11,
                        fontWeight: FontWeight.bold,
                        color: Colors.grey[700],
                      ),
                    ),
                  ],
                ),
                if (badge > 0)
                  Positioned(
                    top: 2,
                    right: 2,
                    child: Container(
                      padding: EdgeInsets.all(4),
                      decoration: BoxDecoration(
                        color: errorColor,
                        shape: BoxShape.circle,
                      ),
                      child: Text(
                        '$badge',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 9,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  // ==================== TODAY CHECK-INS ====================
  Widget _buildTodayCheckIns() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: lightBlue,
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(Icons.login, color: primaryBlue, size: 18),
              ),
              SizedBox(width: 10),
              Text(
                'الحضور اليوم (${_todayCheckIns.length})',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                ),
              ),
            ],
          ),
          SizedBox(height: 16),
          if (_todayCheckIns.isEmpty)
            Center(
              child: Padding(
                padding: EdgeInsets.all(20),
                child: Text(
                  'لا يوجد حضور اليوم',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    color: Colors.grey[500],
                  ),
                ),
              ),
            )
          else
            ..._todayCheckIns
                .take(5)
                .map((checkIn) => _buildCheckInCard(checkIn)),
        ],
      ),
    );
  }

  Widget _buildCheckInCard(Map<String, dynamic> checkIn) {
    final isLate = checkIn['lateMinutes'] > 0;
    final checkInTime = _formatTime(checkIn['checkInTime'] ?? '');
    final status = checkIn['status'] ?? 'حاضر';

    return Container(
      margin: EdgeInsets.only(bottom: 10),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isLate
              ? errorColor.withValues(alpha: 0.2)
              : successColor.withValues(alpha: 0.2),
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 36,
            height: 36,
            decoration: BoxDecoration(
              color: isLate
                  ? errorColor.withValues(alpha: 0.1)
                  : successColor.withValues(alpha: 0.1),
              shape: BoxShape.circle,
            ),
            child: Icon(
              isLate ? Icons.schedule : Icons.check_circle,
              color: isLate ? errorColor : successColor,
              size: 18,
            ),
          ),
          SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  checkIn['employeeName'] ?? '',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: Colors.grey[800],
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  checkIn['departmentName'] ?? '',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 11,
                    color: Colors.grey[500],
                  ),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                checkInTime,
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                  color: isLate ? errorColor : successColor,
                ),
              ),
              SizedBox(height: 2),
              Text(
                status,
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 10,
                  color: isLate ? errorColor : successColor,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  // ==================== PENDING TASKS ====================
  Widget _buildPendingTasks() {
    final totalPending = _pendingApprovals['totalPending'] ?? 0;
    if (totalPending == 0) return SizedBox.shrink();

    final items = _pendingApprovals['items'] ?? [];

    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: errorColor.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(
                  Icons.notifications_active,
                  color: errorColor,
                  size: 18,
                ),
              ),
              SizedBox(width: 10),
              Text(
                'طلبات تحتاج موافقة ($totalPending)',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                ),
              ),
            ],
          ),
          SizedBox(height: 16),
          ...items.take(3).map((item) => _buildPendingItemCard(item)),
        ],
      ),
    );
  }

  Widget _buildPendingItemCard(Map<String, dynamic> item) {
    final type = item['type'] ?? '';
    final Color color;
    switch (type) {
      case 'سلفة':
        color = primaryBlue;
        break;
      case 'إجازة':
        color = successColor;
        break;
      case 'إذن':
        color = warningColor;
        break;
      default:
        color = purpleColor;
    }

    return Container(
      margin: EdgeInsets.only(bottom: 10),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.05),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: color.withValues(alpha: 0.15)),
      ),
      child: Row(
        children: [
          Container(
            padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(12),
            ),
            child: Text(
              type,
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontSize: 10,
                fontWeight: FontWeight.bold,
                color: Colors.white,
              ),
            ),
          ),
          SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item['employeeName'] ?? '',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                    color: Colors.grey[800],
                  ),
                ),
                SizedBox(height: 2),
                Text(
                  item['details'] ?? '',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 11,
                    color: Colors.grey[500],
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_left, color: color, size: 20),
        ],
      ),
    );
  }

  // ==================== NAVIGATION ====================
  void _navigateToMainPage() {
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => MainPage(widget.user)),
    );
  }

  void _logout() async {
    await LocalStorage.clearUser();
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => LoginPage()),
    );
  }

  void _navigateToApproveLoans() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => ApproveLoansPage(user: widget.user)),
    );
    _loadTeamData();
  }

  void _navigateToApproveLeaves() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => ApproveHolidaysPage(user: widget.user)),
    );
    _loadTeamData();
  }

  void _navigateToApprovePermissions() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => ApprovePermissionsPage(user: widget.user),
      ),
    );
    _loadTeamData();
  }

  void _navigateToTeamReports() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => TeamReportsPage(user: widget.user)),
    );
  }

  void _navigateToTeamLocation() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (_) => TeamLocationPage(user: widget.user)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        body: SafeArea(
          child: _isLoading
              ? Center(child: CircularProgressIndicator(color: primaryBlue))
              : RefreshIndicator(
                  onRefresh: _loadTeamData,
                  color: primaryBlue,
                  child: SingleChildScrollView(
                    padding: EdgeInsets.all(16),
                    child: Column(
                      children: [
                        _buildHeader(),
                        _buildTeamStats(),
                        _buildQuickActions(),
                        _buildTodayCheckIns(),
                        _buildPendingTasks(),
                        SizedBox(height: 20),
                      ],
                    ),
                  ),
                ),
        ),
      ),
    );
  }
}
