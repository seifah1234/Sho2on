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
  String _notes = '';
  DateTime _loanDate = DateTime.now();
  DateTime _expectedPaybackDate = DateTime.now().add(Duration(days: 30));
  
  // قوائم البيانات
  List<dynamic> _managers = [];
  final List<int> _installmentOptions = [1, 3, 6, 12];
  int? _customInstallmentCount;
  
  // معلومات الحسابات - مطابقة للـ API
  double _basicSalary = 0;
  double _maxAllowedAmount = 0;
  double _currentLoanBalance = 0;
  double _remainingLimit = 0;
  bool _canTakeLoan = false;
  String _employeeStatus = '';
  
  // حالة التحميل
  bool _isLoading = false;
  bool _isSubmitting = false;
  
  // Controllers
  final TextEditingController _loanAmountController = TextEditingController();
  final TextEditingController _reasonController = TextEditingController();
  final TextEditingController _notesController = TextEditingController();
  final TextEditingController _customInstallmentController = TextEditingController();
  
  // ألوان التصميم
  final Color primaryColor = Color(0xFF1976D2);
  final Color accentColor = Color(0xFF4CAF50);
  final Color warningColor = Color(0xFFFF9800);
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
      await _loadEmployeeDetails();
      await _loadManagers();
    } catch (e) {
      _showError('خطأ في تحميل البيانات: $e');
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Future<void> _loadEmployeeDetails() async {
    try {
      final result = await _loanService.getEmployee(widget.user['id']);
      if (result['success']) {
        final data = result['data'];
        setState(() {
          _basicSalary = (data['basicSalary'] ?? 0).toDouble();
          _maxAllowedAmount = (data['maxAllowedAmount'] ?? 0).toDouble();
          _currentLoanBalance = (data['currentLoanBalance'] ?? 0).toDouble();
          _canTakeLoan = data['canTakeLoan'] ?? false;
          _employeeStatus = data['employeeStatus'] ?? 'غير معروف';
          
          // حساب المتبقي للحد الأقصى
          _remainingLimit = _maxAllowedAmount - _currentLoanBalance;
          if (_remainingLimit < 0) _remainingLimit = 0;
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل بيانات الموظف');
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
          _managers = result['data'] ?? [];
          
          // اختيار مدير الموظف كافتراضي
          if (widget.user['managerId'] != null && _managers.isNotEmpty) {
            final managerId = widget.user['managerId'];
            _selectedManager = _managers.firstWhere(
              (manager) => manager['id'] == managerId,
              orElse: () => _managers.isNotEmpty ? _managers[0] : null,
            );
          } else if (_managers.isNotEmpty) {
            _selectedManager = _managers[0];
          }
        });
      } else {
        _showError(result['message'] ?? 'فشل في تحميل المديرين');
      }
    } catch (e) {
      _showError('خطأ في تحميل المديرين: $e');
    }
  }
  
  // حساب القسط الشهري محلياً (مثل الويب)
  void _calculateInstallmentLocally() {
    if (_loanAmount > 0 && _installmentMonths > 0) {
      final monthlyInstallment = _loanAmount / _installmentMonths;
      final maxMonthlyInstallment = _basicSalary * 0.3;
      
      if (monthlyInstallment > maxMonthlyInstallment) {
        _showError('القسط الشهري يتجاوز 30% من الراتب');
      }
    }
  }
  
  // التحقق من صحة النموذج (مثل ValidateLoanRequest في API)
  bool _validateForm() {
    if (_loanAmount <= 0) {
      _showError('مبلغ السلفة يجب أن يكون أكبر من صفر');
      return false;
    }
    
    if (_loanAmount > _remainingLimit) {
      _showError('المبلغ المطلوب يتجاوز الحد المتبقي (${_remainingLimit.toStringAsFixed(0)} ج)');
      return false;
    }
    
    if (_installmentMonths <= 0) {
      _showError('عدد الأشهر يجب أن يكون أكبر من صفر');
      return false;
    }
    
    if (_reason.isEmpty) {
      _showError('سبب السلفة مطلوب');
      return false;
    }
    
    if (_selectedManager == null) {
      _showError('الرجاء اختيار مدير للموافقة');
      return false;
    }
    
    if (!_canTakeLoan) {
      _showError('هذا الموظف غير مسموح له بأخذ سلفة');
      return false;
    }
    
    // التحقق من القسط الشهري (30% من الراتب)
    final monthlyInstallment = _loanAmount / _installmentMonths;
    final maxMonthlyInstallment = _basicSalary * 0.3;
    if (monthlyInstallment > maxMonthlyInstallment) {
      _showError('القسط الشهري (${monthlyInstallment.toStringAsFixed(0)} ج) يتجاوز 30% من الراتب (${maxMonthlyInstallment.toStringAsFixed(0)} ج)');
      return false;
    }
    
    return true;
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
        notes: _notes,
        approvingManagerId: _selectedManager?['id'] ?? -1,
      );
      
      if (result['success']) {
        final data = result['data'];
        _showSuccessDialog(
          result['message'] ?? 'تم تقديم طلب السلفة بنجاح',
          () {
            Navigator.pop(context, true);
          },
        );  
        clearForm();      
      } else {
        _showError(result['message'] ?? 'فشل في تقديم الطلب');
      }
    } catch (e) {
      _showError('خطأ في تقديم الطلب: $e');
    } finally {
      setState(() => _isSubmitting = false);
    }
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
              onPressed: onOk,
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
  
  Future<void> _selectManager() async {
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
                'اختر المدير للموافقة',
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
                    final isSelected = _selectedManager != null && 
                                     _selectedManager!['id'] == manager['id'];
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
      setState(() => _expectedPaybackDate = picked);
    }
  }
  
  Widget _buildEmployeeInfoCard() {
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
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.blue[100]!),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Row(
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
                              '${widget.user['department']?['name'] ?? ''} - ${widget.user['jobTitle']?['name'] ?? ''}',
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
                      childAspectRatio: 1.8,
                    ),
                    children: [
                      _buildInfoItem('الراتب الأساسي', 
                        '${_basicSalary.toStringAsFixed(0)} ج',
                        Icons.attach_money, Colors.green),
                      
                      _buildInfoItem('الحد الأقصى', 
                        '${_maxAllowedAmount.toStringAsFixed(0)} ج',
                        Icons.warning, Colors.orange),
                      
                      _buildInfoItem('السلف النشطة', 
                        '${_currentLoanBalance.toStringAsFixed(0)} ج',
                        Icons.account_balance, Colors.red),
                      
                      _buildInfoItem('المتبقي للحد', 
                        '${_remainingLimit.toStringAsFixed(0)} ج',
                        Icons.account_balance_wallet, 
                        _remainingLimit > 0 ? Colors.purple : Colors.red),
                    ],
                  ),
                  
                  if (!_canTakeLoan)
                    Padding(
                      padding: const EdgeInsets.only(top: 12),
                      child: Container(
                        padding: EdgeInsets.all(12),
                        decoration: BoxDecoration(
                          color: Colors.red[50],
                          borderRadius: BorderRadius.circular(8),
                          border: Border.all(color: Colors.red[200]!),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.block, color: Colors.red, size: 20),
                            SizedBox(width: 8),
                            Expanded(
                              child: Text(
                                _employeeStatus,
                                textDirection: TextDirection.rtl,
                                style: TextStyle(
                                  color: Colors.red[700],
                                  fontFamily: 'Tajawal',
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ),
                          ],
                        ),
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
  
  Widget _buildInfoItem(String title, String value, IconData icon, Color color) {
    return Container(
      padding: EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(color: color.withValues(alpha: 0.3), width: 1),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 15, color: color),
          SizedBox(height: 2),
          Text(
            title,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 10,
              color: Colors.grey[600],
              fontFamily: 'Tajawal',
            ),
          ),
          SizedBox(height: 2),
          Text(
            value,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontSize: 13,
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
            
            // مبلغ السلفة
            Text(
              'مبلغ السلفة *',
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
              controller: _loanAmountController,
              keyboardType: TextInputType.number,
              textDirection: TextDirection.rtl,
              decoration: InputDecoration(
                hintText: '0',
                hintStyle: TextStyle(
                  color: Colors.grey[400],
                  fontFamily: 'Tajawal',
                ),
                suffixText: 'جنيه',
                suffixStyle: TextStyle(
                  color: Colors.grey[600],
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
                contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
              ),
              onChanged: (value) {
                setState(() {
                  _loanAmount = double.tryParse(value) ?? 0;
                });
              },
            ),
            
            if (_loanAmount > _remainingLimit)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Row(
                  children: [
                    Icon(Icons.warning_amber, color: warningColor, size: 16),
                    SizedBox(width: 4),
                    Text(
                      'المبلغ المطلوب يتجاوز الحد المتبقي',
                      style: TextStyle(
                        color: warningColor,
                        fontSize: 11,
                        fontFamily: 'Tajawal',
                      ),
                    ),
                  ],
                ),
              ),
            
            SizedBox(height: 20),
            
            // عدد الأقساط
            Text(
              'عدد الأقساط *',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.grey[700],
                fontFamily: 'Tajawal',
                fontSize: 13,
              ),
            ),
            SizedBox(height: 8),
            
            Wrap(
              spacing: 8,
              runSpacing: 8,
              alignment: WrapAlignment.start,
              children: [
                ..._installmentOptions.map((months) {
                  final isSelected = _installmentMonths == months && _customInstallmentCount == null;
                  return GestureDetector(
                    onTap: () {
                      setState(() {
                        _installmentMonths = months;
                        _customInstallmentCount = null;
                        _customInstallmentController.clear();
                      });
                    },
                    child: Container(
                      padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                      decoration: BoxDecoration(
                        color: isSelected ? primaryColor : Colors.white,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(
                          color: isSelected ? primaryColor : borderColor,
                          width: isSelected ? 2 : 1,
                        ),
                      ),
                      child: Text(
                        months == 1 ? 'مرة واحدة' : '$months شهور',
                        style: TextStyle(
                          color: isSelected ? Colors.white : Colors.grey[700],
                          fontFamily: 'Tajawal',
                          fontWeight: isSelected ? FontWeight.bold : FontWeight.normal,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  );
                }).toList(),
                
                // Custom installment input
                Container(
                  width: 120,
                  child: Row(
                    children: [
                      Expanded(
                        child: TextField(
                          controller: _customInstallmentController,
                          keyboardType: TextInputType.number,
                          textDirection: TextDirection.rtl,
                          textAlign: TextAlign.center,
                          decoration: InputDecoration(
                            hintText: 'عدد',
                            hintStyle: TextStyle(
                              color: Colors.grey[400],
                              fontFamily: 'Tajawal',
                              fontSize: 12,
                            ),
                            border: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(8),
                              borderSide: BorderSide(color: borderColor),
                            ),
                            enabledBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(8),
                              borderSide: BorderSide(color: borderColor),
                            ),
                            focusedBorder: OutlineInputBorder(
                              borderRadius: BorderRadius.circular(8),
                              borderSide: BorderSide(color: primaryColor, width: 2),
                            ),
                            contentPadding: EdgeInsets.symmetric(horizontal: 8, vertical: 10),
                            isDense: true,
                          ),
                          onChanged: (value) {
                            final count = int.tryParse(value);
                            if (count != null && count > 0) {
                              setState(() {
                                _installmentMonths = count;
                                _customInstallmentCount = count;
                              });
                            } else if (value.isEmpty) {
                              setState(() {
                                _customInstallmentCount = null;
                              });
                            }
                          },
                        ),
                      ),
                      SizedBox(width: 4),
                      Text(
                        'شهر',
                        style: TextStyle(
                          color: Colors.grey[600],
                          fontFamily: 'Tajawal',
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            
            // عرض القسط الشهري
            if (_loanAmount > 0 && _installmentMonths > 0) ...[
              SizedBox(height: 20),
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.green[50],
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.green[100]!),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  textDirection: TextDirection.rtl,
                  children: [
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'القسط الشهري',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.green[700],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        Text(
                          '${(_loanAmount / _installmentMonths).toStringAsFixed(0)} جنيه',
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
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          'إجمالي المبلغ',
                          textDirection: TextDirection.rtl,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.green[700],
                            fontFamily: 'Tajawal',
                          ),
                        ),
                        Text(
                          '${_loanAmount.toStringAsFixed(0)} جنيه',
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
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
  
  Widget _buildManagerSection() {
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
              'اختيار المدير للموافقة',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
            
            if (_managers.isEmpty)
              Container(
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: Colors.grey[50],
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: borderColor),
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
            else
              GestureDetector(
                onTap: _selectManager,
                child: Container(
                  padding: EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.blue[50],
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: primaryColor),
                  ),
                  child: Row(
                    textDirection: TextDirection.rtl,
                    children: [
                      CircleAvatar(
                        radius: 25,
                        backgroundColor: primaryColor,
                        child: Text(
                          _selectedManager != null ? (_selectedManager!['fullName'] ?? '?')[0] : '?',
                          style: TextStyle(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                            fontSize: 18,
                          ),
                        ),
                      ),
                      SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.end,
                          children: [
                            Text(
                              _selectedManager?['fullName'] ?? 'اختر المدير',
                              textDirection: TextDirection.rtl,
                              style: TextStyle(
                                fontSize: 15,
                                fontWeight: FontWeight.bold,
                                color: Colors.blue[900],
                                fontFamily: 'Tajawal',
                              ),
                            ),
                            if (_selectedManager != null)
                              Text(
                                '${_selectedManager!['jobTitleName'] ?? ''}',
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
                      Icon(Icons.edit, color: primaryColor, size: 20),
                    ],
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
  
  Widget _buildReasonSection() {
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
            TextField(
              controller: _reasonController,
              maxLines: 4,
              textDirection: TextDirection.rtl,
              decoration: InputDecoration(
                hintText: 'أدخل سبب السلفة...',
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
          ],
        ),
      ),
    );
  }
  
  Widget _buildNotesSection() {
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
              'ملاحظات',
              textDirection: TextDirection.rtl,
              style: TextStyle(
                fontSize: 18,
                fontWeight: FontWeight.bold,
                color: primaryColor,
                fontFamily: 'Tajawal',
              ),
            ),
            SizedBox(height: 16),
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
                _isSubmitting ? 'جاري الإرسال...' : 'تقديم الطلب',
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
            'طلب سلفة جديدة',
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
                      _buildEmployeeInfoCard(),
                      SizedBox(height: 16),
                      _buildLoanDetailsSection(),
                      SizedBox(height: 16),
                      _buildManagerSection(),
                      SizedBox(height: 16),
                      _buildReasonSection(),
                      SizedBox(height: 16),
                      _buildNotesSection(),
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
    _loanAmountController.dispose();
    _reasonController.dispose();
    _notesController.dispose();
    _customInstallmentController.dispose();
    super.dispose();
  }

  void clearForm() {
    _loanAmountController.clear();
    _reasonController.clear();
    _notesController.clear();
    _customInstallmentController.clear();
    setState(() {
      _loanAmount = 0;
      _installmentMonths = 1;
      _selectedManager = null;
      _reason = '';
      _notes = '';
      _customInstallmentCount = null;
    });
  }
}