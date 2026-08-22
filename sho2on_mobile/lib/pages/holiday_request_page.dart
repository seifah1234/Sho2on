import 'package:flutter/material.dart';
import '../services/holiday_service.dart';

class HolidayRequestPage extends StatefulWidget {
  final Map user;
  const HolidayRequestPage({super.key, required this.user});

  @override
  _HolidayRequestPageState createState() => _HolidayRequestPageState();
}

class _HolidayRequestPageState extends State<HolidayRequestPage> {
  final HolidayService _holidayService = HolidayService();
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  
  // بيانات النموذج
  int? _selectedLeaveTypeId;
  DateTime? _startDate;
  DateTime? _endDate;
  int _duration = 0;
  String _reason = '';
  Map<String, dynamic>? _selectedManager;
  int? _selectedJobTitleId;
  
  // قوائم البيانات
  List<dynamic> _leaveTypes = [];
  List<dynamic> _managers = [];
  final List<dynamic> _jobTitles = [];
  
  // معلومات الرصيد
  Map<String, dynamic>? _leaveBalance;
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isSubmitting = false;
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color secondaryColor = Color(0xFF42A5F5);
  final Color accentColor = Color(0xFF4CAF50);
  final Color errorColor = Color(0xFFF44336);
  final Color backgroundColor = Color(0xFFF5F7FA);
  final Color cardColor = Colors.white;
  
  @override
  void initState() {
    super.initState();
    _loadInitialData();
  }
  
