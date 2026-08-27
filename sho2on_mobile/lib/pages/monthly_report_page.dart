import 'package:flutter/material.dart';
import '../services/attendance_service.dart';

class MonthlyReportPage extends StatefulWidget {
  final Map user;
  const MonthlyReportPage({super.key, required this.user});

  @override
  _MonthlyReportPageState createState() => _MonthlyReportPageState();
}

class _MonthlyReportPageState extends State<MonthlyReportPage> {
  final AttendanceService _attendanceService = AttendanceService();

  // بيانات التقرير
  List<Map<String, dynamic>> _dailyReports = [];
  Map<String, dynamic>? _summary;

  // بيانات الموظف
  String _employeeName = '';
  String _employeeCode = '';
  String _branchName = '';

  // بيانات البريك
  Map<String, double> _breakByDay = {};

  // حالة التحميل
  bool _isLoading = false;
  String _errorMessage = '';

  // الشهر والسنة المختارين
  DateTime _selectedMonth = DateTime.now();

  // ألوان التصميم
  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color presentColor = Color(0xFF10B981);
  final Color absentColor = Color(0xFFEF4444);
  final Color leaveColor = Color(0xFF8B5CF6);
  final Color holidayColor = Color(0xFFF59E0B);
  final Color restColor = Color(0xFF64748B);
  final Color lateColor = Color(0xFFF97316);

  final List<String> arabicMonths = [
    'يناير', 'فبراير', 'مارس', 'أبريل', 'مايو', 'يونيو',
    'يوليو', 'أغسطس', 'سبتمبر', 'أكتوبر', 'نوفمبر', 'ديسمبر'
  ];

  @override
  void initState() {
    super.initState();
    _loadReport();
  }

  String _getArabicDayName(int weekday) {
    switch (weekday) {
      case 1: return 'السبت';
      case 2: return 'الأحد';
      case 3: return 'الإثنين';
      case 4: return 'الثلاثاء';
      case 5: return 'الأربعاء';
      case 6: return 'الخميس';
      case 7: return 'الجمعة';
      default: return '';
    }
  }

  Future<void> _loadReport() async {
    setState(() {
      _isLoading = true;
      _errorMessage = '';
    });

    try {
      int userId = widget.user['id'] ?? 0;
      int year = _selectedMonth.year;
      int month = _selectedMonth.month;

      final result = await _attendanceService.getMonthlyReport(
        userId: userId,
        year: year,
        month: month,
      );

      if (result['success'] == true) {
        _processReportData(result['data']);
        await _loadBreakData();
      } else {
        setState(() {
          _errorMessage = result['message'] ?? 'حدث خطأ أثناء تحميل التقرير';
          _isLoading = false;
        });
      }
    } catch (e) {
      setState(() {
        _errorMessage = 'خطأ في الاتصال: $e';
        _isLoading = false;
      });
    }
  }

  void _processReportData(dynamic reportData) {
    if (reportData == null) {
      setState(() {
        _dailyReports = [];
        _summary = null;
        _isLoading = false;
      });
      return;
    }

    Map<String, dynamic> report;
    if (reportData is List && reportData.isNotEmpty) {
      report = Map<String, dynamic>.from(reportData[0]);
    } else if (reportData is Map) {
      report = Map<String, dynamic>.from(reportData);
    } else {
      setState(() {
        _errorMessage = 'هيكل البيانات غير متوقع';
        _isLoading = false;
      });
      return;
    }

    _employeeName = report['employeeName'] ?? widget.user['fullName'] ?? '';
    _employeeCode = report['employeeCode'] ?? widget.user['code'] ?? '';
    _branchName = report['branchName'] ?? widget.user['branch']?['name'] ?? '';

    List<Map<String, dynamic>> processedReports = [];
    for (var day in (report['dailyReports'] ?? [])) {
      if (day is Map) {
        processedReports.add(Map<String, dynamic>.from(day));
      }
    }

    Map<String, dynamic>? summary;
    if (report['summary'] is Map) {
      summary = Map<String, dynamic>.from(report['summary']);
    }

    setState(() {
      _dailyReports = processedReports;
      _summary = summary;
      _isLoading = false;
    });
  }

