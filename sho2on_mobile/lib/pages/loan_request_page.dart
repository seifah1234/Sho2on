import 'package:flutter/material.dart';
import '../services/loan_service.dart';

class LoanRequestPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const LoanRequestPage({super.key, required this.user});

  @override
  _LoanRequestPageState createState() => _LoanRequestPageState();
}

class _LoanRequestPageState extends State<LoanRequestPage> {
  final LoanService _loanService = LoanService();
  final GlobalKey<FormState> _formKey = GlobalKey<FormState>();
  
  // بيانات النموذج
  Map<String, dynamic>? _selectedManager;
  double _loanAmount = 0;
  int _installmentMonths = 1;
  String _reason = '';
  DateTime _loanDate = DateTime.now();
  DateTime _expectedPaybackDate = DateTime.now().add(Duration(days: 30));
  
  // قوائم البيانات
  List<dynamic> _managers = [];
  final List<int> _installmentOptions = [1, 2, 3, 4, 5, 6];
  
  // معلومات الحسابات
  double _maxAllowedAmount = 0;
  double _monthlyInstallment = 0;
  double _friendshipBoxBalance = 0;
  double _currentLoanBalance = 0;
  String _employeeStatus = '';
  
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
      // تحميل بيانات الموظف الحالي
      await _loadEmployeeDetails(widget.user['id']);
      
      // تحميل المديرين
      await _loadManagers();
      
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _loadEmployeeDetails(int employeeId) async {
    try {
      final result = await _loanService.getEmployee(employeeId);
      if (result['success']) {
        setState(() {
          _maxAllowedAmount = (result['data']['maxAllowedAmount'] ?? 0).toDouble();
          _friendshipBoxBalance = (result['data']['friendshipBoxBalance'] ?? 0).toDouble();
          _currentLoanBalance = (result['data']['currentLoanBalance'] ?? 0).toDouble();
          _employeeStatus = result['data']['employeeStatus'] ?? 'غير معروف';
        });
      }
    } catch (e) {
      _showError('خطأ في تحميل بيانات الموظف: $e');
    }
  }
  
