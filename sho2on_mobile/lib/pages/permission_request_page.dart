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
  
  // بيانات النموذج
  Map<String, dynamic>? _selectedManager;
  int _selectedPermissionType = 2; // 2: إذن
  DateTime _startDateTime = DateTime.now();
  DateTime _endDateTime = DateTime.now().add(Duration(hours: 2));
  String _reason = '';
  String _notes = '';
  bool _showDeductionField = false;
  
  // قوائم البيانات
  List<dynamic> _managers = [];
  List<dynamic> _permissionTypes = [];
  
  // معلومات الحسابات
  double _totalHours = 0;
  double _deductedAmount = 0;
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isSubmitting = false;
  bool _isCalculating = false;
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color secondaryColor = Color(0xFF42A5F5);
  final Color accentColor = Color(0xFF4CAF50);
  final Color warningColor = Color(0xFFFF9800);
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
      // تحميل أنواع الإذن
      final typesResult = await _permissionService.getPermissionTypes();
      if (typesResult['success']) {
        setState(() {
          _permissionTypes = typesResult['data'];
        });
      }
      
      // تحميل المديرين
      await _loadManagers();
      
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _loadManagers() async {
  try {
    final result = await _permissionService.getManagersForApproval();
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
  } catch (e) {
    _showError('خطأ في تحميل المديرين: $e');
  }
}
  
  Future<void> _checkTimeConflict() async {
    if (_startDateTime.isAfter(_endDateTime)) {
      _showError('تاريخ البداية يجب أن يكون قبل تاريخ النهاية');
      return;
    }
    
    setState(() => _isCalculating = true);
    
    try {
      final result = await _permissionService.checkTimeConflict(
        employeeId: widget.user['id'],
        startDateTime: _startDateTime,
        endDateTime: _endDateTime,
      );
      
      if (result['success']) {
        if (result['data']['hasConflicts'] == true) {
          final conflicts = result['data']['conflicts'];
          String conflictMessage = 'هناك تعارض مع:\n';
          for (var conflict in conflicts) {
            conflictMessage += '• ${conflict['type']} من ${_formatDateTime(conflict['startDateTime'])} إلى ${_formatDateTime(conflict['endDateTime'])}\n';
          }
          
          _showWarningDialog('تعارض في المواعيد', conflictMessage);
        } else {
          _showSuccessSnackBar('لا يوجد تعارض في المواعيد');
        }
      }
    } catch (e) {
      _showError('خطأ في التحقق من التعارض: $e');
    } finally {
      setState(() => _isCalculating = false);
    }
  }
  
  Future<void> _calculateDeduction() async {
    if (_selectedPermissionType != 2) return;
    
    setState(() => _isCalculating = true);
    
    try {
      final result = await _permissionService.calculateDeduction(
        employeeId: widget.user['id'],
        startDateTime: _startDateTime,
        endDateTime: _endDateTime,
        deductFromSalary: true,
      );
      
      if (result['success']) {
        setState(() {
          _totalHours = result['data']['totalHours'];
          _deductedAmount = result['data']['deductedAmount'].toDouble();
          _showDeductionField = true;
        });
      }
    } catch (e) {
      _showError('خطأ في حساب الخصم: $e');
    } finally {
      setState(() => _isCalculating = false);
    }
  }
  
  Future<void> _selectPermissionType() async {
    final Map<String, dynamic>? selected = await showModalBottomSheet(
      context: context,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: Container(
          padding: EdgeInsets.all(16),
          height: MediaQuery.of(context).size.height * 0.5,
          child: Column(
            children: [
              Text(
                'اختر نوع الإذن',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
              SizedBox(height: 16),
              Expanded(
                child: ListView.builder(
                  itemCount: _permissionTypes.length,
                  itemBuilder: (context, index) {
                    final type = _permissionTypes[index];
                    return ListTile(
                      leading: _getPermissionTypeIcon(type['id']),
                      title: Text(
                        type['name'],
                        textDirection: TextDirection.rtl,
                      ),
                      subtitle: Text(
                        type['deductFromSalary'] ? 'يقتطع من الراتب' : 'لا يقتطع من الراتب',
                        textDirection: TextDirection.rtl,
                      ),
                      trailing: _selectedPermissionType == type['id']
                          ? Icon(Icons.check, color: primaryColor)
                          : null,
                      onTap: () {
                        Navigator.pop(context, type);
                      },
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
      setState(() {
        _selectedPermissionType = selected['id'];
        _showDeductionField = selected['deductFromSalary'] == true;
      });
      
      if (_showDeductionField) {
        await _calculateDeduction();
      }
    }
  }
  
  Widget _getPermissionTypeIcon(int typeId) {
    IconData icon;
    Color color;
    
    switch (typeId) {
      case 1: // مأمورية
        icon = Icons.business_center;
        color = Colors.blue;
        break;
      case 2: // إذن
        icon = Icons.access_time;
        color = Colors.orange;
        break;
      case 3: // إذن طبي
        icon = Icons.medical_services;
        color = Colors.red;
        break;
      case 4: // إذن عائلي
        icon = Icons.family_restroom;
        color = Colors.green;
        break;
      case 5: // إذن طارئ
        icon = Icons.warning;
        color = Colors.purple;
        break;
      default:
        icon = Icons.access_time;
        color = Colors.grey;
    }
    
    return Icon(icon, color: color);
  }
  
  Future<void> _selectStartDateTime() async {
    DateTime? date = await showDatePicker(
      context: context,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(Duration(days: 30)),
    );
    
    if (date != null) {
      TimeOfDay? time = await showTimePicker(
        context: context,
        initialTime: TimeOfDay.fromDateTime(_startDateTime),
      );
      
      if (time != null) {
        setState(() {
          _startDateTime = DateTime(
            date.year,
            date.month,
            date.day,
            time.hour,
            time.minute,
          );
        });
      }
    }
  }
  
  Future<void> _selectEndDateTime() async {
    DateTime? date = await showDatePicker(
      context: context,
      firstDate: _startDateTime,
      lastDate: DateTime.now().add(Duration(days: 30)),
    );
    
    if (date != null) {
      TimeOfDay? time = await showTimePicker(
        context: context,
        initialTime: TimeOfDay.fromDateTime(_endDateTime),
      );
      
      if (time != null) {
        setState(() {
          _endDateTime = DateTime(
            date.year,
            date.month,
            date.day,
            time.hour,
            time.minute,
          );
        });
      }
    }
  }
  
  Future<void> _selectManager() async {
    if (_managers.isEmpty) {
      _showError('لا يوجد مديرين متاحين');
      return;
    }
    
    final Map<String, dynamic>? selected = await showModalBottomSheet(
      context: context,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
      ),
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: Container(
          padding: EdgeInsets.all(16),
          height: MediaQuery.of(context).size.height * 0.6,
          child: Column(
            children: [
              Text(
                'اختر المدير للموافقة',
                style: TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  fontFamily: 'Tajawal',
                ),
              ),
              SizedBox(height: 16),
              Expanded(
                child: ListView.builder(
                  itemCount: _managers.length,
                  itemBuilder: (context, index) {
                    final manager = _managers[index];
                    return ListTile(
                      leading: CircleAvatar(
                        backgroundColor: primaryColor,
                        child: Text(
                          manager['fullName'][0],
                          style: TextStyle(color: Colors.white),
                        ),
                      ),
                      title: Text(
                        manager['fullName'],
                        textDirection: TextDirection.rtl,
                      ),
                      subtitle: Text(
                        '${manager['jobTitleName']} - ${manager['departmentName']}',
                        textDirection: TextDirection.rtl,
                      ),
                      trailing: _selectedManager != null && 
                               _selectedManager!['id'] == manager['id']
                          ? Icon(Icons.check, color: primaryColor)
                          : null,
                      onTap: () {
                        Navigator.pop(context, manager);
                      },
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
      setState(() => _selectedManager = selected);
    }
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
    if (_selectedPermissionType <= 0) {
      _showError('الرجاء اختيار نوع الإذن');
      return false;
    }
    
    if (_startDateTime.isAfter(_endDateTime)) {
      _showError('تاريخ البداية يجب أن يكون قبل تاريخ النهاية');
      return false;
    }
    
    if (_reason.isEmpty) {
      _showError('الرجاء كتابة سبب الإذن');
      return false;
    }
    
    if (_selectedManager == null) {
      _showError('الرجاء اختيار مدير للموافقة');
      return false;
    }
    
    return true;
  }
  
  String _formatDateTime(DateTime dateTime) {
    return '${dateTime.year}/${dateTime.month}/${dateTime.day} ${dateTime.hour}:${dateTime.minute.toString().padLeft(2, '0')}';
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
  
  void _showSuccessSnackBar(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        backgroundColor: Colors.green,
        duration: Duration(seconds: 2),
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
  
  Widget _buildEmployeeInfoCard() {
    final selectedType = _permissionTypes.firstWhere(
      (type) => type['id'] == _selectedPermissionType,
      orElse: () => {'name': 'إذن', 'deductFromSalary': false},
    );
    
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
              'معلومات الموظف',
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
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.blue[100]!),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    textDirection: TextDirection.rtl,
                    children: [
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            widget.user['fullName'] ?? '',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Colors.blue[900],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          Text(
                            '${widget.user['department']?['name'] ?? ''} - ${widget.user['jobTitle']?['name'] ?? ''}',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              color: Colors.blue[700],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                      CircleAvatar(
                        radius: 25,
                        backgroundColor: primaryColor,
                        child: Icon(
                          Icons.person,
                          size: 30,
                          color: Colors.white,
                        ),
                      ),
                    ],
                  ),
                  
                  SizedBox(height: 16),
                  
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    textDirection: TextDirection.rtl,
                    children: [
                      Column(
                        children: [
                          Icon(Icons.business, color: Colors.green),
                          Text(
                            'الفرع',
                            style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                          ),
                          Text(
                            widget.user['branch']?['name'] ?? 'غير محدد',
                            style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      
                      Column(
                        children: [
                          _getPermissionTypeIcon(_selectedPermissionType),
                          Text(
                            'نوع الإذن',
                            style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                          ),
                          Text(
                            selectedType['name'],
                            style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      
                      Column(
                        children: [
                          Icon(
                            selectedType['deductFromSalary'] 
                                ? Icons.money_off 
                                : Icons.money,
                            color: selectedType['deductFromSalary'] 
                                ? Colors.red 
                                : Colors.green,
                          ),
                          Text(
                            'الخصم',
                            style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                          ),
                          Text(
                            selectedType['deductFromSalary'] 
                                ? 'يقتطع من الراتب' 
                                : 'لا يقتطع',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              color: selectedType['deductFromSalary'] 
                                  ? Colors.red 
                                  : Colors.green,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildPermissionTypeSection() {
    final selectedType = _permissionTypes.firstWhere(
      (type) => type['id'] == _selectedPermissionType,
      orElse: () => {'name': 'إذن', 'deductFromSalary': false},
    );
    
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
                  'نوع الإذن',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.edit, color: primaryColor),
                  onPressed: _selectPermissionType,
                ),
              ],
            ),
            SizedBox(height: 16),
            
            if (_permissionTypes.isEmpty)
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey[300]!),
                ),
                child: Center(
                  child: Text(
                    'جارٍ تحميل أنواع الإذن...',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(color: Colors.grey[600]),
                  ),
                ),
              )
            else
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.blue[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue),
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    _getPermissionTypeIcon(_selectedPermissionType),
                    SizedBox(width: 16),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            selectedType['name'],
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Colors.blue[900],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            selectedType['deductFromSalary'] 
                                ? 'يقتطع من الراتب' 
                                : 'لا يقتطع من الراتب',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              color: selectedType['deductFromSalary'] 
                                  ? Colors.red[700] 
                                  : Colors.green[700],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildTimeSection() {
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
              'الفترة الزمنية',
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
              children: [
                _buildDateTimeField(
                  'من تاريخ',
                  _formatDateTime(_startDateTime),
                  _selectStartDateTime,
                  Icons.access_time,
                ),
                SizedBox(height: 16),
                _buildDateTimeField(
                  'إلى تاريخ',
                  _formatDateTime(_endDateTime),
                  _selectEndDateTime,
                  Icons.access_time,
                ),
              ],
            ),
            
            SizedBox(height: 20),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                ElevatedButton.icon(
                  onPressed: _isCalculating ? null : _checkTimeConflict,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: warningColor,
                    foregroundColor: Colors.white,
                    padding: EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(8),
                    ),
                  ),
                  icon: _isCalculating
                      ? SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(
                            strokeWidth: 2,
                            color: Colors.white,
                          ),
                        )
                      : Icon(Icons.warning, size: 20),
                  label: Text(
                    _isCalculating ? 'جارٍ التحقق...' : 'التحقق من التعارض',
                    style: TextStyle(fontFamily: 'Tajawal'),
                  ),
                ),
              ],
            ),
            
            if (_totalHours > 0) ...[
              SizedBox(height: 20),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.green[100]!),
                ),
                child: Column(
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      textDirection: TextDirection.rtl,
                      children: [
                        Text(
                          'المدة الإجمالية:',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: Colors.green[800],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        Text(
                          '${_totalHours.toStringAsFixed(2)} ساعة',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: Colors.green[800],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                      ],
                    ),
                    
                    if (_showDeductionField && _deductedAmount > 0) ...[
                      SizedBox(height: 10),
                      Divider(),
                      SizedBox(height: 10),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        textDirection: TextDirection.rtl,
                        children: [
                          Text(
                            'المبلغ المقتطع:',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.bold,
                              color: Colors.red[800],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          Text(
                            '${_deductedAmount.toStringAsFixed(2)} جنيه',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                              color: Colors.red[800],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  Widget _buildDateTimeField(String label, String value, VoidCallback onTap, IconData icon) {
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
          ),
        ),
        SizedBox(height: 8),
        GestureDetector(
          onTap: onTap,
          child: Container(
            width: double.infinity,
            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 15),
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
                SizedBox(width: 10),
                Expanded(
                  child: Text(
                    value,
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      color: Colors.black,
                      fontFamily: 'Tajawal',
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
  
  Widget _buildReasonSection() {
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
              'سبب الإذن',
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
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.grey[300]!),
              ),
              child: TextFormField(
                maxLines: 4,
                textDirection: TextDirection.rtl,
                decoration: InputDecoration(
                  hintText: 'اكتب سبب طلب الإذن هنا...',
                  hintStyle: TextStyle(
                    color: Colors.grey[400],
                    fontFamily: 'Tajawal',
                  ),
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.all(12),
                ),
                onChanged: (value) {
                  setState(() => _reason = value);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildNotesSection() {
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
              'ملاحظات (اختياري)',
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
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.grey[300]!),
              ),
              child: TextFormField(
                maxLines: 3,
                textDirection: TextDirection.rtl,
                decoration: InputDecoration(
                  hintText: 'اكتب أي ملاحظات إضافية...',
                  hintStyle: TextStyle(
                    color: Colors.grey[400],
                    fontFamily: 'Tajawal',
                  ),
                  border: InputBorder.none,
                  contentPadding: EdgeInsets.all(12),
                ),
                onChanged: (value) {
                  setState(() => _notes = value);
                },
              ),
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildManagerSection() {
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
                  'اختيار المدير للموافقة',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontFamily: 'Tajawal',
                  ),
                ),
                if (_managers.isNotEmpty)
                  IconButton(
                    icon: Icon(Icons.refresh, color: primaryColor),
                    onPressed: _loadManagers,
                  ),
              ],
            ),
            SizedBox(height: 16),
            
            if (_managers.isEmpty)
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.grey[300]!),
                ),
                child: Center(
                  child: Text(
                    'لا يوجد مديرين متاحين',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(
                      color: Colors.grey[600],
                      fontFamily: 'Tajawal',
                    ),
                  ),
                ),
              )
            else if (_selectedManager == null)
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
                  'اختر المدير للموافقة',
                  style: TextStyle(fontFamily: 'Tajawal'),
                ),
              )
            else
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.blue[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.blue),
                ),
                child: Row(
                  textDirection: TextDirection.rtl,
                  children: [
                    CircleAvatar(
                      radius: 25,
                      backgroundColor: primaryColor,
                      child: Text(
                        _selectedManager!['fullName'][0],
                        style: TextStyle(
                          color: Colors.white,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ),
                    SizedBox(width: 16),
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
                              color: Colors.blue[900],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          SizedBox(height: 4),
                          Text(
                            '${_selectedManager!['jobTitleName']}',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              color: Colors.blue[700],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                          Text(
                            _selectedManager!['departmentName'],
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              color: Colors.blue[700],
                              fontFamily: 'Tajawal',
                            ),
                          ),
                        ],
                      ),
                    ),
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
  
  Widget _buildActionButtons() {
    return Container(
      padding: EdgeInsets.symmetric(vertical: 16),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceEvenly,
        textDirection: TextDirection.rtl,
        children: [
          Expanded(
            child: Container(
              margin: EdgeInsets.symmetric(horizontal: 8),
              child: ElevatedButton.icon(
                onPressed: _isSubmitting ? null : _submitRequest,
                style: ElevatedButton.styleFrom(
                  backgroundColor: accentColor,
                  foregroundColor: Colors.white,
                  padding: EdgeInsets.symmetric(vertical: 16),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
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
                label: _isSubmitting
                    ? Text('جاري الإرسال...')
                    : Text(
                        'إرسال الطلب للمدير',
                        style: TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.bold,
                          fontFamily: 'Tajawal',
                        ),
                      ),
              ),
            ),
          ),
          Container(
            margin: EdgeInsets.symmetric(horizontal: 8),
            child: ElevatedButton.icon(
              onPressed: () => Navigator.pop(context),
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.grey[300],
                foregroundColor: Colors.grey[700],
                padding: EdgeInsets.symmetric(horizontal: 24, vertical: 16),
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
            'طلب إذن',
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
                  padding: EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      // معلومات الموظف
                      _buildEmployeeInfoCard(),
                      
                      SizedBox(height: 16),
                      
                      // نوع الإذن
                      _buildPermissionTypeSection(),
                      
                      SizedBox(height: 16),
                      
                      // الفترة الزمنية
                      _buildTimeSection(),
                      
                      SizedBox(height: 16),
                      
                      // سبب الإذن
                      _buildReasonSection(),
                      
                      SizedBox(height: 16),
                      
                      // ملاحظات
                      _buildNotesSection(),
                      
                      SizedBox(height: 16),
                      
                      // المدير للموافقة
                      _buildManagerSection(),
                      
                      SizedBox(height: 16),
                      
                      // أزرار الإجراء
                      _buildActionButtons(),
                      
                      SizedBox(height: 16),
                      
                      // ملاحظات هامة
                      Container(
                        padding: EdgeInsets.all(16),
                        margin: EdgeInsets.only(bottom: 20),
                        decoration: BoxDecoration(
                          color: Colors.orange[50],
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.orange[100]!),
                        ),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Row(
                              textDirection: TextDirection.rtl,
                              children: [
                                Icon(Icons.warning, color: Colors.orange[700]),
                                SizedBox(width: 8),
                                Text(
                                  'ملاحظات هامة',
                                  textDirection: TextDirection.rtl,
                                  style: TextStyle(
                                    fontWeight: FontWeight.bold,
                                    color: Colors.orange[700],
                                    fontFamily: 'Tajawal',
                                  ),
                                ),
                              ],
                            ),
                            SizedBox(height: 8),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                _buildNoteItem('• يرجى التأكد من صحة البيانات قبل التقديم'),
                                _buildNoteItem('• سيتم إشعار المدير المختص للموافقة على الطلب'),
                                _buildNoteItem('• يمكنك تتبع حالة الطلب من خلال سجل الإذن'),
                                _buildNoteItem('• الإذن العادي (إذن) يقتطع من الراتب بناءً على عدد الساعات'),
                                _buildNoteItem('• المأمورية والإذن الطبي والعائلي والطارئ لا تقتطع من الراتب'),
                              ],
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
  
  Widget _buildNoteItem(String text) {
    return Padding(
      padding: EdgeInsets.symmetric(vertical: 2),
      child: Text(
        text,
        textDirection: TextDirection.rtl,
        style: TextStyle(
          color: Colors.orange[700],
          fontFamily: 'Tajawal',
        ),
      ),
    );
  }
}