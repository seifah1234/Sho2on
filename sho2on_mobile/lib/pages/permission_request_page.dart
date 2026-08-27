import 'package:flutter/material.dart';
import '../services/permission_service.dart';

class PermissionRequestPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const PermissionRequestPage({super.key, required this.user});

  @override
  _PermissionRequestPageState createState() => _PermissionRequestPageState();
}

class _PermissionRequestPageState extends State<PermissionRequestPage> {
  final PermissionService _permissionService = PermissionService();
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();

  Map<String, dynamic>? _selectedManager;
  int _selectedPermissionType = 2;
  DateTime _startDateTime = DateTime.now();
  DateTime _endDateTime = DateTime.now().add(Duration(hours: 2));
  String _reason = '';
  String _notes = '';
  bool _showDeductionField = false;

  List<dynamic> _managers = [];
  List<dynamic> _permissionTypes = [];

  final double _totalHours = 0;
  final double _deductedAmount = 0;

  bool _isLoading = false;
  bool _isSubmitting = false;

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
      final typesResult = await _permissionService.getPermissionTypes();
      if (typesResult['success']) {
        setState(() => _permissionTypes = typesResult['data'] ?? []);
      }
      await _loadManagers();
    } catch (e) {
      _showError('خطأ في تحميل البيانات');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadManagers() async {
    try {
      final result = await _permissionService.getManagersForApproval();
      if (result['success']) {
        setState(() {
          _managers = result['data'] ?? [];
          if (widget.user['managerId'] != null && _managers.isNotEmpty) {
            final managerId = widget.user['managerId'];
            _selectedManager = _managers.firstWhere(
              (manager) => manager['id'] == managerId,
              orElse: () => _managers.first,
            );
          } else if (_managers.isNotEmpty) {
            _selectedManager = _managers.first;
          }
        });
      }
    } catch (e) {}
  }

  Future<void> _selectDateTime({required bool isStart}) async {
    final DateTime? date = await showDatePicker(
      context: context,
      initialDate: isStart ? _startDateTime : _endDateTime,
      firstDate: isStart ? DateTime.now() : _startDateTime,
      lastDate: DateTime.now().add(Duration(days: 30)),
      builder: (context, child) => Theme(
        data: ThemeData.light().copyWith(
          colorScheme: ColorScheme.light(primary: primaryBlue, onPrimary: Colors.white),
        ),
        child: Directionality(textDirection: TextDirection.rtl, child: child!),
      ),
    );

    if (date != null && mounted) {
      final TimeOfDay? time = await showTimePicker(
        context: context,
        initialTime: TimeOfDay.fromDateTime(isStart ? _startDateTime : _endDateTime),
      );

      if (time != null) {
        setState(() {
          final newDateTime = DateTime(date.year, date.month, date.day, time.hour, time.minute);
          if (isStart) {
            _startDateTime = newDateTime;
            if (_endDateTime.isBefore(_startDateTime)) {
              _endDateTime = _startDateTime.add(Duration(hours: 2));
            }
          } else {
            _endDateTime = newDateTime;
          }
        });
      }
    }
  }

  Future<void> _selectManager() async {
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
              Text('اختر المدير للموافقة', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, fontFamily: 'Tajawal', color: primaryBlue)),
              SizedBox(height: 16),
              Expanded(
                child: ListView.builder(
                  itemCount: _managers.length,
                  itemBuilder: (context, index) {
                    final manager = _managers[index];
                    final isSelected = _selectedManager?['id'] == manager['id'];
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
    if (selected != null) setState(() => _selectedManager = selected);
  }

  Future<void> _submitRequest() async {
    if (!_validateForm()) return;
    setState(() => _isSubmitting = true);
    try {
      final result = await _permissionService.submitPermissionRequest(
        employeeId: widget.user['id'],
        permissionTypeId: _selectedPermissionType,
        startDateTime: _startDateTime,
        endDateTime: _endDateTime,
        reason: _reason,
        approvingManagerId: _selectedManager!['id'],
        notes: _notes,
      );
      if (result['success']) {
        _showSuccessDialog('تم تقديم الطلب بنجاح', () => Navigator.pop(context, true));
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
    if (_selectedPermissionType <= 0) { _showError('اختر نوع الإذن'); return false; }
    if (_startDateTime.isAfter(_endDateTime)) { _showError('تاريخ البداية يجب أن يكون قبل النهاية'); return false; }
    if (_reason.isEmpty) { _showError('سبب الإذن مطلوب'); return false; }
    if (_selectedManager == null) { _showError('اختر مدير للموافقة'); return false; }
    return true;
  }

  String _formatDateTime(DateTime dt) {
    final period = dt.hour >= 12 ? 'م' : 'ص';
    final hour12 = dt.hour % 12 == 0 ? 12 : dt.hour % 12;
    return '${dt.year}/${dt.month}/${dt.day} - $hour12:${dt.minute.toString().padLeft(2, '0')} $period';
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

  Widget _buildEmployeeCard() {
    return Container(
      padding: EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(begin: Alignment.topRight, end: Alignment.bottomLeft, colors: [darkBlue, primaryBlue]),
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: primaryBlue.withValues(alpha: 0.3), blurRadius: 10, offset: Offset(0, 4))],
      ),
      child: Row(children: [
        Container(
          width: 45,
          height: 45,
          decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.2), shape: BoxShape.circle),
          child: Center(child: Text((widget.user['fullName'] ?? '?')[0], style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'))),
        ),
        SizedBox(width: 12),
        Expanded(
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Text(widget.user['fullName'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
            Text(widget.user['jobTitle']?['name'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, color: Colors.white.withValues(alpha: 0.8))),
          ]),
        ),
      ]),
    );
  }

  Widget _buildPermissionTypeSelector() {
    final selectedType = _permissionTypes.firstWhere(
      (type) => type['id'] == _selectedPermissionType,
      orElse: () => {'name': 'إذن', 'deductFromSalary': false},
    );

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: _permissionTypes.map((type) {
        final isSelected = _selectedPermissionType == type['id'];
        return GestureDetector(
          onTap: () => setState(() {
            _selectedPermissionType = type['id'];
            _showDeductionField = type['deductFromSalary'] == true;
          }),
          child: Container(
            padding: EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            decoration: BoxDecoration(
              color: isSelected ? primaryBlue : cardColor,
              borderRadius: BorderRadius.circular(20),
              border: Border.all(color: isSelected ? primaryBlue : borderColor, width: isSelected ? 2 : 1),
            ),
            child: Text(
              type['name'] ?? '',
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontSize: 12,
                fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                color: isSelected ? Colors.white : Colors.grey[700],
              ),
            ),
          ),
        );
      }).toList(),
    );
  }

  Widget _buildDateTimeField(String label, DateTime dateTime, VoidCallback onTap) {
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
              Icon(Icons.access_time, color: primaryBlue, size: 18),
              SizedBox(width: 8),
              Expanded(
                child: Text(
                  _formatDateTime(dateTime),
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.black),
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
            Text('طلب إذن', style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
            Text('تقديم طلب إذن جديد', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8))),
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
                    _buildSectionCard('نوع الإذن', Icons.category, [
                      _buildPermissionTypeSelector(),
                    ]),
                    SizedBox(height: 16),
                    _buildSectionCard('الفترة الزمنية', Icons.access_time, [
                      _buildDateTimeField('من تاريخ', _startDateTime, () => _selectDateTime(isStart: true)),
                      SizedBox(height: 14),
                      _buildDateTimeField('إلى تاريخ', _endDateTime, () => _selectDateTime(isStart: false)),
                    ]),
                    SizedBox(height: 16),
                    _buildSectionCard('سبب الإذن', Icons.info_outline, [
                      TextField(
                        maxLines: 3,
                        decoration: InputDecoration(
                          hintText: 'اكتب سبب طلب الإذن...',
                          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
                          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: primaryBlue, width: 2)),
                        ),
                        onChanged: (value) => setState(() => _reason = value),
                      ),
                    ]),
                    SizedBox(height: 16),
                    _buildSectionCard('ملاحظات (اختياري)', Icons.note, [
                      TextField(
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
                    SizedBox(height: 16),
                    _buildSectionCard('المدير للموافقة', Icons.person, [
                      GestureDetector(
                        onTap: _selectManager,
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
                              child: Text(_selectedManager != null ? (_selectedManager!['fullName'] ?? '?')[0] : '?', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                            ),
                            SizedBox(width: 10),
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Text(_selectedManager?['fullName'] ?? 'اختر المدير', style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold, color: darkBlue)),
                                if (_selectedManager != null) Text(_selectedManager!['jobTitleName'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500])),
                              ]),
                            ),
                            Icon(Icons.chevron_left, color: primaryBlue),
                          ]),
                        ),
                      ),
                    ]),
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
}