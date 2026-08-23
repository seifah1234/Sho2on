import 'package:flutter/material.dart';
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
import 'login_page.dart';

class MainPage extends StatefulWidget {
  final Map user;
  const MainPage(this.user, {super.key});

  @override
  State<MainPage> createState() => _MainPageState();
}

class _MainPageState extends State<MainPage> with SingleTickerProviderStateMixin {
  final AttendanceService _attendance = AttendanceService();
  final HolidayService _holidayService = HolidayService();
  final LoanService _loanService = LoanService();
  
  String checkIn = '--:--';
  String checkOut = '--:--';
  String statusText = 'غير مسجل';
  bool _showAttendanceOptions = false;

  // Animation
  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;

  // ألوان التصميم
  final Color babyBlue = Color(0xFF89CFF0);
  final Color darkBlue = Color(0xFF1E3A8A);
  final Color lightGray = Color(0xFFF5F7FA);
  final Color vacationColor = Color(0xFF4CAF50);
  final Color reportColor = Color(0xFF9C27B0);
  final Color historyColor = Color(0xFFFF9800);
  final Color loanColor = Color(0xFF2196F3);
  final Color installmentColor = Color(0xFFFF5722);
  final Color permissionColor = Color(0xFFFF9800);

  // إحصائيات الإجازات
  Map<String, dynamic> _leaveStats = {
    'balance': 0,
    'used': 0,
    'pending': 0,
    'approved': 0,
  };

