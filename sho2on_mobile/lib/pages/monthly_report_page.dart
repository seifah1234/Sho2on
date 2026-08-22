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
  
  List<Map<String, dynamic>> _monthlyData = [];
  DateTime _selectedMonth = DateTime.now();
  bool _isLoading = false;
  String _errorMessage = '';
  
  final Color primaryColor = Color(0xFF1976D2);
  final Color presentColor = Color(0xFF4CAF50);
  final Color absentColor = Color(0xFFF44336);
  
  @override
  void initState() {
    super.initState();
    _loadMonthlyReport();
  }
  
  // دالة لتحويل التاريخ إلى نص عربي
  String _formatDate(DateTime date, {bool includeDay = false}) {
    String day = date.day.toString();
    String month = _getArabicMonth(date.month);
    String year = date.year.toString();
    
    if (includeDay) {
      String dayName = _getArabicDayName(date.weekday);
      return '$dayName $day $month $year';
    }
    
    return '$day $month $year';
  }
  
  // أسماء الأشهر العربية
  String _getArabicMonth(int month) {
    switch (month) {
      case 1: return 'يناير';
      case 2: return 'فبراير';
      case 3: return 'مارس';
      case 4: return 'أبريل';
      case 5: return 'مايو';
      case 6: return 'يونيو';
      case 7: return 'يوليو';
      case 8: return 'أغسطس';
      case 9: return 'سبتمبر';
      case 10: return 'أكتوبر';
      case 11: return 'نوفمبر';
      case 12: return 'ديسمبر';
      default: return '';
    }
  }
  
  // أسماء أيام الأسبوع العربية
  String _getArabicDayName(int weekday) {
    switch (weekday) {
      case 1: return 'الإثنين';
      case 2: return 'الثلاثاء';
      case 3: return 'الأربعاء';
      case 4: return 'الخميس';
      case 5: return 'الجمعة';
      case 6: return 'السبت';
      case 7: return 'الأحد';
      default: return '';
    }
  }
  
  // اسم الشهر الحالي بالعربية
  String _getCurrentMonthName() {
    int month = _selectedMonth.month;
    int year = _selectedMonth.year;
    return '${_getArabicMonth(month)} $year';
  }
  
  // تحويل الوقت من نص إلى TimeOfDay
  TimeOfDay _parseTime(String time) {
    if (time.isEmpty) return TimeOfDay(hour: 0, minute: 0);
    
    List<String> parts = time.split(':');
    if (parts.length >= 2) {
      return TimeOfDay(
        hour: int.tryParse(parts[0]) ?? 0,
        minute: int.tryParse(parts[1]) ?? 0,
      );
    }
    return TimeOfDay(hour: 0, minute: 0);
  }
  
  // حساب الفرق بين وقتين
  Duration _calculateTimeDifference(String start, String end) {
    if (start.isEmpty || end.isEmpty) return Duration.zero;
    
    TimeOfDay startTime = _parseTime(start);
    TimeOfDay endTime = _parseTime(end);
    
    DateTime startDateTime = DateTime(
      _selectedMonth.year,
      _selectedMonth.month,
      1,
      startTime.hour,
      startTime.minute,
    );
    
    DateTime endDateTime = DateTime(
      _selectedMonth.year,
      _selectedMonth.month,
      1,
      endTime.hour,
      endTime.minute,
    );
    
    // إذا كان وقت النهاية قبل وقت البداية (مثل وردية مسائية)
    if (endDateTime.isBefore(startDateTime)) {
      endDateTime = endDateTime.add(Duration(days: 1));
    }
    
    return endDateTime.difference(startDateTime);
  }
  
  // حساب التأخير
  int _calculateLateMinutes(String checkIn) {
    if (checkIn.isEmpty) return 0;
    
    TimeOfDay checkInTime = _parseTime(checkIn);
    int totalMinutes = (checkInTime.hour * 60 + checkInTime.minute);
    int expectedMinutes = (8 * 60 + 15); // الساعة 8:15
    
    if (totalMinutes > expectedMinutes) {
      return totalMinutes - expectedMinutes;
    }
    return 0;
  }
  
  // حساب الخروج المبكر
  int _calculateEarlyLeaveMinutes(String checkOut) {
    if (checkOut.isEmpty) return 0;
    
    TimeOfDay checkOutTime = _parseTime(checkOut);
    int totalMinutes = (checkOutTime.hour * 60 + checkOutTime.minute);
    int expectedMinutes = (16 * 60); // الساعة 16:00
    
    if (totalMinutes < expectedMinutes) {
      return expectedMinutes - totalMinutes;
    }
    return 0;
  }
  
  // حساب العمل الإضافي
  int _calculateOvertimeMinutes(String checkOut) {
    if (checkOut.isEmpty) return 0;
    
    TimeOfDay checkOutTime = _parseTime(checkOut);
    int totalMinutes = (checkOutTime.hour * 60 + checkOutTime.minute);
    int overtimeStart = (17 * 60); // بعد الساعة 17:00
    
    if (totalMinutes > overtimeStart) {
      return totalMinutes - overtimeStart;
    }
    return 0;
  }
  
  // تحويل بيانات الاستجابة إلى تنسيق مناسب للعرض
  void _processApiData(List<dynamic> apiData) {
  print('⚙️ Processing API data with ${apiData.length} items');
  
  if (apiData.isEmpty) {
    print('📭 API data is empty');
    setState(() {
      _monthlyData = [];
      _isLoading = false;
    });
    return;
  }
  
  List<Map<String, dynamic>> processedData = [];
  
  for (int i = 0; i < apiData[0]['dailyReports'].length; i++) {
    var item = apiData[0]['dailyReports'][i];
    print('📝 Processing item $i: $item');
    
    try {
      // تحقق مما إذا كان العنصر خريطة
      if (item is! Map<String, dynamic>) {
        print('⚠️ Skipping non-map item at index $i: ${item.runtimeType}');
        continue;
      }
      
      // تحقق من وجود تاريخ - قد يكون المفتاح مختلفاً
      String? dateString;
      if (item.containsKey('date')) {
        dateString = item['date']?.toString();
      }
      
      if (dateString == null || dateString.isEmpty) {
        print('⚠️ Skipping item without date at index $i');
        continue;
      }
      
      DateTime date;
      try {
        date = DateTime.parse(dateString);
        print('📅 Parsed date: $date');
      } catch (e) {
        print('❌ Error parsing date "$dateString": $e');
        continue;
      }
      
      // تحويل الحالة - تحقق من المفاتيح المختلفة
      dynamic statusValue;
      if (item.containsKey('status')) {
        statusValue = item['status'];
      }
      
      String status = _getStatusText(statusValue);
      print('✅ Status: $status');
      
      // حساب الأوقات - تحقق من المفاتيح المختلفة
      String checkIn = '';
      String checkOut = '';
      
      if (item.containsKey('checkIn')) {
        checkIn = item['checkIn']?.toString() ?? '';
      } 
      
      if (item.containsKey('checkOut')) {
        checkOut = item['checkOut']?.toString() ?? '';
      }
      
      print('⏰ CheckIn: $checkIn, CheckOut: $checkOut');
      
      // إذا كانت الأوقات تأتي كـ DateTime كامل
      if (checkIn.contains('T')) {
        try {
          DateTime checkInTime = DateTime.parse(checkIn);
          checkIn = '${checkInTime.hour.toString().padLeft(2, '0')}:${checkInTime.minute.toString().padLeft(2, '0')}';
          print('🔄 Converted CheckIn to: $checkIn');
        } catch (e) {
          print('❌ Error parsing checkIn time: $e');
        }
      }
      
      if (checkOut.contains('T')) {
        try {
          DateTime checkOutTime = DateTime.parse(checkOut);
          checkOut = '${checkOutTime.hour.toString().padLeft(2, '0')}:${checkOutTime.minute.toString().padLeft(2, '0')}';
          print('🔄 Converted CheckOut to: $checkOut');
        } catch (e) {
          print('❌ Error parsing checkOut time: $e');
        }
      }
      
      // حساب المؤشرات
      int lateMinutes = _calculateLateMinutes(checkIn);
      int earlyLeaveMinutes = _calculateEarlyLeaveMinutes(checkOut);
      int overtimeMinutes = _calculateOvertimeMinutes(checkOut);
      
      // حساب ساعات العمل
      Duration workDuration = _calculateTimeDifference(checkIn, checkOut);
      double workHours = workDuration.inMinutes / 60.0;
      
      // الملاحظات - تحقق من المفاتيح المختلفة
      String notes = '';
      if (item.containsKey('notes')) {
        notes = item['notes']?.toString() ?? '';
      } 
      
      processedData.add({
        'date': date,
        'dayOfWeek': _getArabicDayName(date.weekday),
        'dateFormatted': _formatDate(date, includeDay: true),
        'status': status,
        'checkIn': checkIn.isNotEmpty ? checkIn.substring(0, 5) : '--:--',
        'checkOut': checkOut.isNotEmpty ? checkOut.substring(0, 5) : '--:--',
        'lateMinutes': lateMinutes,
        'earlyLeaveMinutes': earlyLeaveMinutes,
        'overtimeMinutes': overtimeMinutes,
        'workHours': workHours,
        'notes': notes,
      });
      
      print('✅ Successfully processed item $i');
    } catch (e) {
      print('❌ Error processing data item at index $i: $e');
      print('❌ Item: $item');
    }
  }
  
  // ترتيب البيانات حسب التاريخ (من الأحدث إلى الأقدم)
  processedData.sort((a, b) => a['date'].compareTo(b['date']));
  
  print('🎉 Processed ${processedData.length} items successfully');
  print('📊 Final data: ${processedData.map((d) => d['dateFormatted']).toList()}');
  
  setState(() {
    _monthlyData = processedData;
    _isLoading = false;
  });
}

  // تحويل رمز الحالة إلى نص

  String _getStatusText(dynamic status) {
    if (status == null) return 'غائب';
    
    
    switch (status) {
      case 'حاضر': return 'حاضر';
      case 'إجازة': return 'إجازة';
      case 'عمل من المنزل': return 'عمل من المنزل';
      case 'مأمورية': return 'مأمورية';
      default: return 'غائب';
    }
  }

  void _handleApiResponse(Map<String, dynamic> result) {
  if (result['success'] == true) {
    dynamic responseData = result['data'];
    
    if (responseData is List) {
      _processApiData(responseData);
    } else if (responseData is Map) {
      // إذا كانت خريطة واحدة، حولها إلى قائمة
      _processApiData([responseData]);
    } else {
      setState(() {
        _errorMessage = 'هيكل البيانات غير متوقع';
        _isLoading = false;
      });
    }
  } else {
    setState(() {
      _errorMessage = result['message'] ?? 'حدث خطأ أثناء تحميل التقرير';
      _isLoading = false;
    });
  }
}
  
  Future<void> _loadMonthlyReport() async {
  setState(() {
    _isLoading = true;
    _errorMessage = '';
  });
  
  try {
    int userId = widget.user['id'] ?? 0;
    int year = _selectedMonth.year;
    int month = _selectedMonth.month;
    
    print('Loading report for user $userId, year $year, month $month');
    
    final result = await _attendanceService.getMonthlyReport(
      userId: userId,
      year: year,
      month: month,
    );
        
    _handleApiResponse(result);
  } catch (e) {
    print('Error in _loadMonthlyReport: $e');
    setState(() {
      _errorMessage = 'خطأ في الاتصال: $e';
      _isLoading = false;
    });
  }
}
  Future<void> _showMonthPicker() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _selectedMonth,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
      initialDatePickerMode: DatePickerMode.year,
    );
    
    if (picked != null && picked != _selectedMonth) {
      setState(() {
        _selectedMonth = DateTime(picked.year, picked.month, 1);
      });
      _loadMonthlyReport();
    }
  }
  
  Widget _buildMonthSelector() {
    return Container(
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        boxShadow: [
          BoxShadow(
            color: Colors.black12,
            blurRadius: 4,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: ListTile(
        leading: _isLoading
            ? SizedBox(
                width: 24,
                height: 24,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Icon(Icons.calendar_today, color: primaryColor),
        title: Text(
          'الشهر',
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.grey[700],
            fontFamily: 'Tajawal',
          ),
        ),
        subtitle: Text(
          _getCurrentMonthName(),
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: primaryColor,
            fontFamily: 'Tajawal',
          ),
        ),
        trailing: Icon(Icons.arrow_drop_down, color: primaryColor),
        onTap: _showMonthPicker,
      ),
    );
  }
  
  Widget _buildSummaryCard() {
    if (_monthlyData.isEmpty) return SizedBox();
    
    int totalDays = _monthlyData.length;
    int presentDays = _monthlyData.where((d) => d['status'] == 'حاضر').length;
    int absentDays = _monthlyData.where((d) => d['status'] == 'غائب').length;
    int lateDays = _monthlyData.where((d) => (d['lateMinutes'] as int) > 0).length;
    
    double totalWorkHours = _monthlyData.fold(0.0, (sum, d) => sum + (d['workHours'] as double));
    int totalOvertime = _monthlyData.fold(0, (sum, d) => sum + (d['overtimeMinutes'] as int));
    
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'ملخص الشهر',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 12),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                _buildSummaryItem('الأيام', totalDays.toString(), Icons.calendar_today),
                _buildSummaryItem('الحضور', presentDays.toString(), Icons.check_circle, presentColor),
                _buildSummaryItem('الغياب', absentDays.toString(), Icons.cancel, absentColor),
              ],
            ),
            
            SizedBox(height: 12),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                _buildSummaryItem('ساعات العمل', '${totalWorkHours.toStringAsFixed(1)}h', Icons.access_time),
                _buildSummaryItem('أيام التأخير', lateDays.toString(), Icons.watch_later, Colors.orange),
                _buildSummaryItem('العمل الإضافي', '${(totalOvertime/60).toStringAsFixed(1)}h', Icons.add, Colors.purple),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildSummaryItem(String label, String value, IconData icon, [Color? color]) {
    return Column(
      children: [
        Icon(icon, color: color ?? primaryColor, size: 28),
        SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.bold,
            color: Colors.grey[800],
            fontFamily: 'Tajawal',
          ),
        ),
        Text(
          label,
          style: TextStyle(
            fontSize: 12,
            color: Colors.grey[600],
            fontFamily: 'Tajawal',
          ),
        ),
      ],
    );
  }
  
  Widget _buildDailyCard(Map<String, dynamic> dayData) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      margin: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // التاريخ
            Text(
              dayData['dateFormatted'],
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 16,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            
            SizedBox(height: 12),
            
            // الحالة
            Container(
              padding: EdgeInsets.symmetric(horizontal: 12, vertical: 4),
              decoration: BoxDecoration(
                color: dayData['status'] == 'حاضر' ? presentColor.withValues(alpha: 0.1) : absentColor.withValues(alpha: 0.1),
                borderRadius: BorderRadius.circular(20),
                border: Border.all(
                  color: dayData['status'] == 'حاضر' ? presentColor : absentColor,
                  width: 1,
                ),
              ),
              child: Text(
                dayData['status'],
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  color: dayData['status'] == 'حاضر' ? presentColor : absentColor,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
            ),
            
            SizedBox(height: 16),
            
            // أوقات الدخول والانصراف
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                Column(
                  children: [
                    Text(
                      'الدخول',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey[600],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      dayData['checkIn'],
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: Colors.green[700],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
                
                Icon(Icons.arrow_forward, color: primaryColor),
                
                Column(
                  children: [
                    Text(
                      'الانصراف',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.grey[600],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    SizedBox(height: 4),
                    Text(
                      dayData['checkOut'],
                      style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                        color: Colors.red[700],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ],
            ),
            
            SizedBox(height: 16),
            
            // التفاصيل
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Divider(),
                SizedBox(height: 8),
                Text(
                  'التفاصيل',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: Colors.grey[700],
                    fontFamily: 'Tajawal',
                  ),
                ),
                SizedBox(height: 8),
                
                if (dayData['lateMinutes'] > 0)
                  _buildDetailRow('تأخير', '${dayData['lateMinutes']} دقيقة', Colors.orange),
                
                if (dayData['earlyLeaveMinutes'] > 0)
                  _buildDetailRow('خروج مبكر', '${dayData['earlyLeaveMinutes']} دقيقة', Colors.red),
                
                if (dayData['overtimeMinutes'] > 0)
                  _buildDetailRow('عمل إضافي', '${dayData['overtimeMinutes']} دقيقة', Colors.purple),
                
                _buildDetailRow('ساعات العمل', '${(dayData['workHours'] as double).toStringAsFixed(1)} ساعة', primaryColor),
              ],
            ),
            
            if (dayData['notes'] != null && dayData['notes'].isNotEmpty)
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  SizedBox(height: 12),
                  Text(
                    'ملاحظات: ${dayData['notes']}',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontSize: 12,
                      color: Colors.grey[600],
                      fontFamily: 'Tajawal',
                    ),
                  ),
                ],
              ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildDetailRow(String label, String value, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        textDirection: TextDirection.rtl,
        children: [
          Text(
            value,
            style: TextStyle(
              fontWeight: FontWeight.bold,
              color: color,
              fontFamily: 'Tajawal',
            ),
          ),
          Text(
            label,
            style: TextStyle(
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
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
        appBar: AppBar(
          title: Text('تقرير الشهر'),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
          centerTitle: true,
        ),
        body: _isLoading
            ? Center(child: CircularProgressIndicator())
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
                            style: TextStyle(fontSize: 16),
                          ),
                          SizedBox(height: 20),
                          ElevatedButton(
                            onPressed: _loadMonthlyReport,
                            child: Text('إعادة المحاولة'),
                          ),
                        ],
                      ),
                    ),
                  )
                : _monthlyData.isEmpty
                    ? Center(
                        child: Text(
                          'لا توجد بيانات لهذا الشهر',
                          style: TextStyle(fontSize: 16, color: Colors.grey),
                        ),
                      )
                    : SingleChildScrollView(
                        child: Column(
                          children: [
                            _buildMonthSelector(),
                            _buildSummaryCard(),
                            ..._monthlyData.map((day) => _buildDailyCard(day)),
                          ],
                        ),
                      ),
      ),
    );
  }
}