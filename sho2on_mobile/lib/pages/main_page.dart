import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:sho2on_mobile/pages/announcements_page.dart';
import 'package:sho2on_mobile/pages/manager/manager_dashboard.dart';
import '../services/attendance_service.dart';
import '../utils/local_storage.dart';
import '../services/location_service.dart';
import '../services/holiday_service.dart';
import '../services/loan_service.dart';
import 'holiday_request_page.dart';
import 'leave_history_page.dart';
import 'loan_request_page.dart';
import 'loan_history_page.dart';
import 'monthly_report_page.dart';
import 'permission_request_page.dart';
import 'permission_history_page.dart';
import 'chat_list_page.dart';
import 'break_page.dart';
import 'login_page.dart';
import 'task_page.dart';

class MainPage extends StatefulWidget {
  final Map user;
  const MainPage(this.user, {super.key});

  @override
  State<MainPage> createState() => _MainPageState();
}

class _MainPageState extends State<MainPage> {
  final AttendanceService _attendance = AttendanceService();
  final HolidayService _holidayService = HolidayService();
  final LoanService _loanService = LoanService();

  String checkIn = '--:--';
  String checkOut = '--:--';
  String statusText = 'غير مسجل';
  bool _showAttendanceOptions = false;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF1F5F9);
  final Color cardColor = Colors.white;
  final Color greenColor = Color(0xFF10B981);
  final Color orangeColor = Color(0xFFF59E0B);
  final Color redColor = Color(0xFFEF4444);
  final Color purpleColor = Color(0xFF8B5CF6);

  Map<String, dynamic> _leaveStats = {
    'balance': 0,
    'used': 0,
    'pending': 0,
    'approved': 0,
  };
  Map<String, dynamic> _loanStats = {
    'currentBalance': 0,
    'maxAllowed': 0,
    'activeLoans': 0,
    'nextInstallment': 0,
  };

  @override
  void initState() {
    super.initState();
    _initData();
  }

  void _navigateToTasks() async {
    await Navigator.push(
      context,
      MaterialPageRoute(builder: (context) => TasksPage(user: widget.user)),
    );
  }

  Future<void> _initData() async {
    setState(() {
      checkOut = widget.user['today']['checkOut'] ?? '--:--';
      checkIn = widget.user['today']['checkIn'] ?? '--:--';
      statusText = widget.user['today']['status'] ?? 'غير مسجل';
    });
    await _loadLeaveStats();
    await _loadLoanStats();
  }

  Future<void> _loadLeaveStats() async {
    try {
      final result = await _holidayService.getEmployeeRequests(
        widget.user['id'],
      );
      if (result['success'] && result['data'] != null) {
        final requests = result['data'] as List;
        setState(() {
          _leaveStats = {
            'balance': 21,
            'used': 5,
            'pending': requests
                .where((r) => r['status'] == 'قيد الانتظار')
                .length,
            'approved': requests.where((r) => r['status'] == 'موافق').length,
          };
        });
      }
    } catch (e) {}
  }

  Future<void> _loadLoanStats() async {
    try {
      final result = await _loanService.getEmployee(widget.user['id']);
      if (result['success'] && result['data'] != null) {
        final data = result['data'];
        setState(() {
          _loanStats = {
            'currentBalance': (data['currentLoanBalance'] ?? 0).toDouble(),
            'maxAllowed': (data['maxAllowedAmount'] ?? 0).toDouble(),
            'activeLoans': 0,
            'nextInstallment': 0,
          };
        });
      }
    } catch (e) {}
  }

  Future<void> doCheckIn() async {
    final enabled = await LocationService.ensureLocationEnabled(context);
    if (!enabled) return;
    final loc = await LocationService.getCurrent();
    if (loc == null) {
      _showError('تعذر تحديد موقعك');
      return;
    }

    try {
      final ok = await _attendance.checkIn(
        userId: widget.user['id'],
        branchId: widget.user['branch']['id'] ?? 0,
        lat: loc.latitude,
        lon: loc.longitude,
        locationName: loc.locationName,
      );
      if (ok) {
        setState(() {
          checkIn = TimeOfDay.now().format(context);
          statusText = 'حاضر';
        });
        await LocalStorage.saveUser(widget.user);
        _showSuccess('تم تسجيل الحضور بنجاح');
      }
    } catch (e) {
      _showError(e.toString().replaceAll('Exception: ', ''));
    }
  }

  Future<void> doCheckOut() async {
    final enabled = await LocationService.ensureLocationEnabled(context);
    if (!enabled) return;
    final loc = await LocationService.getCurrent();
    if (loc == null) {
      _showError('تعذر تحديد موقعك');
      return;
    }
    final ok = await _attendance.checkOut(
      userId: widget.user['id'],
      branchId: widget.user['branch']['id'] ?? 0,
      lat: loc.latitude,
      lon: loc.longitude,
      locationName: loc.locationName,
    );
    if (ok) {
      setState(() {
        checkOut = TimeOfDay.now().format(context);
        statusText = 'منصرف';
      });
      await LocalStorage.saveUser(widget.user);
      _showSuccess('تم تسجيل الانصراف بنجاح');
    } else {
      _showError('فشل تسجيل الانصراف');
    }
  }

  void _showSuccess(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg, textDirection: TextDirection.rtl),
        backgroundColor: greenColor,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  void _showError(String msg) {
    showDialog(
      context: context,
      builder: (_) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: Text(
            'خطأ',
            style: TextStyle(fontWeight: FontWeight.bold, color: redColor),
          ),
          content: Text(msg),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('موافق'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmployeeAvatar() {
    final profileImage =
        widget.user['profileImageData'] ?? widget.user['profileImage'];

    if (profileImage != null && profileImage.toString().isNotEmpty) {
      return Container(
        padding: EdgeInsets.all(3),
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          border: Border.all(
            color: Colors.white.withValues(alpha: 0.5),
            width: 2,
          ),
        ),
        child: CircleAvatar(
          radius: 30,
          backgroundColor: Colors.white.withValues(alpha: 0.2),
          backgroundImage: MemoryImage(base64Decode(profileImage.toString())),
        ),
      );
    }

    return Container(
      padding: EdgeInsets.all(3),
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(
          color: Colors.white.withValues(alpha: 0.5),
          width: 2,
        ),
      ),
      child: CircleAvatar(
        radius: 30,
        backgroundColor: Colors.white.withValues(alpha: 0.2),
        child: Icon(Icons.person, size: 35, color: Colors.white),
      ),
    );
  }

  Future<void> logout() async {
    await LocalStorage.clearUser();
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => LoginPage()),
    );
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
                padding: EdgeInsets.all(4),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Colors.white.withValues(alpha: 0.3),
                    width: 2,
                  ),
                ),
                child: _buildEmployeeAvatar(),
              ),
              SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      widget.user['fullName'] ?? '',
                      style: TextStyle(
                        fontSize: 20,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      '${widget.user['jobTitle']?['name'] ?? ''}',
                      style: TextStyle(
                        fontSize: 13,
                        color: Colors.white.withValues(alpha: 0.8),
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
              IconButton(
                onPressed: () => _navigateToTasks(),
                icon: Icon(
                  Icons.check_box_outlined,
                  color: Colors.white,
                  size: 28,
                ),
              ),
              IconButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => ChatListPage(
                      user: widget.user,
                      chatToken: widget.user['chatToken'],
                    ),
                  ),
                ),
                icon: Icon(
                  Icons.chat_bubble_outline,
                  color: Colors.white,
                  size: 28,
                ),
              ),
              IconButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                    builder: (_) => AnnouncementsPage(user: widget.user),
                  ),
                ),
                icon: Icon(Icons.campaign, color: Colors.white, size: 28),
                tooltip: 'الإعلانات',
              ),
              _buildManagerDashboardButton(),
            ],
          ),
          SizedBox(height: 20),
          Row(
            children: [
              _buildHeaderStat(
                Icons.business,
                widget.user['branch']?['name'] ?? '',
              ),
              Spacer(),
              _buildHeaderStat(Icons.badge, widget.user['code'] ?? ''),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildHeaderStat(IconData icon, String text) {
    return Row(
      children: [
        Icon(icon, color: Colors.white.withValues(alpha: 0.7), size: 16),
        SizedBox(width: 6),
        Text(
          text,
          style: TextStyle(
            color: Colors.white.withValues(alpha: 0.9),
            fontSize: 12,
            fontFamily: 'Tajawal',
          ),
        ),
      ],
    );
  }

  // ==================== ATTENDANCE CARD ====================
  Widget _buildAttendanceCard() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Text(
                'الحضور اليومي',
                style: TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                  fontFamily: 'Tajawal',
                ),
              ),
              Spacer(),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: statusText == 'حاضر'
                      ? greenColor.withValues(alpha: 0.1)
                      : statusText == 'منصرف'
                      ? primaryBlue.withValues(alpha: 0.1)
                      : Colors.grey.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  statusText,
                  style: TextStyle(
                    color: statusText == 'حاضر'
                        ? greenColor
                        : statusText == 'منصرف'
                        ? primaryBlue
                        : Colors.grey,
                    fontWeight: FontWeight.bold,
                    fontSize: 13,
                    fontFamily: 'Tajawal',
                  ),
                ),
              ),
            ],
          ),
          SizedBox(height: 20),
          Row(
            children: [
              Expanded(
                child: _buildTimeBox('دخول', checkIn, Icons.login, greenColor),
              ),
              SizedBox(width: 12),
              Expanded(
                child: _buildTimeBox(
                  'انصراف',
                  checkOut,
                  Icons.logout,
                  orangeColor,
                ),
              ),
            ],
          ),
          SizedBox(height: 20),
          InkWell(
            onTap: () => setState(
              () => _showAttendanceOptions = !_showAttendanceOptions,
            ),
            child: Container(
              height: 52,
              decoration: BoxDecoration(
                gradient: LinearGradient(colors: [primaryBlue, darkBlue]),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Center(
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.fingerprint, color: Colors.white, size: 24),
                    SizedBox(width: 8),
                    Text(
                      'تسجيل البصمة',
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 16,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          if (_showAttendanceOptions) ...[
            SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: doCheckIn,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: greenColor,
                      padding: EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    icon: Icon(Icons.login, size: 18),
                    label: Text(
                      'حضور',
                      style: TextStyle(fontFamily: 'Tajawal'),
                    ),
                  ),
                ),
                SizedBox(width: 10),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: doCheckOut,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: orangeColor,
                      padding: EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                    ),
                    icon: Icon(Icons.logout, size: 18),
                    label: Text(
                      'انصراف',
                      style: TextStyle(fontFamily: 'Tajawal'),
                    ),
                  ),
                ),
              ],
            ),
            SizedBox(height: 10),
            OutlinedButton.icon(
              onPressed: () => Navigator.push(
                context,
                MaterialPageRoute(builder: (_) => BreakPage(user: widget.user)),
              ),
              style: OutlinedButton.styleFrom(
                foregroundColor: purpleColor,
                side: BorderSide(color: purpleColor.withValues(alpha: 0.3)),
                padding: EdgeInsets.symmetric(vertical: 12, horizontal: 20),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              icon: Icon(Icons.coffee, size: 18),
              label: Text(
                'تسجيل بريك',
                style: TextStyle(fontFamily: 'Tajawal'),
              ),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildTimeBox(String label, String time, IconData icon, Color color) {
    return Container(
      padding: EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.05),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: color.withValues(alpha: 0.2)),
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 22),
          SizedBox(height: 6),
          Text(
            time,
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: color,
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 2),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
        ],
      ),
    );
  }

  // ==================== SERVICES GRID ====================
  Widget _buildServicesGrid() {
    final services = [
      {
        'title': 'طلب إجازة',
        'icon': Icons.beach_access,
        'color': greenColor,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => HolidayRequestPage(user: widget.user),
          ),
        ),
      },
      {
        'title': 'طلب إذن',
        'icon': Icons.access_time,
        'color': orangeColor,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => PermissionRequestPage(user: widget.user),
          ),
        ),
      },
      {
        'title': 'طلب سلفة',
        'icon': Icons.account_balance,
        'color': primaryBlue,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => LoanRequestPage(user: widget.user)),
        ),
      },
      {
        'title': 'سجل الإجازات',
        'icon': Icons.history,
        'color': greenColor,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => LeaveHistoryPage(user: widget.user),
          ),
        ),
      },
      {
        'title': 'سجل الأذونات',
        'icon': Icons.list_alt,
        'color': orangeColor,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => PermissionHistoryPage(user: widget.user),
          ),
        ),
      },
      {
        'title': 'سجل السلف',
        'icon': Icons.receipt_long,
        'color': primaryBlue,
        'onTap': () => Navigator.push(
          context,
          MaterialPageRoute(builder: (_) => LoanHistoryPage(user: widget.user)),
        ),
      },
    ];

    return Container(
      margin: EdgeInsets.only(bottom: 20),
      padding: EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'الخدمات',
            style: TextStyle(
              fontSize: 17,
              fontWeight: FontWeight.bold,
              color: darkBlue,
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 16),
          GridView.builder(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 3,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 0.9,
            ),
            itemCount: services.length,
            itemBuilder: (context, i) {
              final s = services[i];
              return InkWell(
                onTap: s['onTap'] as VoidCallback,
                borderRadius: BorderRadius.circular(16),
                child: Container(
                  decoration: BoxDecoration(
                    color: (s['color'] as Color).withValues(alpha: 0.05),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: (s['color'] as Color).withValues(alpha: 0.15),
                    ),
                  ),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Container(
                        width: 48,
                        height: 48,
                        decoration: BoxDecoration(
                          color: (s['color'] as Color).withValues(alpha: 0.1),
                          shape: BoxShape.circle,
                        ),
                        child: Icon(
                          s['icon'] as IconData,
                          color: s['color'] as Color,
                          size: 24,
                        ),
                      ),
                      SizedBox(height: 8),
                      Text(
                        s['title']!.toString(),
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w600,
                          color: Colors.grey[800],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                    ],
                  ),
                ),
              );
            },
          ),
        ],
      ),
    );
  }

  // ==================== QUICK STATS ====================
  Widget _buildQuickStats() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      child: Row(
        children: [
          Expanded(
            child: _buildMiniStatCard(
              'إجازات متبقية',
              '${_leaveStats['balance']}',
              Icons.beach_access,
              greenColor,
            ),
          ),
          SizedBox(width: 12),
          Expanded(
            child: _buildMiniStatCard(
              'سلف نشطة',
              '${_loanStats['activeLoans']}',
              Icons.account_balance,
              primaryBlue,
            ),
          ),
          SizedBox(width: 12),
          Expanded(
            child: _buildMiniStatCard(
              'طلبات معلقة',
              '${_leaveStats['pending']}',
              Icons.pending,
              orangeColor,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMiniStatCard(
    String label,
    String value,
    IconData icon,
    Color color,
  ) {
    return Container(
      padding: EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        children: [
          Icon(icon, color: color, size: 24),
          SizedBox(height: 6),
          Text(
            value,
            style: TextStyle(
              fontSize: 18,
              fontWeight: FontWeight.bold,
              color: darkBlue,
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 2),
          Text(
            label,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 10,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
        ],
      ),
    );
  }

  // ==================== MONTHLY REPORT BUTTON ====================
  Widget _buildReportButton() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      child: InkWell(
        onTap: () => Navigator.push(
          context,
          MaterialPageRoute(
            builder: (_) => MonthlyReportPage(user: widget.user),
          ),
        ),
        borderRadius: BorderRadius.circular(20),
        child: Container(
          padding: EdgeInsets.all(20),
          decoration: BoxDecoration(
            gradient: LinearGradient(colors: [purpleColor, Color(0xFF6D28D9)]),
            borderRadius: BorderRadius.circular(20),
            boxShadow: [
              BoxShadow(
                color: purpleColor.withValues(alpha: 0.3),
                blurRadius: 12,
                offset: Offset(0, 5),
              ),
            ],
          ),
          child: Row(
            children: [
              Container(
                width: 50,
                height: 50,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.2),
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.bar_chart, color: Colors.white, size: 26),
              ),
              SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'التقرير الشهري',
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.white,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      'عرض تفاصيل الحضور والانصراف',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.white.withValues(alpha: 0.8),
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
              Icon(Icons.arrow_back, color: Colors.white),
            ],
          ),
        ),
      ),
    );
  }

  // ==================== LOGOUT ====================
  Widget _buildLogout() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      child: OutlinedButton.icon(
        onPressed: () {
          showDialog(
            context: context,
            builder: (ctx) => Directionality(
              textDirection: TextDirection.rtl,
              child: AlertDialog(
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
                title: Text(
                  'تسجيل الخروج',
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: redColor,
                  ),
                ),
                content: Text('هل أنت متأكد من رغبتك في تسجيل الخروج؟'),
                actions: [
                  TextButton(
                    onPressed: () => Navigator.pop(ctx),
                    child: Text('إلغاء'),
                  ),
                  ElevatedButton(
                    onPressed: () {
                      Navigator.pop(ctx);
                      logout();
                    },
                    style: ElevatedButton.styleFrom(backgroundColor: redColor),
                    child: Text('تسجيل الخروج'),
                  ),
                ],
              ),
            ),
          );
        },
        style: OutlinedButton.styleFrom(
          foregroundColor: redColor,
          side: BorderSide(color: redColor.withValues(alpha: 0.3)),
          padding: EdgeInsets.symmetric(vertical: 16, horizontal: 20),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(14),
          ),
        ),
        icon: Icon(Icons.logout, size: 20),
        label: Text(
          'تسجيل الخروج',
          style: TextStyle(
            fontWeight: FontWeight.bold,
            fontSize: 15,
            fontFamily: 'Tajawal',
          ),
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
        body: SafeArea(
          child: SingleChildScrollView(
            padding: EdgeInsets.all(16),
            child: Column(
              children: [
                _buildHeader(),
                _buildAttendanceCard(),
                _buildServicesGrid(),
                _buildQuickStats(),
                _buildReportButton(),
                _buildLogout(),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildManagerDashboardButton() {
    if (widget.user['isManager'] != true) return SizedBox.shrink();
    return IconButton(
      onPressed: () => Navigator.push(
        context,
        MaterialPageRoute(builder: (_) => ManagerDashboard(widget.user)),
      ),
      icon: Icon(Icons.dashboard, color: Colors.white, size: 28),
    );
  }
}