  Future<void> _loadBreakData() async {
    try {
      final result = await _attendanceService.getBreakReport(
        userId: widget.user['id'],
        month: _selectedMonth.month,
        year: _selectedMonth.year,
      );

      if (result['success'] == true) {
        Map<String, double> breakMap = {};
        final data = result['data'];

        if (data is Map && data['logs'] is List) {
          for (var log in data['logs']) {
            if (log is Map && log['startTime'] != null) {
              final date = DateTime.parse(log['startTime'].toString());
              final dateKey = '${date.year}-${date.month}-${date.day}';
              final duration = log['endTime'] != null
                  ? DateTime.parse(log['endTime'].toString()).difference(date).inMinutes.toDouble()
                  : 0.0;

              breakMap[dateKey] = (breakMap[dateKey] ?? 0) + duration;
            }
          }
        }

        setState(() => _breakByDay = breakMap);
      }
    } catch (e) {}
  }

  Future<void> _selectMonth() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _selectedMonth,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
      initialDatePickerMode: DatePickerMode.year,
      builder: (context, child) {
        return Theme(
          data: ThemeData.light().copyWith(
            colorScheme: ColorScheme.light(
              primary: primaryBlue,
              onPrimary: Colors.white,
              surface: Colors.white,
              onSurface: Colors.black,
            ),
          ),
          child: Directionality(textDirection: TextDirection.rtl, child: child!),
        );
      },
    );

    if (picked != null) {
      setState(() => _selectedMonth = DateTime(picked.year, picked.month, 1));
      _loadReport();
    }
  }

  String _formatTime(String? time) {
    if (time == null || time.isEmpty) return '—';
    try {
      final parts = time.split(':');
      if (parts.length >= 2) {
        final hour = int.parse(parts[0]);
        final minute = parts[1];
        final period = hour >= 12 ? 'م' : 'ص';
        final hour12 = hour % 12 == 0 ? 12 : hour % 12;
        return '$hour12:$minute $period';
      }
    } catch (e) {}
    return time;
  }

  String _formatDuration(int minutes) {
    if (minutes <= 0) return '—';
    final hours = minutes ~/ 60;
    final mins = minutes % 60;
    return '${hours.toString().padLeft(2, '0')}:${mins.toString().padLeft(2, '0')}';
  }

  String _formatWorkHours(double hours) {
    if (hours <= 0) return '—';
    final h = hours.floor();
    final m = ((hours - h) * 60).round();
    return '${h.toString().padLeft(2, '0')}:${m.toString().padLeft(2, '0')}';
  }

  String _getStatusText(dynamic status) {
    if (status == null) return 'غير معروف';
    return status.toString();
  }

  Color _getStatusColor(String status) {
    if (status.contains('حاضر')) return presentColor;
    if (status.contains('غائب')) return absentColor;
    if (status.contains('إجازة')) return leaveColor;
    if (status.contains('عطلة')) return holidayColor;
    if (status.contains('راحة')) return restColor;
    return Colors.grey;
  }

  // ==================== BUILD WIDGETS ====================

  Widget _buildMonthSelector() {
    return Container(
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
      ),
      child: Row(
        children: [
          Container(
            padding: EdgeInsets.all(8),
            decoration: BoxDecoration(color: lightBlue, borderRadius: BorderRadius.circular(10)),
            child: Icon(Icons.calendar_month, color: primaryBlue, size: 20),
          ),
          SizedBox(width: 12),
          Expanded(
            child: Text(
              '${arabicMonths[_selectedMonth.month - 1]} ${_selectedMonth.year}',
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: darkBlue,
              ),
            ),
          ),
          IconButton(
            icon: Icon(Icons.chevron_right, color: primaryBlue, size: 22),
            onPressed: () {
              setState(() => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month - 1, 1));
              _loadReport();
            },
          ),
          IconButton(
            icon: Icon(Icons.chevron_left, color: primaryBlue, size: 22),
            onPressed: () {
              setState(() => _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month + 1, 1));
              _loadReport();
            },
          ),
        ],
      ),
    );
  }

  Widget _buildEmployeeHeader() {
    return Container(
      padding: EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [darkBlue, primaryBlue],
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: primaryBlue.withValues(alpha: 0.3), blurRadius: 10, offset: Offset(0, 4))],
      ),
      child: Row(
        children: [
          Container(
            width: 50,
            height: 50,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.2),
              shape: BoxShape.circle,
              border: Border.all(color: Colors.white.withValues(alpha: 0.3), width: 2),
            ),
            child: Center(
              child: Text(
                _employeeName.isNotEmpty ? _employeeName[0] : '?',
                style: TextStyle(color: Colors.white, fontSize: 22, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'),
              ),
            ),
          ),
          SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _employeeName,
                  style: TextStyle(fontSize: 17, fontWeight: FontWeight.bold, color: Colors.white, fontFamily: 'Tajawal'),
                ),
                SizedBox(height: 4),
                Text(
                  '$_employeeCode — $_branchName',
                  style: TextStyle(fontSize: 12, color: Colors.white.withValues(alpha: 0.85), fontFamily: 'Tajawal'),
                ),
              ],
            ),
          ),
          Icon(Icons.badge_outlined, color: Colors.white.withValues(alpha: 0.7), size: 24),
        ],
      ),
    );
  }

  Widget _buildStatsGrid() {
    if (_summary == null) return SizedBox.shrink();
    final summary = _summary!;

    final stats = [
      {'label': 'أيام الغياب', 'value': '${summary['absentDays'] ?? summary['totalAbsenceDays'] ?? 0}', 'icon': Icons.person_off, 'color': absentColor},
      {'label': 'راحة أسبوعية', 'value': '${summary['restDays'] ?? summary['totalWeeklyRestDays'] ?? 0}', 'icon': Icons.weekend, 'color': restColor},
      {'label': 'أيام العطلة', 'value': '${summary['holidayDays'] ?? summary['totalHolidayDays'] ?? 0}', 'icon': Icons.beach_access, 'color': holidayColor},
      {'label': 'التأخير', 'value': _formatDuration(summary['totalLateMinutes'] ?? 0), 'icon': Icons.access_time, 'color': lateColor},
      {'label': 'الإضافي', 'value': _formatDuration(summary['totalOvertimeMinutes'] ?? 0), 'icon': Icons.timer, 'color': leaveColor},
      {'label': 'ساعات العمل', 'value': _formatWorkHours(summary['totalWorkHours'] ?? 0), 'icon': Icons.work, 'color': primaryBlue},
    ];

    return GridView(
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
          padding: EdgeInsets.all(8),
          decoration: BoxDecoration(
            color: cardColor,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: color.withValues(alpha: 0.15)),
            boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.03), blurRadius: 5, offset: Offset(0, 2))],
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(color: color.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: Icon(stat['icon'] as IconData, color: color, size: 20),
              ),
              SizedBox(height: 6),
              Text(
                stat['value']!.toString(),
                style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue, fontFamily: 'Tajawal'),
              ),
              SizedBox(height: 2),
              Text(
                stat['label']!.toString(),
                textAlign: TextAlign.center,
                style: TextStyle(fontSize: 9, color: Colors.grey[500], fontFamily: 'Tajawal'),
              ),
            ],
          ),
        );
      }).toList(),
    );
  }

  Widget _buildDailyTable() {
    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: EdgeInsets.all(16),
            child: Row(
              children: [
                Container(
                  padding: EdgeInsets.all(8),
                  decoration: BoxDecoration(color: lightBlue, borderRadius: BorderRadius.circular(10)),
                  child: Icon(Icons.list_alt, color: primaryBlue, size: 20),
                ),
                SizedBox(width: 10),
                Text(
                  'تفاصيل الأيام',
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue, fontFamily: 'Tajawal'),
                ),
                Spacer(),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: lightBlue,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    '${_dailyReports.length} يوم',
                    style: TextStyle(fontSize: 11, color: primaryBlue, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'),
                  ),
                ),
              ],
            ),
          ),
          Divider(height: 1, color: Color(0xFFE5E7EB)),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: DataTable(
              headingRowColor: WidgetStateProperty.all(Color(0xFFF1F5F9)),
              columnSpacing: 14,
              horizontalMargin: 10,
              headingTextStyle: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, fontSize: 11, color: darkBlue),
              columns: [
                DataColumn(label: Text('اليوم')),
                DataColumn(label: Text('التاريخ')),
                DataColumn(label: Text('الحضور')),
                DataColumn(label: Text('الانصراف')),
                DataColumn(label: Text('التأخير')),
                DataColumn(label: Text('الإضافي')),
                DataColumn(label: Text('ساعات')),
                DataColumn(label: Text('الحالة')),
              ],
              rows: _dailyReports.map((day) {
                final date = day['date'] != null ? DateTime.parse(day['date'].toString()) : null;
                final dateKey = date != null ? '${date.year}-${date.month}-${date.day}' : '';
                final breakMinutes = _breakByDay[dateKey] ?? 0.0;
                final workHours = (day['workHours'] is num) ? (day['workHours'] as num).toDouble() : 0.0;
                final actualWorkHours = workHours - (breakMinutes / 60.0);
                final status = _getStatusText(day['status']);
                final statusColor = _getStatusColor(status);

                return DataRow(
                  color: WidgetStateProperty.resolveWith<Color?>((states) {
                    if (status.contains('غائب')) return absentColor.withValues(alpha: 0.03);
                    if (status.contains('إجازة')) return leaveColor.withValues(alpha: 0.03);
                    if (status.contains('عطلة')) return holidayColor.withValues(alpha: 0.03);
                    if (status.contains('راحة')) return restColor.withValues(alpha: 0.03);
                    return null;
                  }),
                  cells: [
                    DataCell(Text(day['dayOfWeek'] ?? '—', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11))),
                    DataCell(Text(date != null ? '${date.day}/${date.month}' : '—', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11))),
                    DataCell(Text(_formatTime(day['checkIn']), style: TextStyle(fontFamily: 'Tajawal', fontSize: 11))),
                    DataCell(Text(_formatTime(day['checkOut']), style: TextStyle(fontFamily: 'Tajawal', fontSize: 11))),
                    DataCell(Text(_formatDuration(day['lateMinutes'] ?? 0), style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: (day['lateMinutes'] ?? 0) > 0 ? lateColor : Colors.grey[400]))),
                    DataCell(Text(_formatDuration(day['overtimeMinutes'] ?? 0), style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: (day['overtimeMinutes'] ?? 0) > 0 ? leaveColor : Colors.grey[400]))),
                    DataCell(Text(
                      actualWorkHours > 0 ? _formatWorkHours(actualWorkHours) : '—',
                      style: TextStyle(
                        fontFamily: 'Tajawal',
                        fontSize: 11,
                        color: actualWorkHours >= 8 ? presentColor : lateColor,
                        fontWeight: FontWeight.bold,
                      ),
                    )),
                    DataCell(
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: statusColor.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(color: statusColor.withValues(alpha: 0.3), width: 0.5),
                        ),
                        child: Text(
                          status,
                          style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: statusColor, fontWeight: FontWeight.bold),
                        ),
                      ),
                    ),
                  ],
                );
              }).toList(),
            ),
          ),
        ],
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
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'التقرير الشهري',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              Text(
                '${arabicMonths[_selectedMonth.month - 1]} ${_selectedMonth.year}',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8)),
              ),
            ],
          ),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: false,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(bottom: Radius.circular(20))),
        ),
        body: _isLoading
            ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    CircularProgressIndicator(color: primaryBlue),
                    SizedBox(height: 16),
                    Text('جاري تحميل التقرير...', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500])),
                  ],
                ),
              )
            : _errorMessage.isNotEmpty
                ? Center(
                    child: Padding(
                      padding: EdgeInsets.all(20),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Container(
                            width: 70,
                            height: 70,
                            decoration: BoxDecoration(color: absentColor.withValues(alpha: 0.1), shape: BoxShape.circle),
                            child: Icon(Icons.error_outline, color: absentColor, size: 35),
                          ),
                          SizedBox(height: 16),
                          Text(_errorMessage, textAlign: TextAlign.center, style: TextStyle(fontSize: 15, fontFamily: 'Tajawal', color: Colors.grey[700])),
                          SizedBox(height: 20),
                          ElevatedButton.icon(
                            onPressed: _loadReport,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: primaryBlue,
                              foregroundColor: Colors.white,
                              padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            ),
                            icon: Icon(Icons.refresh, size: 18),
                            label: Text('إعادة المحاولة', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
                          ),
                        ],
                      ),
                    ),
                  )
                : _dailyReports.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Container(
                              width: 70,
                              height: 70,
                              decoration: BoxDecoration(color: lightBlue, shape: BoxShape.circle),
                              child: Icon(Icons.calendar_today, color: primaryBlue, size: 35),
                            ),
                            SizedBox(height: 16),
                            Text('لا توجد بيانات لهذا الشهر', style: TextStyle(fontSize: 15, color: Colors.grey[600], fontFamily: 'Tajawal')),
                          ],
                        ),
                      )
                    : SingleChildScrollView(
                        padding: EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            _buildMonthSelector(),
                            SizedBox(height: 16),
                            _buildStatsGrid(),
                            SizedBox(height: 16),
                            _buildDailyTable(),
                          ],
                        ),
                      ),
      ),
    );
  }
}