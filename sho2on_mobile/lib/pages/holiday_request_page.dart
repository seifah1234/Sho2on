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

  int? _selectedLeaveTypeId;
  DateTime? _startDate;
  DateTime? _endDate;
  int _duration = 0;
  String _reason = '';
  String _notes = '';
  int? _approverId;
  int? _replacementUserId;

  List<dynamic> _leaveTypes = [];
  List<dynamic> _allEmployees = [];
  List<dynamic> _managers = [];
  final List<dynamic> _filteredEmployees = [];

  Map<String, dynamic>? _leaveBalance;
  Map<String, dynamic>? _selectedLeaveType;

  bool _isLoading = false;
  bool _isSubmitting = false;

  final TextEditingController _reasonController = TextEditingController();
  final TextEditingController _notesController = TextEditingController();
  final TextEditingController _searchController = TextEditingController();

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color successColor = Color(0xFF10B981);
  final Color errorColor = Color(0xFFEF4444);
  final Color borderColor = Color(0xFFE5E7EB);

  @override
  void initState() {
    super.initState();
    _loadInitialData();
  }

  Future<void> _loadInitialData() async {
    setState(() => _isLoading = true);
    try {
      final leaveTypesResult = await _holidayService.getLeaveTypes();
      if (leaveTypesResult['success']) {
        setState(() => _leaveTypes = leaveTypesResult['data'] ?? []);
      }
      await _loadManagers();
      await _loadAllEmployees();
    } catch (e) {
      _showError('خطأ في تحميل البيانات');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadManagers() async {
    try {
      final result = await _holidayService.getManagers();
      if (result['success']) setState(() => _managers = result['data'] ?? []);
    } catch (e) {}
  }

  Future<void> _loadAllEmployees() async {
    try {
      final result = await _holidayService.searchEmployees(searchTerm: '');
      if (result['success']) setState(() => _allEmployees = result['data'] ?? []);
    } catch (e) {}
  }

  Future<void> _loadLeaveBalance() async {
    if (_selectedLeaveTypeId == null || _selectedLeaveTypeId == 0) {
      setState(() => _leaveBalance = null);
      return;
    }
    try {
      final result = await _holidayService.getLeaveBalance(widget.user['id'], _selectedLeaveTypeId!);
      if (result['success']) setState(() => _leaveBalance = result['data']);
    } catch (e) {}
  }

  void _updateDuration() {
    if (_startDate == null || _endDate == null || _endDate!.isBefore(_startDate!)) {
      setState(() => _duration = 0);
      return;
    }
    setState(() => _duration = (_endDate!.difference(_startDate!).inDays) + 1);
    _loadLeaveBalance();
  }

  Future<void> _selectDate({required bool isStart}) async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: isStart ? (_startDate ?? DateTime.now()) : (_endDate ?? _startDate ?? DateTime.now()),
      firstDate: isStart ? DateTime.now() : (_startDate ?? DateTime.now()),
      lastDate: DateTime.now().add(Duration(days: 365)),
      builder: (context, child) {
        return Theme(
          data: ThemeData.light().copyWith(
            colorScheme: ColorScheme.light(primary: primaryBlue, onPrimary: Colors.white, surface: Colors.white, onSurface: Colors.black),
          ),
          child: Directionality(textDirection: TextDirection.rtl, child: child!),
        );
      },
    );
    if (picked != null) {
      setState(() {
        if (isStart) {
          _startDate = picked;
          if (_endDate != null && _endDate!.isBefore(picked)) _endDate = null;
        } else {
          _endDate = picked;
        }
      });
      _updateDuration();
    }
  }

  Future<void> _selectApprover() async {
    if (_managers.isEmpty) return;
    final selected = await showModalBottomSheet<Map<String, dynamic>>(
      context: context,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(20))),
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: Container(
          padding: EdgeInsets.all(20),
          height: MediaQuery.of(context).size.height * 0.6,
          child: Column(
            children: [
              Container(width: 40, height: 4, decoration: BoxDecoration(color: Colors.grey[300], borderRadius: BorderRadius.circular(2))),
              SizedBox(height: 16),
              Text('اختر المسؤول عن الاعتماد', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, fontFamily: 'Tajawal', color: primaryBlue)),
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
                        side: BorderSide(color: isSelected ? primaryBlue : borderColor, width: isSelected ? 2 : 1),
                      ),
                      child: ListTile(
                        leading: CircleAvatar(
                          backgroundColor: isSelected ? primaryBlue : Colors.grey[300],
                          child: Text((manager['fullName'] ?? '?')[0], style: TextStyle(color: Colors.white)),
                        ),
                        title: Text(manager['fullName'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
                        subtitle: Text(manager['jobTitleName'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12)),
                        trailing: isSelected ? Icon(Icons.check_circle, color: primaryBlue) : null,
                        onTap: () => Navigator.pop(context, manager),
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
    if (selected != null) setState(() => _approverId = selected['id']);
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
        final message = requiresApproval ? 'تم إرسال طلب الإجازة للاعتماد' : 'تم تسجيل الإجازة واعتمادها تلقائياً';
        _showSuccessDialog(message, () {
          Navigator.pushReplacement(context, MaterialPageRoute(builder: (context) => LeaveHistoryPage(user: widget.user)));
        });
      } else {
        _showError(result['message'] ?? 'فشل في تقديم الطلب');
      }
    } catch (e) {
      _showError('خطأ في تقديم الطلب');
    } finally {
      setState(() => _isSubmitting = false);
    }
  }

  bool _validateForm() {
    if (_selectedLeaveTypeId == null || _selectedLeaveTypeId == 0) { _showError('اختر نوع الإجازة'); return false; }
    if (_startDate == null || _endDate == null) { _showError('حدد تاريخ بداية ونهاية الإجازة'); return false; }
    if (_endDate!.isBefore(_startDate!)) { _showError('تاريخ النهاية يجب أن يكون بعد تاريخ البداية'); return false; }
    if (_duration <= 0) { _showError('مدة الإجازة غير صحيحة'); return false; }
    if (_reason.isEmpty) { _showError('سبب الإجازة مطلوب'); return false; }
    if (_selectedLeaveType != null && _selectedLeaveType!['maxConsecutiveDays'] != null && _duration > _selectedLeaveType!['maxConsecutiveDays']) {
      _showError('الحد الأقصى لهذا النوع هو ${_selectedLeaveType!['maxConsecutiveDays']} يوم');
      return false;
    }
    if (_selectedLeaveType != null && _selectedLeaveType!['deductFromBalance'] == true && _leaveBalance != null && _duration > (_leaveBalance!['remainingBalance'] ?? 0)) {
      _showError('الرصيد المتبقي غير كافٍ');
      return false;
    }
    if (_selectedLeaveType != null && _selectedLeaveType!['requiresApproval'] == true && _approverId == null) {
      _showError('اختر المسؤول عن اعتماد الإجازة');
      return false;
    }
    return true;
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal')),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
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
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('نجاح', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: successColor)),
              SizedBox(width: 10),
              Container(padding: EdgeInsets.all(8), decoration: BoxDecoration(color: successColor.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: Icon(Icons.check_circle, color: successColor, size: 20)),
            ],
          ),
          content: Text(message, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal')),
          actions: [
            TextButton(
              onPressed: () { Navigator.pop(context); onOk(); },
              style: TextButton.styleFrom(foregroundColor: primaryBlue),
              child: Text('موافق', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSectionCard(String title, IconData icon, List<Widget> children) {
    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
        border: Border.all(color: borderColor),
      ),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Container(padding: EdgeInsets.all(8), decoration: BoxDecoration(color: lightBlue, borderRadius: BorderRadius.circular(10)),
                child: Icon(icon, color: primaryBlue, size: 18)),
              SizedBox(width: 10),
              Text(title, style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue)),
            ]),
            SizedBox(height: 16),
            ...children,
          ],
        ),
      ),
    );
  }

  Widget _buildLeaveTypeDropdown() {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 12),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: borderColor),
      ),
      child: DropdownButtonFormField<int>(
        initialValue: _selectedLeaveTypeId,
        items: [
          DropdownMenuItem<int>(value: 0, child: Text('اختر نوع الإجازة', style: TextStyle(fontFamily: 'Tajawal'))),
          ..._leaveTypes.map((type) => DropdownMenuItem<int>(
            value: type['id'],
            child: Text('${type['name']} (${type['code'] ?? ''})', style: TextStyle(fontFamily: 'Tajawal')),
          )),
        ],
        onChanged: (value) {
          setState(() {
            _selectedLeaveTypeId = value;
            if (value != null && value > 0) {
              _selectedLeaveType = _leaveTypes.firstWhere((type) => type['id'] == value, orElse: () => {});
            } else {
              _selectedLeaveType = null;
            }
          });
          _loadLeaveBalance();
        },
        decoration: InputDecoration(border: InputBorder.none),
        dropdownColor: Colors.white,
        icon: Icon(Icons.arrow_drop_down, color: primaryBlue),
        isExpanded: true,
      ),
    );
  }

  Widget _buildDateField(String label, DateTime? date, VoidCallback onTap) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
        SizedBox(height: 8),
        GestureDetector(
          onTap: onTap,
          child: Container(
            padding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
            decoration: BoxDecoration(
              color: cardColor,
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: borderColor),
            ),
            child: Row(children: [
              Icon(Icons.calendar_today, color: primaryBlue, size: 18),
              SizedBox(width: 8),
              Expanded(
                child: Text(
                  date != null ? '${date.year}/${date.month.toString().padLeft(2, '0')}/${date.day.toString().padLeft(2, '0')}' : 'اختر التاريخ',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: date != null ? Colors.black : Colors.grey[400]),
                ),
              ),
            ]),
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
          title: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text('طلب إجازة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
            Text('تقديم طلب إجازة جديد', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8))),
          ]),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(bottom: Radius.circular(20))),
        ),
        body: _isLoading
            ? Center(child: CircularProgressIndicator(color: primaryBlue))
            : SingleChildScrollView(
                padding: EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    _buildSectionCard('بيانات الإجازة', Icons.beach_access, [
                      Text('نوع الإجازة *', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
                      SizedBox(height: 8),
                      _buildLeaveTypeDropdown(),
                      if (_selectedLeaveType != null) ...[
                        SizedBox(height: 12),
                        Container(
                          padding: EdgeInsets.all(12),
                          decoration: BoxDecoration(color: Color(0xFFF8FAFC), borderRadius: BorderRadius.circular(10), border: Border.all(color: borderColor)),
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Text('الحد الأقصى: ${_selectedLeaveType!['maxConsecutiveDays']?.toString() ?? 'لا يوجد'}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500])),
                            SizedBox(height: 4),
                            Text('الخصم من الرصيد: ${_selectedLeaveType!['deductFromBalance'] == true ? 'نعم' : 'لا'}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500])),
                            SizedBox(height: 4),
                            Text('يتطلب اعتماد: ${_selectedLeaveType!['requiresApproval'] == true ? 'نعم' : 'لا'}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500])),
                          ]),
                        ),
                      ],
                      if (_leaveBalance != null && _selectedLeaveType?['deductFromBalance'] == true) ...[
                        SizedBox(height: 12),
                        Container(
                          padding: EdgeInsets.all(12),
                          decoration: BoxDecoration(color: successColor.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(10), border: Border.all(color: successColor.withValues(alpha: 0.3))),
                          child: Row(mainAxisAlignment: MainAxisAlignment.spaceAround, children: [
                            _buildBalanceItem('الإجمالي', _leaveBalance!['totalBalance']?.toString() ?? '0'),
                            _buildBalanceItem('المستخدم', _leaveBalance!['usedBalance']?.toString() ?? '0'),
                            _buildBalanceItem('المتبقي', _leaveBalance!['remainingBalance']?.toString() ?? '0'),
                          ]),
                        ),
                      ],
                      SizedBox(height: 16),
                      Row(children: [
                        Expanded(child: _buildDateField('من تاريخ *', _startDate, () => _selectDate(isStart: true))),
                        SizedBox(width: 10),
                        Expanded(child: _buildDateField('إلى تاريخ *', _endDate, () => _selectDate(isStart: false))),
                      ]),
                      SizedBox(height: 14),
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                        decoration: BoxDecoration(color: lightBlue, borderRadius: BorderRadius.circular(10)),
                        child: Row(children: [
                          Icon(Icons.timer, color: primaryBlue, size: 18),
                          SizedBox(width: 8),
                          Text('المدة: $_duration يوم', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: darkBlue)),
                        ]),
                      ),
                      SizedBox(height: 16),
                      Text('سبب الإجازة *', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
                      SizedBox(height: 8),
                      TextField(
                        controller: _reasonController,
                        maxLines: 3,
                        decoration: InputDecoration(
                          hintText: 'أدخل سبب الإجازة...',
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: primaryBlue, width: 2)),
                        ),
                        onChanged: (value) => setState(() => _reason = value),
                      ),
                      SizedBox(height: 14),
                      Text('ملاحظات (اختياري)', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
                      SizedBox(height: 8),
                      TextField(
                        controller: _notesController,
                        maxLines: 2,
                        decoration: InputDecoration(
                          hintText: 'ملاحظات إضافية...',
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: primaryBlue, width: 2)),
                        ),
                        onChanged: (value) => setState(() => _notes = value),
                      ),
                    ]),
                    if (_selectedLeaveType != null && _selectedLeaveType!['requiresApproval'] == true) ...[
                      SizedBox(height: 16),
                      _buildSectionCard('الاعتماد', Icons.person, [
                        GestureDetector(
                          onTap: _selectApprover,
                          child: Container(
                            padding: EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: lightBlue.withValues(alpha: 0.5),
                              borderRadius: BorderRadius.circular(10),
                              border: Border.all(color: primaryBlue.withValues(alpha: 0.3)),
                            ),
                            child: Row(children: [
                              CircleAvatar(
                                radius: 20,
                                backgroundColor: primaryBlue,
                                child: Text(_approverId != null ? (_managers.firstWhere((m) => m['id'] == _approverId, orElse: () => {'fullName': '?'})['fullName'] ?? '?')[0] : '?', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                              ),
                              SizedBox(width: 10),
                              Expanded(
                                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                  Text(_approverId != null ? _managers.firstWhere((m) => m['id'] == _approverId, orElse: () => {'fullName': 'اختر المسؤول'})['fullName'] ?? 'اختر المسؤول' : 'اختر المسؤول عن الاعتماد',
                                    style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold, color: darkBlue)),
                                ]),
                              ),
                              Icon(Icons.chevron_left, color: primaryBlue),
                            ]),
                          ),
                        ),
                      ]),
                    ],
                    SizedBox(height: 20),
                    Row(children: [
                      Expanded(
                        child: Container(
                          height: 52,
                          decoration: BoxDecoration(
                            gradient: LinearGradient(colors: [primaryBlue, darkBlue]),
                            borderRadius: BorderRadius.circular(14),
                            boxShadow: [BoxShadow(color: primaryBlue.withValues(alpha: 0.4), blurRadius: 10, offset: Offset(0, 4))],
                          ),
                          child: ElevatedButton(
                            onPressed: _isSubmitting ? null : _submitRequest,
                            style: ElevatedButton.styleFrom(backgroundColor: Colors.transparent, shadowColor: Colors.transparent, shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14))),
                            child: _isSubmitting
                                ? SizedBox(width: 22, height: 22, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                                : Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                                    Icon(Icons.send, size: 18),
                                    SizedBox(width: 8),
                                    Text('إرسال الطلب', style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold)),
                                  ]),
                          ),
                        ),
                      ),
                      SizedBox(width: 10),
                      OutlinedButton(
                        onPressed: () => Navigator.pop(context),
                        style: OutlinedButton.styleFrom(
                          foregroundColor: Colors.grey[600],
                          side: BorderSide(color: Colors.grey[300]!),
                          padding: EdgeInsets.symmetric(horizontal: 20, vertical: 14),
                          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
                        ),
                        child: Text('إلغاء', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
                      ),
                    ]),
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildBalanceItem(String label, String value) {
    return Column(children: [
      Text(label, style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500])),
      SizedBox(height: 2),
      Text(value, style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold, color: successColor)),
    ]);
  }

  @override
  void dispose() {
    _reasonController.dispose();
    _notesController.dispose();
    _searchController.dispose();
    super.dispose();
  }
}