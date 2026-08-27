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

  Map<String, dynamic>? _selectedManager;
  double _loanAmount = 0;
  int _installmentMonths = 1;
  String _reason = '';
  String _notes = '';
  final DateTime _loanDate = DateTime.now();
  final DateTime _expectedPaybackDate = DateTime.now().add(Duration(days: 30));

  List<dynamic> _managers = [];
  final List<int> _installmentOptions = [1, 3, 6, 12];
  int? _customInstallmentCount;

  double _basicSalary = 0;
  double _maxAllowedAmount = 0;
  double _currentLoanBalance = 0;
  double _remainingLimit = 0;
  bool _canTakeLoan = false;
  String _employeeStatus = '';

  bool _isLoading = false;
  bool _isSubmitting = false;

  final TextEditingController _loanAmountController = TextEditingController();
  final TextEditingController _reasonController = TextEditingController();
  final TextEditingController _notesController = TextEditingController();
  final TextEditingController _customInstallmentController = TextEditingController();

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color successColor = Color(0xFF10B981);
  final Color warningColor = Color(0xFFF59E0B);
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
      await _loadEmployeeDetails();
      await _loadManagers();
    } catch (e) {
      _showError('خطأ في تحميل البيانات');
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
          _remainingLimit = (_maxAllowedAmount - _currentLoanBalance).clamp(0, double.infinity);
        });
      }
    } catch (e) {}
  }

  Future<void> _loadManagers() async {
    try {
      final result = await _loanService.getManagers();
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

  bool _validateForm() {
    if (_loanAmount <= 0) {
      _showError('مبلغ السلفة يجب أن يكون أكبر من صفر');
      return false;
    }
    if (_loanAmount > _remainingLimit) {
      _showError('المبلغ المطلوب يتجاوز الحد المتبقي');
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
    final monthlyInstallment = _loanAmount / _installmentMonths;
    final maxMonthlyInstallment = _basicSalary * 0.3;
    if (monthlyInstallment > maxMonthlyInstallment) {
      _showError('القسط الشهري يتجاوز 30% من الراتب');
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
        _showSuccessDialog(result['message'] ?? 'تم تقديم طلب السلفة بنجاح', () {
          Navigator.pop(context, true);
        });
        clearForm();
      } else {
        _showError(result['message'] ?? 'فشل في تقديم الطلب');
      }
    } catch (e) {
      _showError('خطأ في تقديم الطلب');
    } finally {
      setState(() => _isSubmitting = false);
    }
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
          backgroundColor: Colors.white,
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('نجاح', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: successColor)),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(color: successColor.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: Icon(Icons.check_circle, color: successColor, size: 20),
              ),
            ],
          ),
          content: Text(message, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal')),
          actions: [
            TextButton(
              onPressed: onOk,
              style: TextButton.styleFrom(foregroundColor: primaryBlue),
              child: Text('موافق', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
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
                        subtitle: Text('${manager['jobTitleName'] ?? ''}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12)),
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
              Text('طلب سلفة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
              Text('من صندوق الزمالة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8))),
            ],
          ),
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
                    _buildEmployeeCard(),
                    SizedBox(height: 16),
                    _buildLoanAmountCard(),
                    SizedBox(height: 16),
                    _buildManagerCard(),
                    SizedBox(height: 16),
                    _buildReasonCard(),
                    SizedBox(height: 16),
                    _buildNotesCard(),
                    SizedBox(height: 20),
                    _buildActionButtons(),
                  ],
                ),
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
            Row(
              children: [
                Container(
                  padding: EdgeInsets.all(8),
                  decoration: BoxDecoration(color: lightBlue, borderRadius: BorderRadius.circular(10)),
                  child: Icon(icon, color: primaryBlue, size: 18),
                ),
                SizedBox(width: 10),
                Text(title, style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue)),
              ],
            ),
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
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              // Container(
              //   width: 45,
              //   height: 45,
              //   decoration: BoxDecoration(color: Colors.white.withOpacity(0.2), shape: BoxShape.circle),
              //   child: Center(child: Text((widget.user['fullName'] ?? '?')[0], style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'))),
              // ),
              // SizedBox(width: 12),
              // Expanded(
              //   child: Column(
              //     crossAxisAlignment: CrossAxisAlignment.start,
              //     children: [
              //       Text(widget.user['fullName'] ?? '', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
              //       Text('${widget.user['jobTitle']?['name'] ?? ''}', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, color: Colors.white.withOpacity(0.8))),
              //     ],
              //   ),
              // ),
             
              if (!_canTakeLoan)
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(color: errorColor.withValues(alpha: 0.2), borderRadius: BorderRadius.circular(12)),
                  child: Text('غير مسموح', style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.white, fontWeight: FontWeight.bold)),
                ),
            ],
          ),
          SizedBox(height: 14),
          GridView(
            shrinkWrap: true,
            physics: NeverScrollableScrollPhysics(),
            gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(crossAxisCount: 2, crossAxisSpacing: 8, mainAxisSpacing: 8, childAspectRatio: 1.7),
            children: [
              _buildWhiteInfoItem('الراتب', '${_basicSalary.toStringAsFixed(0)} ج', Icons.attach_money),
              _buildWhiteInfoItem('الحد الأقصى', '${_maxAllowedAmount.toStringAsFixed(0)} ج', Icons.warning),
              _buildWhiteInfoItem('السلف النشطة', '${_currentLoanBalance.toStringAsFixed(0)} ج', Icons.account_balance),
              _buildWhiteInfoItem('المتبقي', '${_remainingLimit.toStringAsFixed(0)} ج', Icons.account_balance_wallet),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildWhiteInfoItem(String label, String value, IconData icon) {
    return Container(
      padding: EdgeInsets.all(8),
      decoration: BoxDecoration(color: Colors.white.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(10)),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, color: Colors.white, size: 18),
          SizedBox(height: 4),
          Text(value, style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: Colors.white)),
          Text(label, style: TextStyle(fontFamily: 'Tajawal', fontSize: 9, color: Colors.white.withValues(alpha: 0.7))),
        ],
      ),
    );
  }

  Widget _buildLoanAmountCard() {
    return _buildSectionCard('معلومات السلفة', Icons.account_balance, [
      Text('مبلغ السلفة *', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
      SizedBox(height: 8),
      TextField(
        controller: _loanAmountController,
        keyboardType: TextInputType.number,
        decoration: InputDecoration(
          hintText: '0',
          suffixText: 'جنيه',
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: primaryBlue, width: 2)),
        ),
        onChanged: (value) => setState(() => _loanAmount = double.tryParse(value) ?? 0),
      ),
      if (_loanAmount > _remainingLimit) ...[
        SizedBox(height: 6),
        Row(children: [
          Icon(Icons.warning_amber, color: warningColor, size: 16),
          SizedBox(width: 4),
          Text('المبلغ يتجاوز الحد المتبقي', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: warningColor)),
        ]),
      ],
      SizedBox(height: 16),
      Text('عدد الأقساط *', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, fontWeight: FontWeight.bold, color: darkBlue)),
      SizedBox(height: 8),
      Wrap(
        spacing: 8,
        runSpacing: 8,
        children: [
          ..._installmentOptions.map((months) {
            final isSelected = _installmentMonths == months && _customInstallmentCount == null;
            return GestureDetector(
              onTap: () => setState(() { _installmentMonths = months; _customInstallmentCount = null; _customInstallmentController.clear(); }),
              child: Container(
                padding: EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                decoration: BoxDecoration(
                  color: isSelected ? primaryBlue : cardColor,
                  borderRadius: BorderRadius.circular(8),
                  border: Border.all(color: isSelected ? primaryBlue : borderColor, width: isSelected ? 2 : 1),
                ),
                child: Text(months == 1 ? 'مرة واحدة' : '$months شهور', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, fontWeight: isSelected ? FontWeight.bold : FontWeight.normal, color: isSelected ? Colors.white : Colors.grey[700])),
              ),
            );
          }),
          SizedBox(
            width: 100,
            child: Row(children: [
              Expanded(
                child: TextField(
                  controller: _customInstallmentController,
                  keyboardType: TextInputType.number,
                  textAlign: TextAlign.center,
                  decoration: InputDecoration(hintText: 'عدد', border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)), isDense: true),
                  onChanged: (value) {
                    final count = int.tryParse(value);
                    if (count != null && count > 0) setState(() { _installmentMonths = count; _customInstallmentCount = count; });
                  },
                ),
              ),
              SizedBox(width: 4),
              Text('شهر', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[600])),
            ]),
          ),
        ],
      ),
      if (_loanAmount > 0 && _installmentMonths > 0) ...[
        SizedBox(height: 14),
        Container(
          padding: EdgeInsets.all(12),
          decoration: BoxDecoration(color: successColor.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(10), border: Border.all(color: successColor.withValues(alpha: 0.3))),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('القسط الشهري', style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500])),
                Text('${(_loanAmount / _installmentMonths).toStringAsFixed(0)} ج', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: successColor)),
              ]),
              Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                Text('الإجمالي', style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500])),
                Text('${_loanAmount.toStringAsFixed(0)} ج', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: darkBlue)),
              ]),
            ],
          ),
        ),
      ],
    ]);
  }

  Widget _buildManagerCard() {
    return _buildSectionCard('المدير للموافقة', Icons.person, [
      GestureDetector(
        onTap: _selectManager,
        child: Container(
          padding: EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: lightBlue.withValues(alpha: 0.5),
            borderRadius: BorderRadius.circular(10),
            border: Border.all(color: primaryBlue.withValues(alpha: 0.3)),
          ),
          child: Row(
            children: [
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
            ],
          ),
        ),
      ),
    ]);
  }

  Widget _buildReasonCard() {
    return _buildSectionCard('سبب السلفة', Icons.info_outline, [
      TextField(
        controller: _reasonController,
        maxLines: 3,
        decoration: InputDecoration(
          hintText: 'أدخل سبب السلفة...',
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
          enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: borderColor)),
          focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: BorderSide(color: primaryBlue, width: 2)),
        ),
        onChanged: (value) => setState(() => _reason = value),
      ),
    ]);
  }

  Widget _buildNotesCard() {
    return _buildSectionCard('ملاحظات (اختياري)', Icons.note, [
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
    ]);
  }

  Widget _buildActionButtons() {
    return Row(
      children: [
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
                      Text('تقديم الطلب', style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold, color: Colors.white)),
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
          child: Text('إلغاء', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: Colors.grey[600])),
        ),
      ],
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