  // إحصائيات السلف
  Map<String, dynamic> _loanStats = {
    'currentBalance': 0,
    'maxAllowed': 0,
    'pendingLoans': 0,
    'activeLoans': 0,
    'nextInstallment': 0,
    'nextDueDate': null,
  };

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
      vsync: this,
      duration: Duration(milliseconds: 600),
    );
    
    _fadeAnimation = CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeIn,
    );
    
    _animationController.forward();
    _initData();
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
      final result = await _holidayService.getEmployeeRequests(widget.user['id']);
      if (result['success'] && result['data'] != null) {
        final requests = result['data'] as List;
        final pending = requests.where((req) => req['status'] == 'قيد الانتظار').length;
        final approved = requests.where((req) => req['status'] == 'موافق').length;
        
        setState(() {
          _leaveStats = {
            'balance': 21,
            'used': 5,
            'pending': pending,
            'approved': approved,
          };
        });
      }
    } catch (e) {
      print('Error loading leave stats: $e');
    }
  }

  Future<void> _loadLoanStats() async {
    try {
      final result = await _loanService.getEmployee(widget.user['id']);
      if (result['success'] && result['data'] != null) {
        final data = result['data'];
        
        final loansResult = await _loanService.getEmployeeLoans(widget.user['id']);
        int pendingLoans = 0;
        int activeLoans = 0;
        double nextInstallment = 0;
        DateTime? nextDueDate;
        
        if (loansResult['success'] && loansResult['data'] != null) {
          final loans = loansResult['data'] as List;
          pendingLoans = loans.where((loan) => loan['status'] == 'Pending').length;
          activeLoans = loans.where((loan) => 
            loan['status'] == 'Approved' || loan['status'] == 'PartiallyPaid').length;
          
          for (var loan in loans) {
            if (loan['status'] == 'Approved' || loan['status'] == 'PartiallyPaid') {
              if ((loan['remainingAmount'] ?? 0) > 0) {
                nextInstallment = (loan['monthlyInstallment'] ?? 0).toDouble();
                nextDueDate = DateTime.now().add(Duration(days: 30));
                break;
              }
            }
          }
        }
        
        setState(() {
          _loanStats = {
            'currentBalance': (data['currentLoanBalance'] ?? 0).toDouble(),
            'maxAllowed': (data['maxAllowedAmount'] ?? 0).toDouble(),
            'pendingLoans': pendingLoans,
            'activeLoans': activeLoans,
            'nextInstallment': nextInstallment,
            'nextDueDate': nextDueDate,
          };
        });
      }
    } catch (e) {
      print('Error loading loan stats: $e');
    }
  }

  Future<void> doCheckIn() async {
    final enabled = await LocationService.ensureLocationEnabled(context);
    if (!enabled) return;

    final loc = await LocationService.getCurrent();
    if (loc == null) {
      _showError('تعذر تحديد موقعك الحالي');
      return;
    }

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
        widget.user['today']['checkIn'] = checkIn;
      });
      await LocalStorage.saveUser(widget.user);
      _showAttendanceOptions = false;
      _showSuccessSnackBar('تم تسجيل الحضور بنجاح');
    } else {
      _showError('فشل تسجيل الحضور');
    }
  }

  Future<void> doCheckOut() async {
    final enabled = await LocationService.ensureLocationEnabled(context);
    if (!enabled) return;

    final loc = await LocationService.getCurrent();
    if (loc == null) {
      _showError('تعذر تحديد موقعك الحالي');
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
        widget.user['today']['checkOut'] = checkOut;
      });
      await LocalStorage.saveUser(widget.user);
      _showAttendanceOptions = false;
      _showSuccessSnackBar('تم تسجيل الانصراف بنجاح');
    } else {
      _showError('فشل تسجيل الانصراف');
    }
  }

  void _showSuccessSnackBar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Icon(Icons.check_circle, color: Colors.white, size: 20),
            SizedBox(width: 10),
            Text(
              message,
              textDirection: TextDirection.rtl,
              style: TextStyle(fontFamily: 'Tajawal'),
            ),
          ],
        ),
        backgroundColor: Colors.green,
        duration: Duration(seconds: 2),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(10),
        ),
        margin: EdgeInsets.all(16),
      ),
    );
  }

  void _showError(String message) {
    showDialog(
      context: context,
      builder: (_) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('خطأ', 
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: Colors.red,
                )
              ),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Colors.red.withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.error_outline, color: Colors.red, size: 20),
              ),
            ],
          ),
          content: Text(message, 
            textDirection: TextDirection.rtl,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 14),
          ),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(
                foregroundColor: darkBlue,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text('موافق', 
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> logout() async {
    await LocalStorage.clearUser();
    _navigateToLogin();
  }

  void _navigateToHolidayRequest() async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => HolidayRequestPage(user: widget.user),
      ),
    );
    
    if (result == true) {
      await _loadLeaveStats();
      _showSuccessSnackBar('تم تقديم طلب الإجازة بنجاح');
    }
  }

  void _navigateToLeaveHistory() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => LeaveHistoryPage(user: widget.user),
      ),
    );
    await _loadLeaveStats();
  }

  void _navigateToLogin() async {
    await Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (context) => LoginPage(),
      ),
    );
  }

  void _navigateToPermissionHistory() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => PermissionHistoryPage(user: widget.user),
      ),
    );
  }

  void _navigateToLoanRequest() async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => LoanRequestPage(user: widget.user),
      ),
    );
    
    if (result == true) {
      await _loadLoanStats();
      _showSuccessSnackBar('تم تقديم طلب السلفة بنجاح');
    }
  }

  void _navigateToPermissionRequest() async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => PermissionRequestPage(user: widget.user),
      ),
    );
    
    if (result == true) {
      _showSuccessSnackBar('تم تقديم طلب الإذن بنجاح');
    }
  }

  void _navigateToLoanHistory() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => LoanHistoryPage(user: widget.user),
      ),
    );
    await _loadLoanStats();
  }

  void _navigateToMonthlyReport() async {
    await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => MonthlyReportPage(user: widget.user),
      ),
    );
  }

  String _formatDate(DateTime? date) {
    if (date == null) return 'غير محدد';
    return '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}';
  }

  Widget _buildEmployeeInfoCard() {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.all(20),
      margin: EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [darkBlue, babyBlue],
        ),
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: darkBlue.withOpacity(0.3),
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
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      'مرحباً بك 👋',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 16,
                        color: Colors.white.withOpacity(0.9),
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
                    ),
                    SizedBox(height: 5),
                    Text(
                      '${widget.user['department']?['name'] ?? ''} - ${widget.user['jobTitle']?['name'] ?? ''}',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 13,
                        color: Colors.white.withOpacity(0.8),
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                padding: EdgeInsets.all(3),
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Colors.white.withOpacity(0.5),
                    width: 2,
                  ),
                ),
                child: CircleAvatar(
                  radius: 30,
                  backgroundColor: Colors.white.withOpacity(0.2),
                  child: Icon(
                    Icons.person,
                    size: 35,
                    color: Colors.white,
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildAttendanceSection() {
    return Container(
      margin: EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'حالة الحضور اليومية',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: darkBlue,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Container(
                  padding: EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: babyBlue.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(Icons.calendar_today, color: babyBlue, size: 20),
                ),
              ],
            ),
            SizedBox(height: 16),
            
            // حالة الحضور
            Container(
              padding: EdgeInsets.all(16),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: statusText == 'حاضر' 
                    ? [Colors.green[50]!, Colors.green[100]!]
                    : statusText == 'منصرف' 
                      ? [Colors.blue[50]!, Colors.blue[100]!]
                      : [Colors.grey[100]!, Colors.grey[200]!],
                ),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: statusText == 'حاضر' 
                    ? Colors.green[300]!
                    : statusText == 'منصرف' 
                      ? Colors.blue[300]!
                      : Colors.grey[300]!,
                  width: 1.5,
                ),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                textDirection: TextDirection.rtl,
                children: [
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        'الحالة الحالية',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontSize: 12,
                          color: Colors.grey[600],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                      SizedBox(height: 4),
                      Text(
                        statusText,
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.bold,
                          color: statusText == 'حاضر' 
                            ? Colors.green[800]
                            : statusText == 'منصرف' 
                              ? Colors.blue[800]
                              : Colors.grey[700],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                    ],
                  ),
                  Container(
                    padding: EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.white,
                      shape: BoxShape.circle,
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withOpacity(0.1),
                          blurRadius: 8,
                          offset: Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Icon(
                      statusText == 'حاضر' ? Icons.check_circle : 
                      statusText == 'منصرف' ? Icons.logout : Icons.schedule,
                      color: statusText == 'حاضر' ? Colors.green : 
                             statusText == 'منصرف' ? Colors.blue : Colors.grey,
                      size: 30,
                    ),
                  ),
                ],
              ),
            ),
            
            SizedBox(height: 16),
            
            // أوقات الدخول والخروج
            Row(
              textDirection: TextDirection.rtl,
              children: [
                Expanded(
                  child: _buildTimeCard(
                    'وقت الدخول',
                    checkIn,
                    Icons.login,
                    checkIn != '--:--' ? Colors.green : Colors.grey,
                  ),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: _buildTimeCard(
                    'وقت الانصراف',
                    checkOut,
                    Icons.logout,
                    checkOut != '--:--' ? Colors.orange : Colors.grey,
                  ),
                ),
              ],
            ),
            
            SizedBox(height: 16),
            
            // زر البصمة
            Container(
              height: 56,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [babyBlue, darkBlue],
                ),
                borderRadius: BorderRadius.circular(16),
                boxShadow: [
                  BoxShadow(
                    color: babyBlue.withOpacity(0.4),
                    blurRadius: 12,
                    offset: Offset(0, 4),
                  ),
                ],
              ),
              child: ElevatedButton.icon(
                onPressed: () {
                  setState(() {
                    _showAttendanceOptions = !_showAttendanceOptions;
                  });
                },
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.transparent,
                  shadowColor: Colors.transparent,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                  ),
                ),
                icon: Icon(Icons.fingerprint, size: 24),
                label: Text(
                  'بصمة الحضور',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'Tajawal',
                  ),
                ),
              ),
            ),
            
            // خيارات الحضور
            AnimatedSwitcher(
              duration: Duration(milliseconds: 300),
              child: _showAttendanceOptions
                  ? Padding(
                      key: ValueKey('attendance_options'),
                      padding: const EdgeInsets.only(top: 16),
                      child: _buildAttendanceOptions(),
                    )
                  : SizedBox.shrink(),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTimeCard(String title, String time, IconData icon, Color color) {
    return Container(
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: color.withOpacity(0.3), width: 1),
        boxShadow: [
          BoxShadow(
            color: color.withOpacity(0.1),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Text(
            title,
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
            textDirection: TextDirection.rtl,
            children: [
              Text(
                time,
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: color,
                  fontFamily: 'Tajawal',
                ),
              ),
              Container(
                padding: EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: color.withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(icon, color: color, size: 16),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildAttendanceOptions() {
    return Column(
      children: [
        Divider(),
        SizedBox(height: 12),
        Text(
          'اختر العملية المطلوبة:',
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: darkBlue,
            fontFamily: 'Tajawal',
          ),
        ),
        SizedBox(height: 12),
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceEvenly,
          textDirection: TextDirection.rtl,
          children: [
            Expanded(
              child: ElevatedButton.icon(
                onPressed: doCheckIn,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.green,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                icon: Icon(Icons.login, size: 20),
                label: Text('حضور', 
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            SizedBox(width: 12),
            Expanded(
              child: ElevatedButton.icon(
                onPressed: doCheckOut,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.orange,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                ),
                icon: Icon(Icons.logout, size: 20),
                label: Text('انصراف', 
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
          ],
        ),
        SizedBox(height: 8),
        TextButton(
          onPressed: () {
            setState(() {
              _showAttendanceOptions = false;
            });
          },
          child: Text(
            'إخفاء الخيارات',
            textDirection: TextDirection.rtl,
            style: TextStyle(
              color: Colors.grey,
              fontFamily: 'Tajawal',
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildQuickServices() {
    return Container(
      margin: EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'الخدمات السريعة',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: darkBlue,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Container(
                  padding: EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: babyBlue.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(Icons.grid_view, color: babyBlue, size: 20),
                ),
              ],
            ),
            SizedBox(height: 16),
            
            GridView(
              shrinkWrap: true,
              physics: NeverScrollableScrollPhysics(),
              gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 3,
                crossAxisSpacing: 12,
                mainAxisSpacing: 12,
                childAspectRatio: 0.85,
              ),
              children: [
                _buildServiceButton(
                  'طلب إجازة',
                  Icons.beach_access,
                  vacationColor,
                  _navigateToHolidayRequest,
                ),
                _buildServiceButton(
                  'طلب إذن',
                  Icons.access_time,
                  permissionColor,
                  _navigateToPermissionRequest,
                ),
                _buildServiceButton(
                  'طلب سلفة',
                  Icons.account_balance,
                  loanColor,
                  _navigateToLoanRequest,
                ),
                _buildServiceButton(
                  'سجل الإجازات',
                  Icons.history,
                  vacationColor,
                  _navigateToLeaveHistory,
                ),
                _buildServiceButton(
                  'سجل الاذونات',
                  Icons.bar_chart,
                  permissionColor,
                  _navigateToPermissionHistory,
                ),
                _buildServiceButton(
                  'سجل السلف',
                  Icons.history_edu,
                  loanColor,
                  _navigateToLoanHistory,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildServiceButton(String title, IconData icon, Color color, VoidCallback onTap) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: color.withOpacity(0.2), width: 1),
          boxShadow: [
            BoxShadow(
              color: color.withOpacity(0.08),
              blurRadius: 8,
              offset: Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Container(
              width: 50,
              height: 50,
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  colors: [color.withOpacity(0.1), color.withOpacity(0.05)],
                ),
                shape: BoxShape.circle,
              ),
              child: Icon(icon, color: color, size: 26),
            ),
            SizedBox(height: 8),
            Text(
              title,
              textDirection: TextDirection.rtl,
              textAlign: TextAlign.center,
              style: TextStyle(
                fontSize: 11,
                fontWeight: FontWeight.bold,
                color: darkBlue,
                fontFamily: 'Tajawal',
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLeaveStatsSection() {
  return Container(
    margin: EdgeInsets.only(bottom: 16),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withOpacity(0.05),
          blurRadius: 10,
          offset: Offset(0, 3),
        ),
      ],
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            textDirection: TextDirection.rtl,
            children: [
              Text(
                'إحصائيات الإجازات',
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: vacationColor,
                  fontFamily: 'Tajawal',
                ),
              ),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: vacationColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(Icons.beach_access, color: vacationColor, size: 20),
              ),
            ],
          ),
          SizedBox(height: 16),
          
          GridView(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.8, // زيادة النسبة لتوفير مساحة أكبر
            ),
            children: [
              _buildStatItem(
                'الرصيد المتبقي',
                '${_leaveStats['balance']} يوم',
                Icons.account_balance_wallet,
                Colors.green,
              ),
              _buildStatItem(
                'المستخدم',
                '${_leaveStats['used']} يوم',
                Icons.airline_seat_recline_normal,
                Colors.orange,
              ),
              _buildStatItem(
                'قيد الانتظار',
                '${_leaveStats['pending']} طلب',
                Icons.access_time,
                Colors.blue,
              ),
              _buildStatItem(
                'الموافق عليه',
                '${_leaveStats['approved']} طلب',
                Icons.check_circle,
                Colors.purple,
              ),
            ],
          ),
          
          SizedBox(height: 16),
          
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              Expanded(
                child: _buildActionButton(
                  'سجل الإجازات',
                  Icons.history,
                  Colors.grey[100]!,
                  darkBlue,
                  _navigateToLeaveHistory,
                ),
              ),
              SizedBox(width: 8),
              Expanded(
                child: _buildActionButton(
                  'طلب جديد',
                  Icons.add,
                  vacationColor,
                  Colors.white,
                  _navigateToHolidayRequest,
                ),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}

  Widget _buildLoanStatsSection() {
  return Container(
    margin: EdgeInsets.only(bottom: 16),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withOpacity(0.05),
          blurRadius: 10,
          offset: Offset(0, 3),
        ),
      ],
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            textDirection: TextDirection.rtl,
            children: [
              Text(
                'إحصائيات السلف',
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: loanColor,
                  fontFamily: 'Tajawal',
                ),
              ),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: loanColor.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(Icons.account_balance, color: loanColor, size: 20),
              ),
            ],
          ),
          SizedBox(height: 16),
          
          GridView(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.8, // زيادة النسبة لتوفير مساحة أكبر
            ),
            children: [
              _buildStatItem(
                'الرصيد الحالي',
                '${_loanStats['currentBalance'].toStringAsFixed(0)} ج',
                Icons.money,
                Colors.red,
              ),
              _buildStatItem(
                'الحد الأقصى',
                '${_loanStats['maxAllowed'].toStringAsFixed(0)} ج',
                Icons.warning,
                Colors.green,
              ),
              _buildStatItem(
                'القروض النشطة',
                '${_loanStats['activeLoans']}',
                Icons.credit_card,
                Colors.blue,
              ),
              _buildStatItem(
                'القسط القادم',
                '${_loanStats['nextInstallment'].toStringAsFixed(0)} ج',
                Icons.calendar_today,
                Colors.orange,
              ),
            ],
          ),
          
          if (_loanStats['nextInstallment'] > 0 && _loanStats['nextDueDate'] != null)
            Padding(
              padding: const EdgeInsets.only(top: 16),
              child: Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [Colors.orange[50]!, Colors.orange[100]!],
                  ),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.orange[200]!),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  textDirection: TextDirection.rtl,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            'القسط القادم مستحق',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 12,
                              color: Colors.orange[800],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            '${_loanStats['nextInstallment'].toStringAsFixed(2)} جنيه',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Colors.orange[800],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            'تاريخ الاستحقاق: ${_formatDate(_loanStats['nextDueDate'])}',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 11,
                              color: Colors.orange[600],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(width: 8),
                    Container(
                      padding: EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.notifications_active, color: Colors.orange, size: 20),
                    ),
                  ],
                ),
              ),
            ),
          
          SizedBox(height: 16),
          
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceEvenly,
            children: [
              Expanded(
                child: _buildActionButton(
                  'سجل السلف',
                  Icons.history,
                  Colors.grey[100]!,
                  darkBlue,
                  _navigateToLoanHistory,
                ),
              ),
              SizedBox(width: 8),
              Expanded(
                child: _buildActionButton(
                  'طلب سلفة',
                  Icons.add,
                  loanColor,
                  Colors.white,
                  _navigateToLoanRequest,
                ),
              ),
            ],
          ),
        ],
      ),
    ),
  );
}

  Widget _buildActionButton(String text, IconData icon, Color bgColor, Color textColor, VoidCallback onPressed) {
  return ElevatedButton.icon(
    onPressed: onPressed,
    style: ElevatedButton.styleFrom(
      backgroundColor: bgColor,
      foregroundColor: textColor,
      padding: EdgeInsets.symmetric(vertical: 12, horizontal: 8), // تقليل padding
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
    ),
    icon: Icon(icon, size: 16), // تقليل حجم الأيقونة
    label: Text(
      text,
      style: TextStyle(
        fontSize: 11, // تقليل حجم الخط
        fontWeight: FontWeight.bold,
        fontFamily: 'Tajawal',
      ),
    ),
  );
}

  Widget _buildStatItem(String title, String value, IconData icon, Color color) {
  return Container(
    decoration: BoxDecoration(
      gradient: LinearGradient(
        colors: [Colors.white, color.withOpacity(0.05)],
      ),
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: color.withOpacity(0.3), width: 1),
      boxShadow: [
        BoxShadow(
          color: color.withOpacity(0.08),
          blurRadius: 8,
          offset: Offset(0, 2),
        ),
      ],
    ),
    child: Padding(
      padding: const EdgeInsets.all(4.0),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Icon(icon, color: color, size: 24),
          SizedBox(height: 2),
          Text(
            title,
            textDirection: TextDirection.rtl,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 11, // تقليل حجم الخط
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 2),
          Text(
            value,
            textDirection: TextDirection.rtl,
            textAlign: TextAlign.center,
            style: TextStyle(
              fontSize: 14, // تقليل حجم الخط
              fontWeight: FontWeight.bold,
              color: darkBlue,
              fontFamily: 'Tajawal',
            ),
          ),
        ],
      ),
    ),
  );
}

  Widget _buildMonthlyStats() {
  return Container(
    margin: EdgeInsets.only(bottom: 16),
    decoration: BoxDecoration(
      color: Colors.white,
      borderRadius: BorderRadius.circular(20),
      boxShadow: [
        BoxShadow(
          color: Colors.black.withOpacity(0.05),
          blurRadius: 10,
          offset: Offset(0, 3),
        ),
      ],
    ),
    child: Padding(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            textDirection: TextDirection.rtl,
            children: [
              Text(
                'إحصائيات هذا الشهر',
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                  fontFamily: 'Tajawal',
                ),
              ),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: babyBlue.withOpacity(0.1),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Icon(Icons.calendar_month, color: babyBlue, size: 20),
              ),
            ],
          ),
          SizedBox(height: 16),
          
          GridView(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
              crossAxisCount: 2,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.6, // زيادة النسبة لتوفير مساحة أكبر
            ),
            children: [
              _buildMonthlyStatItem(
                'أيام الحضور',
                widget.user['stats']['present'].toString(),
                Icons.check_circle,
                Colors.green,
              ),
              _buildMonthlyStatItem(
                'أيام الغياب',
                widget.user['stats']['absent'].toString(),
                Icons.cancel,
                Colors.red,
              ),
              _buildMonthlyStatItem(
                'التأخيرات',
                widget.user['stats']['late'].toString(),
                Icons.schedule,
                Colors.orange,
              ),
              _buildMonthlyStatItem(
                'الإجازات',
                widget.user['stats']['vacation'].toString(),
                Icons.beach_access,
                babyBlue,
              ),
            ],
          ),
          
          SizedBox(height: 16),
          
          Container(
            height: 50,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [reportColor, reportColor.withOpacity(0.8)],
              ),
              borderRadius: BorderRadius.circular(12),
              boxShadow: [
                BoxShadow(
                  color: reportColor.withOpacity(0.3),
                  blurRadius: 8,
                  offset: Offset(0, 3),
                ),
              ],
            ),
            child: ElevatedButton.icon(
              onPressed: _navigateToMonthlyReport,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.transparent,
                shadowColor: Colors.transparent,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
              icon: Icon(Icons.bar_chart, size: 20),
              label: Text(
                'عرض التقرير الشامل',
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
            ),
          ),
        ],
      ),
    ),
  );
}

  Widget _buildMonthlyStatItem(String title, String value, IconData icon, Color color) {
  return Container(
    decoration: BoxDecoration(
      gradient: LinearGradient(
        colors: [Colors.white, color.withOpacity(0.05)],
      ),
      borderRadius: BorderRadius.circular(16),
      border: Border.all(color: color.withOpacity(0.3), width: 1),
      boxShadow: [
        BoxShadow(
          color: color.withOpacity(0.08),
          blurRadius: 8,
          offset: Offset(0, 2),
        ),
      ],
    ),
    child: Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Icon(icon, color: color, size: 28),
        SizedBox(height: 6),
        Text(
          value,
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontSize: 20, // تقليل حجم الخط قليلاً
            fontWeight: FontWeight.bold,
            color: darkBlue,
            fontFamily: 'Tajawal',
          ),
        ),
        SizedBox(height: 2),
        Text(
          title,
          textDirection: TextDirection.rtl,
          textAlign: TextAlign.center,
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

  Widget _buildPersonalInfo() {
    return Container(
      margin: EdgeInsets.only(bottom: 16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'معلومات شخصية',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: darkBlue,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Container(
                  padding: EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    color: babyBlue.withOpacity(0.1),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Icon(Icons.person, color: babyBlue, size: 20),
                ),
              ],
            ),
            SizedBox(height: 16),
            
            Column(
              children: [
                _buildInfoRow('الفرع', widget.user['branch']['name'] ?? 'الفرع الرئيسي', Icons.business),
                Divider(height: 20),
                _buildInfoRow('الرقم الوظيفي', widget.user['employeeId']?.toString() ?? 'N/A', Icons.badge),
                Divider(height: 20),
                _buildInfoRow('البريد الإلكتروني', widget.user['email'] ?? 'N/A', Icons.email),
                Divider(height: 20),
                _buildInfoRow('رقم الهاتف', widget.user['phone'] ?? 'N/A', Icons.phone),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(String label, String value, IconData icon) {
    return Row(
      textDirection: TextDirection.rtl,
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                label,
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 12,
                  color: Colors.grey[600],
                  fontFamily: 'Tajawal',
                ),
              ),
              SizedBox(height: 4),
              Text(
                value,
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: darkBlue,
                  fontFamily: 'Tajawal',
                ),
              ),
            ],
          ),
        ),
        SizedBox(width: 12),
        Container(
          padding: EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: babyBlue.withOpacity(0.1),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(icon, color: babyBlue, size: 20),
        ),
      ],
    );
  }

  Widget _buildLogoutButton() {
    return Container(
      margin: EdgeInsets.only(bottom: 20),
      child: ElevatedButton.icon(
        onPressed: () {
          _showLogoutConfirmation();
        },
        style: ElevatedButton.styleFrom(
          backgroundColor: Colors.white,
          foregroundColor: Colors.red,
          minimumSize: Size(double.infinity, 56),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: BorderSide(color: Colors.red.withOpacity(0.3), width: 1.5),
          ),
          elevation: 0,
        ),
        icon: Icon(Icons.logout, size: 20),
        label: Text(
          'تسجيل الخروج',
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            fontFamily: 'Tajawal',
          ),
        ),
      ),
    );
  }

  void _showLogoutConfirmation() {
    showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'تسجيل الخروج',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: Colors.red,
                ),
              ),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Colors.red.withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.logout, color: Colors.red, size: 20),
              ),
            ],
          ),
          content: Text(
            'هل أنت متأكد من رغبتك في تسجيل الخروج؟',
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 14,
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(
                foregroundColor: darkBlue,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text(
                'إلغاء',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            ElevatedButton(
              onPressed: logout,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.red,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text(
                'تسجيل الخروج',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: lightGray,
        appBar: AppBar(
          title: Text(
            'لوحة التحكم',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontFamily: 'Tajawal',
            ),
          ),
          backgroundColor: babyBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.vertical(
              bottom: Radius.circular(20),
            ),
          ),
        ),
        body: SafeArea(
          child: FadeTransition(
            opacity: _fadeAnimation,
            child: SingleChildScrollView(
              padding: EdgeInsets.all(16),
              child: Column(
                children: [
                  _buildEmployeeInfoCard(),
                  _buildAttendanceSection(),
                  _buildQuickServices(),
                  _buildLeaveStatsSection(),
                  _buildLoanStatsSection(),
                  _buildMonthlyStats(),
                  _buildPersonalInfo(),
                  _buildLogoutButton(),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _animationController.dispose();
    super.dispose();
  }
}