  Future<void> _loadManagers() async {
  try {
    final result = await _loanService.getManagers();
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
  
  Future<void> _calculateInstallment() async {
    if (_loanAmount <= 0) {
      _showError('الرجاء إدخال مبلغ السلفة');
      return;
    }
    
    if (_loanAmount > _maxAllowedAmount) {
      _showError('مبلغ السلفة يتجاوز الحد المسموح');
      return;
    }
    
    setState(() => _isCalculating = true);
    
    try {
      final result = await _loanService.calculateInstallment(
        employeeId: widget.user['id'],
        loanAmount: _loanAmount,
        installmentMonths: _installmentMonths,
      );
      
      if (result['success']) {
        setState(() {
          _monthlyInstallment = (result['data']['monthlyInstallment'] ?? 0).toDouble();
        });
      } else {
        _showError(result['message'] ?? 'فشل في الحساب');
      }
    } catch (e) {
      _showError('خطأ في الحساب: $e');
    } finally {
      setState(() => _isCalculating = false);
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
  
  Future<void> _selectLoanDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _loanDate,
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
            ), dialogTheme: DialogThemeData(backgroundColor: Colors.white),
          ),
          child: Directionality(
            textDirection: TextDirection.rtl,
            child: child!,
          ),
        );
      },
    );
    
    if (picked != null) {
      setState(() => _loanDate = picked);
    }
  }
  
  Future<void> _selectPaybackDate() async {
    final DateTime? picked = await showDatePicker(
      context: context,
      initialDate: _expectedPaybackDate,
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(Duration(days: 365 * 2)),
      builder: (context, child) {
        return Theme(
          data: ThemeData.light().copyWith(
            colorScheme: ColorScheme.light(
              primary: primaryColor,
              onPrimary: Colors.white,
              surface: Colors.white,
              onSurface: Colors.black,
            ), dialogTheme: DialogThemeData(backgroundColor: Colors.white),
          ),
          child: Directionality(
            textDirection: TextDirection.rtl,
            child: child!,
          ),
        );
      },
    );
    
    if (picked != null) {
      setState(() => _expectedPaybackDate = picked);
    }
  }
  
  Future<void> _submitRequest() async {
    if (!_validateForm()) return;
    
    setState(() => _isSubmitting = true);
    
    try {
      final result = await _loanService.submitLoanRequest(
        employeeId: widget.user['id'],
        loanAmount: _loanAmount,
        loanDate: _loanDate,
        expectedPaybackDate: _expectedPaybackDate,
        installmentMonths: _installmentMonths,
        reason: _reason,
        approvingManagerId: _selectedManager?['id'] ?? -1,
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
    if (_loanAmount <= 0) {
      _showError('الرجاء إدخال مبلغ السلفة');
      return false;
    }
    
    if (_loanAmount > _maxAllowedAmount) {
      _showError('مبلغ السلفة يتجاوز الحد المسموح');
      return false;
    }
    
    if (_selectedManager == null) {
      _showError('الرجاء اختيار مدير للموافقة');
      return false;
    }
    
    if (_reason.isEmpty) {
      _showError('الرجاء كتابة سبب السلفة');
      return false;
    }
    
    if (_employeeStatus != 'مسموح بالسلفة') {
      _showError('حالتك لا تسمح بأخذ سلفة');
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
                  
                  Wrap(
                    spacing: 12,
                    runSpacing: 12,
                    alignment: WrapAlignment.center,
                    children: [
                      _buildInfoItem('الراتب الأساسي', 
                        '${widget.user['mainSalary']?.toStringAsFixed(2) ?? '0.00'} جنيه',
                        Icons.attach_money, Colors.green),
                      
                      _buildInfoItem('الحد الأقصى', 
                        '${_maxAllowedAmount.toStringAsFixed(2)} جنيه',
                        Icons.warning, Colors.orange),
                      
                      _buildInfoItem('السلف المستحقة', 
                        '${_currentLoanBalance.toStringAsFixed(2)} جنيه',
                        Icons.account_balance, Colors.red),
                      
                      _buildInfoItem('رصيد الصندوق', 
                        '${_friendshipBoxBalance.toStringAsFixed(2)} جنيه',
                        Icons.account_balance_wallet, Colors.purple),
                      
                      _buildInfoItem('الحالة', 
                        _employeeStatus,
                        Icons.verified_user, 
                        _employeeStatus == 'مسموح بالسلفة' ? Colors.green : Colors.red),
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
  
  Widget _buildInfoItem(String title, String value, IconData icon, Color color) {
    return Container(
      constraints: BoxConstraints(minWidth: 140),
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Column(
        children: [
          Icon(icon, size: 24, color: color),
          SizedBox(height: 5),
          Text(
            title,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 12,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 5),
          Text(
            value,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 14,
              fontWeight: FontWeight.bold,
              color: color,
              fontFamily: 'Tajawal',
            ),
          ),
        ],
      ),
    );
  }
  
  Widget _buildLoanDetailsSection() {
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
              'معلومات السلفة',
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
              textDirection: TextDirection.rtl,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        'مبلغ السلفة',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.grey[700],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                      SizedBox(height: 8),
                      TextFormField(
                        keyboardType: TextInputType.number,
                        textDirection: TextDirection.rtl,
                        decoration: InputDecoration(
                          hintText: 'أدخل المبلغ',
                          hintStyle: TextStyle(
                            color: Colors.grey[400],
                            fontFamily: 'Tajawal',
                          ),
                          suffixText: 'جنيه',
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
                          contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
                        ),
                        onChanged: (value) {
                          setState(() {
                            _loanAmount = double.tryParse(value) ?? 0;
                          });
                        },
                      ),
                    ],
                  ),
                ),
                
                SizedBox(width: 12),
                
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        'عدد الأشهر',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.grey[700],
                          fontFamily: 'Tajawal',
                        ),
                      ),
                      SizedBox(height: 8),
                      Container(
                        padding: EdgeInsets.symmetric(horizontal: 12),
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.grey[300]!),
                        ),
                        child: DropdownButton<int>(
                          value: _installmentMonths,
                          items: _installmentOptions.map((months) {
                            return DropdownMenuItem<int>(
                              value: months,
                              child: Text(
                                '$months شهر',
                                textDirection: TextDirection.rtl,
                                style: TextStyle(fontFamily: 'Tajawal'),
                              ),
                            );
                          }).toList(),
                          onChanged: (value) {
                            if (value != null) {
                              setState(() {
                                _installmentMonths = value;
                              });
                            }
                          },
                          underline: SizedBox(),
                          isExpanded: true,
                          icon: Icon(Icons.arrow_drop_down, color: primaryColor),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            
            SizedBox(height: 20),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                ElevatedButton.icon(
                  onPressed: _isCalculating ? null : _calculateInstallment,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: primaryColor,
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
                      : Icon(Icons.calculate, size: 20),
                  label: Text(
                    _isCalculating ? 'جاري الحساب...' : 'حساب القسط الشهري',
                    style: TextStyle(fontFamily: 'Tajawal'),
                  ),
                ),
              ],
            ),
            
            if (_monthlyInstallment > 0) ...[
              SizedBox(height: 20),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green[50],
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: Colors.green[100]!),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  textDirection: TextDirection.rtl,
                  children: [
                    Text(
                      'القسط الشهري:',
                      textDirection: TextDirection.rtl,
                      style: TextStyle(
                        fontSize: 16,
                        fontWeight: FontWeight.bold,
                        color: Colors.green[800],
                        fontFamily: 'Tajawal',
                      ),
                    ),
                    Text(
                      '${_monthlyInstallment.toStringAsFixed(2)} جنيه',
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
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  Widget _buildDatesSection() {
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
              'التواريخ',
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
                _buildDateField(
                  'تاريخ الطلب',
                  '${_loanDate.year}/${_loanDate.month}/${_loanDate.day}',
                  _selectLoanDate,
                  Icons.date_range,
                ),
                SizedBox(height: 16),
                _buildDateField(
                  'تاريخ السداد المتوقع',
                  '${_expectedPaybackDate.year}/${_expectedPaybackDate.month}/${_expectedPaybackDate.day}',
                  _selectPaybackDate,
                  Icons.date_range,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildDateField(String label, String value, VoidCallback onTap, IconData icon) {
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
              'سبب السلفة',
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
                  hintText: 'اكتب سبب طلب السلفة هنا...',
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
            'طلب سلفة من صندوق الزمالة',
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
                      // معلومات الموظف الحالي
                      _buildEmployeeInfoCard(),
                      
                      SizedBox(height: 16),
                      
                      // معلومات السلفة
                      _buildLoanDetailsSection(),
                      
                      SizedBox(height: 16),
                      
                      // التواريخ
                      _buildDatesSection(),
                      
                      SizedBox(height: 16),
                      
                      // المدير للموافقة
                      _buildManagerSection(),
                      
                      SizedBox(height: 16),
                      
                      // سبب السلفة
                      _buildReasonSection(),
                      
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
                                _buildNoteItem('• يجب توافر رصيد كافٍ في صندوق الزمالة'),
                                _buildNoteItem('• سيتم إشعار المدير المختص للموافقة على الطلب'),
                                _buildNoteItem('• يمكنك تتبع حالة الطلب من خلال سجل السلف'),
                                _buildNoteItem('• الحد الأقصى للسلفة هو 50% من الراتب الأساسي'),
                                _buildNoteItem('• القسط الشهري لا يتجاوز 30% من الراتب'),
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