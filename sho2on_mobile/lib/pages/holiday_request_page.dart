import 'package:flutter/material.dart';
import '../services/holiday_service.dart';
import 'leave_history_page.dart';

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
  String _notes = '';
  int? _approverId;
  int? _replacementUserId;
  
  // قوائم البيانات
  List<dynamic> _leaveTypes = [];
  List<dynamic> _allEmployees = [];
  List<dynamic> _managers = [];
  List<dynamic> _filteredEmployees = [];
  
  // معلومات الرصيد
  Map<String, dynamic>? _leaveBalance;
  Map<String, dynamic>? _selectedLeaveType;
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isSubmitting = false;
  String _employeeSearch = '';
  
  // Controllers
  final TextEditingController _reasonController = TextEditingController();
  final TextEditingController _notesController = TextEditingController();
  final TextEditingController _searchController = TextEditingController();
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color accentColor = Color(0xFF4CAF50);
  final Color errorColor = Color(0xFFF44336);
  final Color backgroundColor = Color(0xFFF5F7FA);
  final Color cardColor = Colors.white;
  final Color borderColor = Color(0xFFE0E0E0);
  
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
          _leaveTypes = leaveTypesResult['data'] ?? [];
        });
      }
      
      // تحميل المديرين
      await _loadManagers();
      
      // تحميل جميع الموظفين (للبحث عن البديل)
      await _loadAllEmployees();
      
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _loadManagers() async {
    try {
      final result = await _holidayService.getManagers();
      if (result['success']) {
        setState(() {
          _managers = result['data'] ?? [];
        });
      }
    } catch (e) {
      _showError('خطأ في تحميل المديرين: $e');
    }
  }
  
  Future<void> _loadAllEmployees() async {
    try {
      final result = await _holidayService.searchEmployees(searchTerm: '');
      if (result['success']) {
        setState(() {
          _allEmployees = result['data'] ?? [];
        });
      }
    } catch (e) {
      // تجاهل الخطأ
    }
  }
  
  Future<void> _searchEmployees(String searchTerm) async {
    try {
      final result = await _holidayService.searchEmployees(searchTerm: searchTerm);
      if (result['success']) {
        setState(() {
          _filteredEmployees = result['data'] ?? [];
        });
      }
    } catch (e) {
      _showError('خطأ في البحث عن الموظفين: $e');
    }
  }
  
  Future<void> _loadLeaveBalance() async {
    if (_selectedLeaveTypeId == null || _selectedLeaveTypeId == 0) {
      setState(() {
        _leaveBalance = null;
      });
      return;
    }
    
    try {
      final result = await _holidayService.getLeaveBalance(
        widget.user['id'],
        _selectedLeaveTypeId!,
      );
      
      if (result['success']) {
        setState(() {
          _leaveBalance = result['data'];
        });
      }
    } catch (e) {
      _showError('خطأ في تحميل الرصيد: $e');
    }
  }
  
  void _updateDuration() {
    if (_startDate == null || _endDate == null) {
      setState(() {
        _duration = 0;
      });
      return;
    }
    
    if (_endDate!.isBefore(_startDate!)) {
      setState(() {
        _duration = 0;
      });
      return;
    }
    
    setState(() {
      _duration = (_endDate!.difference(_startDate!).inDays) + 1;
    });
    
    _loadLeaveBalance();
  }
  
  Future<void> _selectStartDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _startDate ?? DateTime.now(),
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
        }
      });
      _updateDuration();
    }
  }
  
  Future<void> _selectEndDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _endDate ?? _startDate ?? DateTime.now(),
      firstDate: _startDate ?? DateTime.now(),
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
      _updateDuration();
    }
  }
  
  Future<void> _selectApprover() async {
    if (_managers.isEmpty) {
      _showError('لا يوجد مديرين متاحين');
      return;
    }
    
    final Map<String, dynamic>? selected = await showModalBottomSheet(
      context: context,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: Container(
          padding: EdgeInsets.all(20),
          height: MediaQuery.of(context).size.height * 0.6,
          child: Column(
            children: [
              Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              SizedBox(height: 16),
              Text(
                'اختر المسؤول عن الاعتماد',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                  color: primaryColor,
                ),
              ),
              SizedBox(height: 16),
              Expanded(
                child: ListView.builder(
                  itemCount: _managers.length,
                  itemBuilder: (context, index) {
                    final manager = _managers[index];
                    final isSelected = _approverId == manager['id'];
                    return Card(
                      elevation: isSelected ? 2 : 0,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                        side: BorderSide(
                          color: isSelected ? primaryColor : borderColor,
                          width: isSelected ? 2 : 1,
                        ),
                      ),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isSelected ? primaryColor : Colors.grey[300],
                          child: Text(
                            (manager['fullName'] ?? '?')[0],
                            style: TextStyle(color: Colors.white),
                          ),
                        ),
                        title: Text(
                          manager['fullName'] ?? '',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        subtitle: Text(
                          '${manager['jobTitleName'] ?? ''} - ${manager['departmentName'] ?? ''}',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(fontFamily: 'Tajawal', fontSize: 12),
                        ),
                        trailing: isSelected
                            ? Icon(Icons.check_circle, color: primaryColor)
                            : null,
                        onTap: () {
                          Navigator.pop(context, manager);
                        },
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
    
    if (selected != null) {
      setState(() => _approverId = selected['id']);
    }
  }
  
  Future<void> _selectReplacement() async {
    if (_allEmployees.isEmpty) {
      _showError('لا يوجد موظفين متاحين');
      return;
    }
    
    final Map<String, dynamic>? selected = await showModalBottomSheet(
      context: context,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: Container(
          padding: EdgeInsets.all(20),
          height: MediaQuery.of(context).size.height * 0.7,
          child: Column(
            children: [
              Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: Colors.grey[300],
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
              SizedBox(height: 16),
              Text(
                'اختر الموظف البديل',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                  color: primaryColor,
                ),
              ),
              SizedBox(height: 16),
              // Search field
              TextField(
                textDirection: TextDirection.rtl,
                decoration: InputDecoration(
                  hintText: 'ابحث عن موظف...',
                  hintStyle: TextStyle(fontFamily: 'Tajawal'),
                  prefixIcon: Icon(Icons.search, color: primaryColor),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: borderColor),
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: borderColor),
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(10),
                    borderSide: BorderSide(color: primaryColor),
                  ),
                ),
                onChanged: _searchEmployees,
              ),
              SizedBox(height: 16),
              Expanded(
                child: ListView.builder(
                  itemCount: _allEmployees.length,
                  itemBuilder: (context, index) {
                    final employee = _allEmployees[index];
                    // استبعاد الموظف الحالي
                    if (employee['id'] == widget.user['id']) {
                      return SizedBox.shrink();
                    }
                    final isSelected = _replacementUserId == employee['id'];
                    return Card(
                      elevation: isSelected ? 2 : 0,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                        side: BorderSide(
                          color: isSelected ? primaryColor : borderColor,
                          width: isSelected ? 2 : 1,
                        ),
                      ),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isSelected ? primaryColor : Colors.grey[300],
                          child: Text(
                            (employee['fullName'] ?? '?')[0],
                            style: TextStyle(color: Colors.white),
                          ),
                        ),
                        title: Text(
                          employee['fullName'] ?? '',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        subtitle: Text(
                          '${employee['code'] ?? ''}',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(fontFamily: 'Tajawal', fontSize: 12),
                        ),
                        trailing: isSelected
                            ? Icon(Icons.check_circle, color: primaryColor)
                            : null,
                        onTap: () {
                          Navigator.pop(context, employee);
                        },
                      ),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
    
    if (selected != null) {
      setState(() => _replacementUserId = selected['id']);
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
      approvingManagerId: _approverId,
    );
    
    if (result['success']) {
      final requiresApproval = _selectedLeaveType?['requiresApproval'] ?? true;
      final message = requiresApproval
          ? 'تم إرسال طلب الإجازة للاعتماد'
          : 'تم تسجيل الإجازة واعتمادها تلقائياً';
      
      _showSuccessDialog(message, () {
        // الانتقال إلى صفحة سجل الإجازات
        Navigator.pushReplacement(
          context,
          MaterialPageRoute(
            builder: (context) => LeaveHistoryPage(user: widget.user),
          ),
        );
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
    if (_selectedLeaveTypeId == null || _selectedLeaveTypeId == 0) {
      _showError('اختر نوع الإجازة');
      return false;
    }
    
    if (_startDate == null || _endDate == null) {
      _showError('حدد تاريخ بداية ونهاية الإجازة');
      return false;
    }
    
    if (_startDate!.isBefore(DateTime.now().subtract(Duration(days: 1)))) {
      _showError('لا يمكن تقديم طلب إجازة بتاريخ سابق');
      return false;
    }
    
    if (_endDate!.isBefore(_startDate!)) {
      _showError('تاريخ النهاية يجب أن يكون بعد تاريخ البداية');
      return false;
    }
    
    if (_duration <= 0) {
      _showError('مدة الإجازة غير صحيحة');
      return false;
    }
    
    if (_reason.isEmpty) {
      _showError('سبب الإجازة مطلوب');
      return false;
    }
    
    // التحقق من الحد الأقصى
    if (_selectedLeaveType != null && 
        _selectedLeaveType!['maxConsecutiveDays'] != null &&
        _duration > _selectedLeaveType!['maxConsecutiveDays']) {
      _showError('الحد الأقصى لهذا النوع هو ${_selectedLeaveType!['maxConsecutiveDays']} يوم');
      return false;
    }
    
    // التحقق من الرصيد
    if (_selectedLeaveType != null && 
        _selectedLeaveType!['deductFromBalance'] == true &&
        _leaveBalance != null &&
        _duration > (_leaveBalance!['remainingBalance'] ?? 0)) {
      _showError('الرصيد المتبقي غير كافٍ. المتبقي: ${_leaveBalance!['remainingBalance']} يوم');
      return false;
    }
    
    // التحقق من وجود مدير إذا كانت الإجازة تتطلب اعتماد
    if (_selectedLeaveType != null && 
        _selectedLeaveType!['requiresApproval'] == true &&
        _approverId == null) {
      _showError('اختر المسؤول عن اعتماد الإجازة');
      return false;
    }
    
    // التحقق من عدم اختيار الموظف نفسه كبديل
    if (_replacementUserId != null && _replacementUserId == widget.user['id']) {
      _showError('لا يمكن اختيار الموظف نفسه كبديل');
      return false;
    }
    
    return true;
  }
  
  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Row(
          children: [
            Icon(Icons.error_outline, color: Colors.white, size: 20),
            SizedBox(width: 10),
            Expanded(
              child: Text(
                message,
                textDirection: TextDirection.rtl,
                style: TextStyle(fontFamily: 'Tajawal'),
              ),
            ),
          ],
        ),
        backgroundColor: errorColor,
        duration: Duration(seconds: 3),
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(10),
        ),
        margin: EdgeInsets.all(16),
      ),
    );
  }
  
  void _showSuccessDialog(String message, VoidCallback onOk) {
  showDialog(
    context: context,
    barrierDismissible: false,
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
              'نجاح',
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontWeight: FontWeight.bold,
                color: Colors.green,
              ),
            ),
            SizedBox(width: 10),
            Container(
              padding: EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: Colors.green.withOpacity(0.1),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.check_circle, color: Colors.green, size: 20),
            ),
          ],
        ),
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        actions: [
          TextButton(
            onPressed: () {
              Navigator.pop(context); // إغلاق الديالوج
              onOk(); // تنفيذ الانتقال
            },
            style: TextButton.styleFrom(
              foregroundColor: primaryColor,
            ),
            child: Text(
              'موافق',
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
  
  Widget _buildEmployeeCard() {
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
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'الموظف',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            Container(
              padding: EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.blue[50],
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.blue[100]!),
              ),
              child: Row(
                textDirection: TextDirection.rtl,
                children: [
                  CircleAvatar(
                    radius: 25,
                    backgroundColor: primaryColor,
                    child: Text(
                      (widget.user['fullName'] ?? '?')[0],
                      style: TextStyle(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                        fontSize: 20,
                      ),
                    ),
                  ),
                  SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          widget.user['fullName'] ?? '',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: Colors.blue[900],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        Text(
                          '${widget.user['code'] ?? widget.user['id'] ?? ''}',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.blue[700],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                  ),
                  Icon(Icons.check, color: Colors.green, size: 24),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildLeaveDataCard() {
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
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'بيانات الإجازة',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            
            // نوع الإجازة
            Text(
              'نوع الإجازة *',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            Container(
              padding: EdgeInsets.symmetric(horizontal: 12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: borderColor),
              ),
              child: DropdownButtonFormField<int>(
                value: _selectedLeaveTypeId,
                items: [
                  DropdownMenuItem<int>(
                    value: 0,
                    child: Text('اختر نوع الإجازة', style: TextStyle(fontFamily: 'Tajawal')),
                  ),
                  ..._leaveTypes.map((type) {
                    return DropdownMenuItem<int>(
                      value: type['id'],
                      child: Text(
                        '${type['name']} (${type['code'] ?? ''})',
                        style: TextStyle(fontFamily: 'Tajawal'),
                      ),
                    );
                  }).toList(),
                ],
                onChanged: (value) {
                  setState(() {
                    _selectedLeaveTypeId = value;
                    if (value != null && value > 0) {
                      _selectedLeaveType = _leaveTypes.firstWhere(
                        (type) => type['id'] == value,
                        orElse: () => {},
                      );
                    } else {
                      _selectedLeaveType = null;
                    }
                  });
                  _loadLeaveBalance();
                },
                decoration: InputDecoration(
                  border: InputBorder.none,
                  hintStyle: TextStyle(fontFamily: 'Tajawal'),
                ),
                dropdownColor: Colors.white,
                icon: Icon(Icons.arrow_drop_down, color: primaryColor),
                isExpanded: true,
              ),
            ),
            
            // معلومات النوع
            if (_selectedLeaveType != null) ...[
              SizedBox(height: 12),
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: borderColor),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    _buildInfoRow('الحد الأقصى', 
                      _selectedLeaveType!['maxConsecutiveDays']?.toString() ?? 'لا يوجد'),
                    SizedBox(height: 4),
                    _buildInfoRow('الخصم من الرصيد', 
                      _selectedLeaveType!['deductFromBalance'] == true ? 'نعم' : 'لا'),
                    SizedBox(height: 4),
                    _buildInfoRow('يتطلب اعتماد', 
                      _selectedLeaveType!['requiresApproval'] == true ? 'نعم' : 'لا'),
                  ],
                ),
              ),
            ],
            
            // الرصيد
            if (_leaveBalance != null && _selectedLeaveType?['deductFromBalance'] == true) ...[
              SizedBox(height: 12),
              Container(
                padding: EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.green[50],
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: Colors.green[100]!),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceAround,
                  textDirection: TextDirection.rtl,
                  children: [
                    _buildBalanceItem('الإجمالي', _leaveBalance!['totalBalance']?.toString() ?? '0'),
                    _buildBalanceItem('المستخدم', _leaveBalance!['usedBalance']?.toString() ?? '0'),
                    _buildBalanceItem('المتبقي', _leaveBalance!['remainingBalance']?.toString() ?? '0'),
                  ],
                ),
              ),
            ],
            
            SizedBox(height: 20),
            
            // التواريخ
            Row(
              textDirection: TextDirection.rtl,
              children: [
                Expanded(
                  child: _buildDateField(
                    'من تاريخ *',
                    _startDate,
                    _selectStartDate,
                  ),
                ),
                SizedBox(width: 12),
                Expanded(
                  child: _buildDateField(
                    'إلى تاريخ *',
                    _endDate,
                    _selectEndDate,
                  ),
                ),
              ],
            ),
            
            SizedBox(height: 16),
            
            // المدة
            Container(
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: Colors.grey[50],
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: borderColor),
              ),
              child: Row(
                textDirection: TextDirection.rtl,
                children: [
                  Icon(Icons.timer, color: primaryColor, size: 20),
                  SizedBox(width: 8),
                  Text(
                    'المدة: $_duration يوم',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontWeight: FontWeight.bold,
                      color: Colors.grey[800],
                    ),
                  ),
                ],
              ),
            ),
            
            SizedBox(height: 20),
            
            // سبب الإجازة
            Text(
              'سبب الإجازة *',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            TextField(
              controller: _reasonController,
              maxLines: 4,
              textDirection: TextDirection.rtl,
              decoration: InputDecoration(
                hintText: 'أدخل سبب الإجازة...',
                hintStyle: TextStyle(
                  color: Colors.grey[400],
                  fontFamily: 'Tajawal',
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: borderColor),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: borderColor),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: primaryColor, width: 2),
                ),
                contentPadding: EdgeInsets.all(12),
              ),
              onChanged: (value) {
                setState(() => _reason = value);
              },
            ),
            
            SizedBox(height: 16),
            
            // ملاحظات
            Text(
              'ملاحظات',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            TextField(
              controller: _notesController,
              maxLines: 3,
              textDirection: TextDirection.rtl,
              decoration: InputDecoration(
                hintText: 'ملاحظات إضافية (اختياري)...',
                hintStyle: TextStyle(
                  color: Colors.grey[400],
                  fontFamily: 'Tajawal',
                ),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: borderColor),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: borderColor),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10),
                  borderSide: BorderSide(color: primaryColor, width: 2),
                ),
                contentPadding: EdgeInsets.all(12),
              ),
              onChanged: (value) {
                setState(() => _notes = value);
              },
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildInfoRow(String label, String value) {
    return Row(
      textDirection: TextDirection.rtl,
      children: [
        Text(
          '$label: ',
          style: TextStyle(
            fontFamily: 'Tajawal',
            fontSize: 12,
            color: Colors.grey[600],
          ),
        ),
        Text(
          value,
          style: TextStyle(
            fontFamily: 'Tajawal',
            fontSize: 12,
            fontWeight: FontWeight.bold,
            color: Colors.grey[800],
          ),
        ),
      ],
    );
  }
  
  Widget _buildBalanceItem(String label, String value) {
    return Column(
      children: [
        Text(
          label,
          style: TextStyle(
            fontFamily: 'Tajawal',
            fontSize: 11,
            color: Colors.grey[600],
          ),
        ),
        SizedBox(height: 2),
        Text(
          value,
          style: TextStyle(
            fontFamily: 'Tajawal',
            fontSize: 14,
            fontWeight: FontWeight.bold,
            color: Colors.green[800],
          ),
        ),
      ],
    );
  }
  
  Widget _buildDateField(String label, DateTime? date, VoidCallback onTap) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text(
          label,
          textDirection: TextDirection.rtl,
          style: TextStyle(
            fontWeight: FontWeight.bold,
            color: Colors.grey[700],
            fontFamily: 'Tajawal',
            fontSize: 13,
          ),
        ),
        SizedBox(height: 8),
        GestureDetector(
          onTap: onTap,
          child: Container(
            padding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: borderColor),
            ),
            child: Row(
              textDirection: TextDirection.rtl,
              children: [
                Icon(Icons.calendar_today, color: primaryColor, size: 18),
                SizedBox(width: 8),
                Expanded(
                  child: Text(
                    date != null 
                      ? '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}'
                      : 'اختر التاريخ',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      color: date != null ? Colors.black : Colors.grey[400],
                      fontFamily: 'Tajawal',
                      fontSize: 13,
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
  
  Widget _buildApprovalCard() {
    if (_selectedLeaveType == null || _selectedLeaveType!['requiresApproval'] != true) {
      return SizedBox.shrink();
    }
    
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
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              'الاعتماد والبديل',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            
            // المسؤول عن الاعتماد
            Text(
              'المسؤول عن الاعتماد *',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            GestureDetector(
              onTap: _selectApprover,
              child: Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: _approverId != null ? Colors.blue[50] : Colors.grey[50],
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: _approverId != null ? primaryColor : borderColor,
                  ),
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    Icon(
                      _approverId != null ? Icons.person : Icons.person_add,
                      color: _approverId != null ? primaryColor : Colors.grey[400],
                    ),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _approverId != null
                          ? _managers.firstWhere(
                              (m) => m['id'] == _approverId,
                              orElse: () => {'fullName': 'غير محدد'},
                            )['fullName'] ?? 'غير محدد'
                          : 'اختر المسؤول عن الاعتماد',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          color: _approverId != null ? Colors.black : Colors.grey[400],
                        ),
                      ),
                    ),
                    if (_approverId != null)
                      Icon(Icons.edit, color: primaryColor, size: 18),
                  ],
                ),
              ),
            ),
            
            SizedBox(height: 16),
            
            // الموظف البديل
            Text(
              'الموظف البديل (اختياري)',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            GestureDetector(
              onTap: _selectReplacement,
              child: Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: _replacementUserId != null ? Colors.green[50] : Colors.grey[50],
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(
                    color: _replacementUserId != null ? Colors.green : borderColor,
                  ),
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    Icon(
                      _replacementUserId != null ? Icons.person : Icons.person_add,
                      color: _replacementUserId != null ? Colors.green : Colors.grey[400],
                    ),
                    SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        _replacementUserId != null
                          ? _allEmployees.firstWhere(
                              (e) => e['id'] == _replacementUserId,
                              orElse: () => {'fullName': 'غير محدد', 'code': ''},
                            )['fullName'] ?? 'غير محدد'
                          : 'لا يوجد',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          color: _replacementUserId != null ? Colors.black : Colors.grey[400],
                        ),
                      ),
                    ),
                    if (_replacementUserId != null)
                      Icon(Icons.close, color: Colors.red, size: 18),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildActionButtons() {
    return Container(
      padding: EdgeInsets.symmetric(vertical: 16),
      child: Row(
        children: [
          Expanded(
            child: ElevatedButton.icon(
              onPressed: _isSubmitting ? null : _submitRequest,
              style: ElevatedButton.styleFrom(
                backgroundColor: accentColor,
                foregroundColor: Colors.white,
                padding: EdgeInsets.symmetric(vertical: 16),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
                elevation: 3,
              ),
              icon: _isSubmitting
                  ? SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(
                        strokeWidth: 2,
                        color: Colors.white,
                      ),
                    )
                  : Icon(Icons.send, size: 20),
              label: Text(
                _isSubmitting ? 'جاري الإرسال...' : 'إرسال الطلب',
                style: TextStyle(
                  fontSize: 15,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
            ),
          ),
          SizedBox(width: 12),
          ElevatedButton.icon(
            onPressed: () => Navigator.pop(context),
            style: ElevatedButton.styleFrom(
              backgroundColor: Colors.grey[200],
              foregroundColor: Colors.grey[700],
              padding: EdgeInsets.symmetric(horizontal: 24, vertical: 16),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
              ),
              elevation: 0,
            ),
            icon: Icon(Icons.cancel, size: 20),
            label: Text(
              'إلغاء',
              style: TextStyle(
                fontSize: 15,
                fontWeight: FontWeight.bold,
                fontFamily: 'Tajawal',
              ),
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
            'طلب إجازة جديد',
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
                child: CircularProgressIndicator(color: primaryColor),
              )
            : Form(
                key: _formKey,
                child: SingleChildScrollView(
                  padding: EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      _buildEmployeeCard(),
                      SizedBox(height: 16),
                      _buildLeaveDataCard(),
                      SizedBox(height: 16),
                      _buildApprovalCard(),
                      SizedBox(height: 16),
                      _buildActionButtons(),
                    ],
                  ),
                ),
              ),
      ),
    );
  }
  
  @override
  void dispose() {
    _reasonController.dispose();
    _notesController.dispose();
    _searchController.dispose();
    super.dispose();
  }
}