  Future<void> _loadInitialData() async {
    setState(() => _isLoading = true);
    
    try {
      // تحميل أنواع الإجازات
      final leaveTypesResult = await _holidayService.getLeaveTypes();
      if (leaveTypesResult['success']) {
        setState(() {
          _leaveTypes = leaveTypesResult['data'];
          if (_leaveTypes.isNotEmpty) {
            _selectedLeaveTypeId = _leaveTypes[0]['id'];
            _loadLeaveBalance();
          }
        });
      }
      
      // تحميل المسميات الوظيفية (للفلتر)
      // يمكنك تحميلها من API آخر
      
      // تحميل المديرين
      await _loadManagers();
      
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _loadLeaveBalance() async {
    if (_selectedLeaveTypeId == null) return;
    
    final result = await _holidayService.getLeaveBalance(
      widget.user['id'],
      _selectedLeaveTypeId!,
    );
    
    if (result['success']) {
      setState(() {
        _leaveBalance = result['data'];
      });
    }
  }
  
  Future<void> _loadManagers({int? jobTitleId}) async {
  final result = await _holidayService.getManagers(jobTitleId: jobTitleId);
  
  if (result['success']) {
    setState(() {
      _managers = result['data'];
      
      // اختيار مدير الموظف كافتراضي
      if (widget.user['managerId'] != null) {
        final managerId = widget.user['managerId'];
        _selectedManager = _managers.firstWhere(
          (manager) => manager['id'] == managerId,
          orElse: () => _managers.isNotEmpty ? _managers[0] : null,
        );
      } else if (_managers.isNotEmpty) {
        _selectedManager = _managers[0];
      }
    });
  }
}
  
  Future<void> _checkDateConflicts() async {
    if (_startDate == null || _endDate == null) return;
    
    final result = await _holidayService.checkDateConflicts(
      widget.user['id'],
      _startDate!,
      _endDate!,
    );
    
    if (result['success'] && result['hasConflicts'] == true) {
      _showWarningDialog('تعارض في التواريخ', result['message']);
    }
  }
  
  void _calculateDuration() {
    if (_startDate != null && _endDate != null) {
      if (_endDate!.isAfter(_startDate!) || _endDate!.isAtSameMomentAs(_startDate!)) {
        final difference = _endDate!.difference(_startDate!);
        setState(() {
          _duration = difference.inDays + 1; // +1 لتضمين اليوم الأول
        });
        
        // التحقق من تعارض التواريخ
        _checkDateConflicts();
      } else {
        _showError('تاريخ النهاية يجب أن يكون بعد تاريخ البداية');
      }
    }
  }
  
  Future<void> _selectStartDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now(),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(Duration(days: 365)),
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
        _startDate = picked;
        if (_endDate != null && _endDate!.isBefore(picked)) {
          _endDate = null;
          _duration = 0;
        }
      });
      _calculateDuration();
    }
  }
  
  Future<void> _selectEndDate() async {
    if (_startDate == null) {
      _showError('الرجاء تحديد تاريخ البداية أولاً');
      return;
    }
    
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _startDate!,
      firstDate: _startDate!,
      lastDate: DateTime.now().add(Duration(days: 365)),
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
      setState(() => _endDate = picked);
      _calculateDuration();
    }
  }
  
  Future<void> _selectManager() async {
    if (_managers.isEmpty) {
      _showError('لا يوجد مديرين متاحين');
      return;
    }
    
    final Map<String, dynamic>? selected = await showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Text('اختر الموافق على الإجازة'),
          content: SizedBox(
            width: double.maxFinite,
            height: 400,
            child: ListView.builder(
              itemCount: _managers.length,
              itemBuilder: (context, index) {
                final manager = _managers[index];
                return ListTile(
                  leading: Icon(Icons.person, color: primaryColor),
                  title: Text(
                    manager['fullName'],
                    textDirection: TextDirection.rtl,
                  ),
                  subtitle: Text(
                    '${manager['jobTitleName']} - ${manager['departmentName']}',
                    textDirection: TextDirection.rtl,
                  ),
                  onTap: () => Navigator.pop(context, manager),
                );
              },
            ),
          ),
        ),
      ),
    );
    
    if (selected != null) {
      setState(() => _selectedManager = selected);
    }
  }
  
  Future<void> _submitRequest() async {
    if (!_validateForm()) return;
    
    setState(() => _isSubmitting = true);
    
    try {
      final result = await _holidayService.submitHolidayRequest(
        employeeId: widget.user['id'],
        leaveTypeId: _selectedLeaveTypeId!,
        startDate: _startDate!,
        endDate: _endDate!,
        duration: _duration,
        reason: _reason,
        approvingManagerId: _selectedManager?['id'],
      );
      
      if (result['success']) {
        _showSuccessDialog('تم تقديم الطلب بنجاح', () {
          Navigator.pop(context, true);
        });
      } else {
        _showError(result['message'] ?? 'فشل في تقديم الطلب');
      }
    } catch (e) {
      _showError('خطأ في تقديم الطلب: $e');
    } finally {
      setState(() => _isSubmitting = false);
    }
  }
  
  bool _validateForm() {
    if (_selectedLeaveTypeId == null) {
      _showError('الرجاء اختيار نوع الإجازة');
      return false;
    }
    
    if (_startDate == null) {
      _showError('الرجاء تحديد تاريخ البداية');
      return false;
    }
    
    if (_endDate == null) {
      _showError('الرجاء تحديد تاريخ النهاية');
      return false;
    }
    
    if (_duration <= 0) {
      _showError('مدة الإجازة غير صحيحة');
      return false;
    }
    
    if (_reason.isEmpty) {
      _showError('الرجاء كتابة سبب الإجازة');
      return false;
    }
    
    // التحقق إذا كان نوع الإجازة يتطلب موافقة
    final selectedType = _leaveTypes.firstWhere(
      (type) => type['id'] == _selectedLeaveTypeId,
      orElse: () => {'requiresApproval': true},
    );
    
    if (selectedType['requiresApproval'] == true && _selectedManager == null) {
      _showError('الرجاء اختيار الموافق على الإجازة');
      return false;
    }
    
    return true;
  }
  
  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        backgroundColor: errorColor,
        duration: Duration(seconds: 3),
      ),
    );
  }
  
  void _showWarningDialog(String title, String message) {
    showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Row(
            children: [
              Icon(Icons.warning, color: Colors.orange),
              SizedBox(width: 10),
              Text(title),
            ],
          ),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('متابعة'),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('إلغاء'),
            ),
          ],
        ),
      ),
    );
  }
  
  void _showSuccessDialog(String message, VoidCallback onOk) {
    showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          title: Row(
            children: [
              Icon(Icons.check_circle, color: Colors.green),
              SizedBox(width: 10),
              Text('نجاح'),
            ],
          ),
          content: Text(message),
          actions: [
            TextButton(
              onPressed: onOk,
              child: Text('موافق'),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildLeaveTypeSelector() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'نوع الإجازة',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            Wrap(
              spacing: 10,
              runSpacing: 10,
              alignment: WrapAlignment.end,
              children: _leaveTypes.map((type) {
                return ChoiceChip(
                  label: Text(
                    type['name'],
                    style: TextStyle(
                      color: _selectedLeaveTypeId == type['id'] 
                          ? Colors.white 
                          : Colors.black,
                      fontFamily: 'Tajawal',
                    ),
                  ),
                  selected: _selectedLeaveTypeId == type['id'],
                  selectedColor: primaryColor,
                  backgroundColor: Colors.grey[200],
                  onSelected: (selected) {
                    setState(() {
                      _selectedLeaveTypeId = type['id'];
                      _loadLeaveBalance();
                    });
                  },
                );
              }).toList(),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildBalanceInfo() {
    if (_leaveBalance == null) return SizedBox.shrink();
    
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'رصيد الإجازات',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              textDirection: TextDirection.rtl,
              children: [
                _buildBalanceCard('الرصيد الكلي', 
                  '${_leaveBalance!['totalBalance']}', 
                  Icons.account_balance_wallet, 
                  Colors.blue),
                _buildBalanceCard('المستخدم', 
                  '${_leaveBalance!['usedBalance']}', 
                  Icons.airline_seat_recline_normal, 
                  Colors.orange),
                _buildBalanceCard('المتبقي', 
                  '${_leaveBalance!['remainingBalance']}', 
                  Icons.rotate_left, 
                  Colors.green),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildBalanceCard(String title, String value, IconData icon, Color color) {
    return Column(
      children: [
        Icon(icon, size: 40, color: color),
        SizedBox(height: 8),
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
  
  Widget _buildDateSelector() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'فترة الإجازة',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            Column(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              textDirection: TextDirection.rtl,
              children: [
                _buildDateField(
                  'من تاريخ',
                  _startDate == null 
                    ? 'اختر التاريخ' 
                    : '${_startDate!.year}/${_startDate!.month}/${_startDate!.day}',
                  _selectStartDate,
                  Icons.calendar_today,
                ),
                _buildDateField(
                  'إلى تاريخ',
                  _endDate == null 
                    ? 'اختر التاريخ' 
                    : '${_endDate!.year}/${_endDate!.month}/${_endDate!.day}',
                  _selectEndDate,
                  Icons.calendar_today,
                ),
                _buildDurationField(),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildDateField(String label, String value, VoidCallback onTap, IconData icon) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          label,
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.grey[700],
            fontFamily: 'Tajawal',
          ),
        ),
        SizedBox(height: 8),
        GestureDetector(
          onTap: onTap,
          child: Container(
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: Colors.grey[50],
              borderRadius: BorderRadius.circular(8),
              border: Border.all(color: Colors.grey[300]!),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              textDirection: TextDirection.rtl,
              children: [
                Icon(icon, color: primaryColor, size: 20),
                SizedBox(width: 8),
                Text(
                  value,
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    color: value == 'اختر التاريخ' 
                        ? Colors.grey[400] 
                        : Colors.black,
                    fontFamily: 'Tajawal',
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
  
  Widget _buildDurationField() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text(
          'المدة',
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.grey[700],
            fontFamily: 'Tajawal',
          ),
        ),
        SizedBox(height: 8),
        Container(
          padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          decoration: BoxDecoration(
            color: Colors.grey[50],
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: Colors.grey[300]!),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            textDirection: TextDirection.rtl,
            children: [
              Icon(Icons.timer, color: primaryColor, size: 20),
              SizedBox(width: 8),
              Text(
                '$_duration يوم',
                textDirection: TextDirection.rtl,
                style: TextStyle(
                  color: Colors.black,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
  
  Widget _buildManagerSelector() {
    final selectedType = _leaveTypes.firstWhere(
      (type) => type['id'] == _selectedLeaveTypeId,
      orElse: () => {'requiresApproval': false},
    );
    
    if (selectedType['requiresApproval'] != true) {
      return SizedBox.shrink();
    }
    
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
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
                  'الموافق على الإجازة',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.filter_list, color: primaryColor),
                  onPressed: () {
                    // عرض فلتر المسمى الوظيفي
                  },
                ),
              ],
            ),
            SizedBox(height: 16),
            
            if (_selectedManager == null)
              ElevatedButton.icon(
                onPressed: _selectManager,
                style: ElevatedButton.styleFrom(
                  backgroundColor: Colors.grey[200],
                  foregroundColor: Colors.grey[700],
                  padding: EdgeInsets.symmetric(horizontal: 24, vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
                icon: Icon(Icons.person_add, size: 20),
                label: Text(
                  'اختر الموافق على الإجازة',
                  style: TextStyle(fontFamily: 'Tajawal'),
                ),
              )
            else
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.green),
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            _selectedManager!['fullName'],
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Colors.green[700],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            '${_selectedManager!['jobTitleName']} - ${_selectedManager!['departmentName']}',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              color: Colors.grey[600],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                    ),
                    SizedBox(width: 16),
                    IconButton(
                      icon: Icon(Icons.edit, color: primaryColor),
                      onPressed: _selectManager,
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildReasonField() {
    return Card(
      elevation: 3,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'سبب الإجازة',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            TextFormField(
              maxLines: 4,
              textDirection: TextDirection.rtl,
              decoration: InputDecoration(
                hintText: 'اكتب سبب الإجازة هنا...',
                hintStyle: TextStyle(
                  color: Colors.grey[400],
                  fontFamily: 'Tajawal',
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: Colors.grey[300]!),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: Colors.grey[300]!),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide(color: primaryColor),
                ),
                filled: true,
                fillColor: Colors.grey[50],
              ),
              onChanged: (value) {
                setState(() => _reason = value);
              },
              validator: (value) {
                if (value == null || value.isEmpty) {
                  return 'الرجاء كتابة سبب الإجازة';
                }
                return null;
              },
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildActionButtons() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceEvenly,
      textDirection: TextDirection.rtl,
      children: [
        ElevatedButton.icon(
          onPressed: _isSubmitting ? null : _submitRequest,
          style: ElevatedButton.styleFrom(
            backgroundColor: accentColor,
            foregroundColor: Colors.white,
            padding: EdgeInsets.symmetric(horizontal: 32, vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
            elevation: 3,
          ),
          icon: Icon(Icons.send, size: 20),
          label: _isSubmitting 
              ? SizedBox(
                  width: 20,
                  height: 20,
                  child: CircularProgressIndicator(
                    strokeWidth: 2,
                    color: Colors.white,
                  ),
                )
              : Text(
                  'تقديم الطلب',
                  style: TextStyle(
                    fontSize: 16,
                    fontWeight: FontWeight.bold,
                    fontFamily: 'Tajawal',
                  ),
                ),
        ),
        
        ElevatedButton.icon(
          onPressed: () => Navigator.pop(context),
          style: ElevatedButton.styleFrom(
            backgroundColor: Colors.grey[300],
            foregroundColor: Colors.grey[700],
            padding: EdgeInsets.symmetric(horizontal: 32, vertical: 16),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
            elevation: 0,
          ),
          icon: Icon(Icons.cancel, size: 20),
          label: Text(
            'إلغاء',
            style: TextStyle(
              fontSize: 16,
              fontWeight: FontWeight.bold,
              fontFamily: 'Tajawal',
            ),
          ),
        ),
      ],
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
            'طلب إجازة',
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
        ),
        body: _isLoading
            ? Center(
                child: CircularProgressIndicator(
                  color: primaryColor,
                ),
              )
            : Form(
                key: _formKey,
                child: SingleChildScrollView(
                  padding: EdgeInsets.all(20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      // معلومات الموظف
                      Card(
                        elevation: 3,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Padding(
                          padding: const EdgeInsets.all(16.0),
                          child: Row(
                            textDirection: TextDirection.rtl,
                            children: [
                              CircleAvatar(
                                radius: 30,
                                backgroundColor: primaryColor,
                                child: Icon(
                                  Icons.person,
                                  size: 40,
                                  color: Colors.white,
                                ),
                              ),
                              SizedBox(width: 16),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text(
                                      widget.user['fullName'] ?? '',
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
                                      widget.user['department']?['name'] ?? 'غير محدد',
                                      textDirection: TextDirection.rtl,
                                      style: TextStyle(
                                        color: Colors.grey[600],
                                        fontFamily: 'Tajawal',
                                      ),
                                    ),
                                    Text(
                                      widget.user['jobTitle']?['name'] ?? 'غير محدد',
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
                      ),
                      
                      SizedBox(height: 20),
                      
                      // نوع الإجازة
                      _buildLeaveTypeSelector(),
                      
                      SizedBox(height: 20),
                      
                      // رصيد الإجازات
                      _buildBalanceInfo(),
                      
                      SizedBox(height: 20),
                      
                      // فترة الإجازة
                      _buildDateSelector(),
                      
                      SizedBox(height: 20),
                      
                      // الموافق على الإجازة
                      _buildManagerSelector(),
                      
                      SizedBox(height: 20),
                      
                      // سبب الإجازة
                      _buildReasonField(),
                      
                      SizedBox(height: 32),
                      
                      // أزرار الإجراء
                      _buildActionButtons(),
                      
                      SizedBox(height: 20),
                      
                      // ملاحظات
                      Container(
                        padding: EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: Colors.blue[50],
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.blue[100]!),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Row(
                              textDirection: TextDirection.rtl,
                              children: [
                                Icon(Icons.info, color: Colors.blue[700]),
                                SizedBox(width: 8),
                                Text(
                                  'ملاحظات هامة',
                                  textDirection: TextDirection.rtl,
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: Colors.blue[700],
                                    fontFamily: 'Tajawal',
                                  ),
                                ),
                              ],
                            ),
                            SizedBox(height: 8),
                            Text(
                              '• يرجى التأكد من صحة البيانات قبل التقديم\n'
                              '• سيتم إشعار الموافق على الإجازة\n'
                              '• يمكنك تتبع حالة الطلب من خلال سجل الإجازات',
                              textDirection: TextDirection.rtl,
                              style: TextStyle(
                                color: Colors.blue[700],
                                fontFamily: 'Tajawal',
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
}