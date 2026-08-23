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
  Map<String, dynamic>? _report;
  List<Map<String, dynamic>> _dailyReports = [];
  Map<String, dynamic>? _summary;
  
  // بيانات الموظف
  String _employeeName = '';
  String _employeeCode = '';
  String _branchName = '';
  
  // بيانات البريك
  Map<String, double> _breakByDay = {}; // Date -> break minutes
  
  // حالة التحميل
  bool _isLoading = false;
  String _errorMessage = '';
  
  // الشهر والسنة المختارين
  DateTime _selectedMonth = DateTime.now();
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color presentColor = Color(0xFF4CAF50);
  final Color absentColor = Color(0xFFF44336);
  final Color leaveColor = Color(0xFF9C27B0);
  final Color holidayColor = Color(0xFFFF9800);
  final Color restColor = Color(0xFF607D8B);
  final Color backgroundColor = Color(0xFFF5F7FA);
  final Color cardColor = Colors.white;
  
  // أسماء الشهور العربية
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
        
        // تحميل بيانات البريك
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
    
    // استخراج البيانات
    Map<String, dynamic> report;
    if (reportData is List && reportData.isNotEmpty) {
      report = reportData[0] as Map<String, dynamic>;
    } else if (reportData is Map<String, dynamic>) {
      report = reportData;
    } else {
      setState(() {
        _errorMessage = 'هيكل البيانات غير متوقع';
        _isLoading = false;
      });
      return;
    }
    
    // استخراج معلومات الموظف
    _employeeName = report['employeeName'] ?? widget.user['fullName'] ?? '';
    _employeeCode = report['employeeCode'] ?? widget.user['code'] ?? '';
    _branchName = report['branchName'] ?? widget.user['branch']?['name'] ?? '';
    
    // استخراج التقرير اليومي
    List<dynamic> dailyReportsRaw = report['dailyReports'] ?? [];
    List<Map<String, dynamic>> processedReports = [];
    
    for (var day in dailyReportsRaw) {
      if (day is Map) {
        processedReports.add(Map<String, dynamic>.from(day));
      }
    }
    
    // استخراج الملخص
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
        // معالجة بيانات البريك
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
              
              if (breakMap.containsKey(dateKey)) {
                breakMap[dateKey] = breakMap[dateKey]! + duration;
              } else {
                breakMap[dateKey] = duration;
              }
            }
          }
        }
        
        setState(() {
          _breakByDay = breakMap;
        });
      }
    } catch (e) {
      // تجاهل أخطاء البريك
    }
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
              primary: primaryColor,
              onPrimary: Colors.white,
              surface: Colors.white,
              onSurface: Colors.black,
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
        _selectedMonth = DateTime(picked.year, picked.month, 1);
      });
      _loadReport();
    }
  }
  
  String _formatTime(String? time) {
    if (time == null || time.isEmpty) return '—';
    
    // تحويل من HH:mm إلى HH:mm AM/PM
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
  
  Widget _buildMonthSelector() {
    return Container(
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Row(
        textDirection: TextDirection.rtl,
        children: [
          Icon(Icons.calendar_month, color: primaryColor),
          SizedBox(width: 8),
          Expanded(
            child: Text(
              '${arabicMonths[_selectedMonth.month - 1]} ${_selectedMonth.year}',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
          ),
          IconButton(
            icon: Icon(Icons.chevron_left, color: primaryColor),
            onPressed: () {
              setState(() {
                _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month - 1, 1);
              });
              _loadReport();
            },
          ),
          IconButton(
            icon: Icon(Icons.chevron_right, color: primaryColor),
            onPressed: () {
              setState(() {
                _selectedMonth = DateTime(_selectedMonth.year, _selectedMonth.month + 1, 1);
              });
              _loadReport();
            },
          ),
          TextButton(
            onPressed: _selectMonth,
            child: Text(
              'اختيار',
              style: TextStyle(
                color: primaryColor,
                fontFamily: 'Tajawal',
                fontWeight: FontWeight.bold,
              ),
            ),
          ),
        ],
      ),
    );
  }
  
  Widget _buildEmployeeHeader() {
    return Container(
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [primaryColor, Color(0xFF42A5F5)],
        ),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: primaryColor.withOpacity(0.3),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Row(
        textDirection: TextDirection.rtl,
        children: [
          CircleAvatar(
            radius: 25,
            backgroundColor: Colors.white.withOpacity(0.2),
            child: Text(
              _employeeName.isNotEmpty ? _employeeName[0] : '?',
              style: TextStyle(
                color: Colors.white,
                fontSize: 20,
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
                  _employeeName,
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: Colors.white,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Text(
                  '$_employeeCode — $_branchName',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.white.withOpacity(0.8),
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
          ),
          Icon(Icons.calendar_today, color: Colors.white, size: 20),
        ],
      ),
    );
  }
  
  Widget _buildStatsGrid() {
    if (_summary == null) return SizedBox.shrink();
    
    final summary = _summary!;
    
    final stats = [
      {
        'label': 'أيام الغياب',
        'value': '${summary['absentDays'] ?? summary['totalAbsenceDays'] ?? 0}',
        'icon': Icons.person_off,
        'color': absentColor,
      },
      {
        'label': 'الراحة الأسبوعية',
        'value': '${summary['restDays'] ?? summary['totalWeeklyRestDays'] ?? 0}',
        'icon': Icons.weekend,
        'color': restColor,
      },
      {
        'label': 'أيام العطلة',
        'value': '${summary['holidayDays'] ?? summary['totalHolidayDays'] ?? 0}',
        'icon': Icons.beach_access,
        'color': holidayColor,
      },
      {
        'label': 'التأخير',
        'value': _formatDuration(summary['totalLateMinutes'] ?? 0),
        'icon': Icons.access_time,
        'color': Colors.orange,
      },
      {
        'label': 'الإضافي',
        'value': _formatDuration(summary['totalOvertimeMinutes'] ?? 0),
        'icon': Icons.timer,
        'color': Colors.purple,
      },
      {
        'label': 'ساعات العمل',
        'value': _formatWorkHours(summary['totalWorkHours'] ?? 0),
        'icon': Icons.work,
        'color': primaryColor,
      },
    ];
    
    return GridView(
      shrinkWrap: true,
      physics: NeverScrollableScrollPhysics(),
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 3,
        crossAxisSpacing: 10,
        mainAxisSpacing: 10,
        childAspectRatio: 1.1,
      ),
      children: stats.map((stat) {
        return Container(
          decoration: BoxDecoration(
            color: cardColor,
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: (stat['color'] as Color).withOpacity(0.2)),
            boxShadow: [
              BoxShadow(
                color: Colors.black.withOpacity(0.03),
                blurRadius: 5,
                offset: Offset(0, 2),
              ),
            ],
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: (stat['color'] as Color).withOpacity(0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  stat['icon'] as IconData,
                  color: stat['color'] as Color,
                  size: 20,
                ),
              ),
              SizedBox(height: 6),
              Text(
                stat['value']!.toString(),
                style: TextStyle(
                  fontSize: 14,
                  fontWeight: FontWeight.bold,
                  color: Colors.grey[800],
                  fontFamily: 'Tajawal',
                ),
              ),
              SizedBox(height: 2),
              Text(
                stat['label']!.toString(),
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
      }).toList(),
    );
  }
  
  Widget _buildDailyTable() {
    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 10,
            offset: Offset(0, 3),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          Padding(
            padding: EdgeInsets.all(16),
            child: Row(
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'تفاصيل الأيام',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                Spacer(),
                Text(
                  '${_dailyReports.length} يوم',
                  style: TextStyle(
                    fontSize: 12,
                    color: Colors.grey[600],
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
          ),
          Divider(height: 1),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: DataTable(
              headingRowColor: WidgetStateProperty.all(Colors.grey[50]),
              columnSpacing: 16,
              horizontalMargin: 12,
              columns: [
                DataColumn(label: Text('اليوم', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('التاريخ', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('الحضور', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('الانصراف', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('التأخير', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('الإضافي', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('ساعات العمل', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
                DataColumn(label: Text('الحالة', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold))),
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
                    if (status.contains('غائب')) return Colors.red.withOpacity(0.05);
                    if (status.contains('إجازة')) return Colors.purple.withOpacity(0.05);
                    if (status.contains('عطلة')) return Colors.orange.withOpacity(0.05);
                    if (status.contains('راحة')) return Colors.grey.withOpacity(0.05);
                    return null;
                  }),
                  cells: [
                    DataCell(Text(day['dayOfWeek'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(date != null ? '${date.day}/${date.month}/${date.year}' : '—', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(_formatTime(day['checkIn']), style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(_formatTime(day['checkOut']), style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(_formatDuration(day['lateMinutes'] ?? 0), style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(_formatDuration(day['overtimeMinutes'] ?? 0), style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    DataCell(Text(
                      actualWorkHours > 0 ? _formatWorkHours(actualWorkHours) : '—',
                      style: TextStyle(
                        fontFamily: 'Tajawal',
                        fontSize: 12,
                        color: actualWorkHours >= 8 ? Colors.green : Colors.orange,
                        fontWeight: FontWeight.bold,
                      ),
                    )),
                    DataCell(
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                        decoration: BoxDecoration(
                          color: statusColor.withOpacity(0.1),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: statusColor, width: 0.5),
                        ),
                        child: Text(
                          status,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 11,
                            color: statusColor,
                            fontWeight: FontWeight.bold,
                          ),
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
          title: Text(
            'تقرير الموظف الشهري',
            style: TextStyle(
              fontWeight: FontWeight.bold,
              fontFamily: 'Tajawal',
            ),
          ),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
        ),
        body: _isLoading
            ? Center(
                child: CircularProgressIndicator(color: primaryColor),
              )
            : _errorMessage.isNotEmpty
                ? Center(
                    child: Padding(
                      padding: const EdgeInsets.all(20.0),
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.error_outline, color: Colors.red, size: 48),
                          SizedBox(height: 16),
                          Text(
                            _errorMessage,
                            textAlign: TextAlign.center,
                            style: TextStyle(fontSize: 16, fontFamily: 'Tajawal'),
                          ),
                          SizedBox(height: 20),
                          ElevatedButton(
                            onPressed: _loadReport,
                            style: ElevatedButton.styleFrom(
                              backgroundColor: primaryColor,
                              foregroundColor: Colors.white,
                            ),
                            child: Text('إعادة المحاولة', style: TextStyle(fontFamily: 'Tajawal')),
                          ),
                        ],
                      ),
                    ),
                  )
                : _dailyReports.isEmpty
                    ? Center(
                        child: Text(
                          'لا توجد بيانات لهذا الشهر',
                          style: TextStyle(fontSize: 16, color: Colors.grey, fontFamily: 'Tajawal'),
                        ),
                      )
                    : SingleChildScrollView(
                        padding: EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            _buildMonthSelector(),
                            SizedBox(height: 16),
                            _buildEmployeeHeader(),
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