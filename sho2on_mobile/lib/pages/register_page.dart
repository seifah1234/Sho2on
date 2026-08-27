import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../utils/device_helper.dart';
import 'login_page.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> with SingleTickerProviderStateMixin {
  final _idCtrl = TextEditingController();
  final _usernameCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  final AuthService _auth = AuthService();
  bool loading = false;
  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;

  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _slideAnimation;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color successColor = Color(0xFF10B981);
  final Color errorColor = Color(0xFFEF4444);

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
      vsync: this,
      duration: Duration(milliseconds: 800),
    );
    _fadeAnimation = CurvedAnimation(parent: _animationController, curve: Curves.easeIn);
    _slideAnimation = Tween<Offset>(begin: Offset(0, 0.1), end: Offset.zero)
        .animate(CurvedAnimation(parent: _animationController, curve: Curves.easeOut));
    _animationController.forward();
  }

  Future<void> submit() async {
    if (_idCtrl.text.trim().isEmpty || _usernameCtrl.text.trim().isEmpty || _passCtrl.text.isEmpty || _confirmCtrl.text.isEmpty) {
      showError('يرجى ملء جميع الحقول');
      return;
    }

    if (_passCtrl.text != _confirmCtrl.text) {
      showError('كلمات المرور غير متطابقة');
      return;
    }

    if (_passCtrl.text.length < 6) {
      showError('كلمة المرور يجب أن تكون على الأقل 6 أحرف');
      return;
    }

    setState(() => loading = true);
    final deviceId = await DeviceHelper.getDeviceId();
    final res = await _auth.register(_idCtrl.text.trim(), _usernameCtrl.text.trim(), _passCtrl.text, deviceId);
    setState(() => loading = false);

    if (res == 'success') {
      showSuccess('تم إنشاء الحساب بنجاح');
    } else {
      showError(res.toString());
    }
  }

  void showError(String msg) {
    showDialog(
      context: context,
      builder: (_) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          backgroundColor: Colors.white,
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('خطأ', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: errorColor)),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(color: errorColor.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: Icon(Icons.error_outline, color: errorColor, size: 20),
              ),
            ],
          ),
          content: Text(msg, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal', fontSize: 14)),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(foregroundColor: primaryBlue),
              child: Text('موافق', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  void showSuccess(String msg) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => Directionality(
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
                child: Icon(Icons.check_circle_outline, color: successColor, size: 20),
              ),
            ],
          ),
          content: Text(msg, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal', fontSize: 14)),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(context);
                Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => LoginPage()));
              },
              style: TextButton.styleFrom(foregroundColor: primaryBlue),
              child: Text('حسناً', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        body: SafeArea(
          child: FadeTransition(
            opacity: _fadeAnimation,
            child: SlideTransition(
              position: _slideAnimation,
              child: SingleChildScrollView(
                child: Column(
                  children: [
                    _buildHeader(),
                    _buildRegisterForm(),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildHeader() {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.only(top: 30, bottom: 35, left: 24, right: 24),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topRight,
          end: Alignment.bottomLeft,
          colors: [darkBlue, primaryBlue],
        ),
        borderRadius: BorderRadius.only(
          bottomLeft: Radius.circular(40),
          bottomRight: Radius.circular(40),
        ),
        boxShadow: [
          BoxShadow(color: primaryBlue.withValues(alpha: 0.3), blurRadius: 20, offset: Offset(0, 10)),
        ],
      ),
      child: Column(
        children: [
          // Back button + Logo
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              IconButton(
                icon: Icon(Icons.arrow_back, color: Colors.white, size: 26),
                onPressed: () => Navigator.pop(context),
              ),
              Container(
                width: 70,
                height: 70,
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.15),
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white.withValues(alpha: 0.3), width: 2),
                ),
                child: Icon(Icons.person_add_alt_1, size: 36, color: Colors.white),
              ),
              SizedBox(width: 48), // For balance
            ],
          ),
          SizedBox(height: 16),
          Text(
            'إنشاء حساب جديد',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 24,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          SizedBox(height: 6),
          Text(
            'املأ البيانات التالية لإنشاء حسابك',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 13,
              color: Colors.white.withValues(alpha: 0.85),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRegisterForm() {
    return Padding(
      padding: EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(height: 10),

          // كود الموظف
          _buildLabel('كود الموظف'),
          SizedBox(height: 10),
          _buildTextField(
            controller: _idCtrl,
            hintText: 'أدخل كود الموظف',
            icon: Icons.badge_outlined,
          ),

          SizedBox(height: 20),

          // اسم المستخدم
          _buildLabel('اسم المستخدم'),
          SizedBox(height: 10),
          _buildTextField(
            controller: _usernameCtrl,
            hintText: 'أدخل اسم المستخدم',
            icon: Icons.person_outline,
          ),

          SizedBox(height: 20),

          // كلمة المرور
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _buildLabel('كلمة المرور'),
              Text(
                '6 أحرف على الأقل',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
              ),
            ],
          ),
          SizedBox(height: 10),
          _buildTextField(
            controller: _passCtrl,
            hintText: 'أدخل كلمة المرور',
            icon: Icons.lock_outline,
            obscureText: _obscurePassword,
            suffixIcon: IconButton(
              icon: Icon(
                _obscurePassword ? Icons.visibility_off : Icons.visibility,
                color: Colors.grey[400],
                size: 20,
              ),
              onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
            ),
          ),

          SizedBox(height: 20),

          // تأكيد كلمة المرور
          _buildLabel('تأكيد كلمة المرور'),
          SizedBox(height: 10),
          _buildTextField(
            controller: _confirmCtrl,
            hintText: 'أعد إدخال كلمة المرور',
            icon: Icons.lock_reset_outlined,
            obscureText: _obscureConfirmPassword,
            suffixIcon: IconButton(
              icon: Icon(
                _obscureConfirmPassword ? Icons.visibility_off : Icons.visibility,
                color: Colors.grey[400],
                size: 20,
              ),
              onPressed: () => setState(() => _obscureConfirmPassword = !_obscureConfirmPassword),
            ),
          ),

          SizedBox(height: 32),

          // زر إنشاء الحساب
          Container(
            height: 56,
            decoration: BoxDecoration(
              gradient: LinearGradient(colors: [primaryBlue, darkBlue]),
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(color: primaryBlue.withValues(alpha: 0.4), blurRadius: 15, offset: Offset(0, 6)),
              ],
            ),
            child: ElevatedButton(
              onPressed: loading ? null : submit,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.transparent,
                shadowColor: Colors.transparent,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
              ),
              child: loading
                  ? SizedBox(width: 24, height: 24, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                  : Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.person_add, size: 20, color: Colors.white),
                        SizedBox(width: 10),
                        Text('إنشاء الحساب', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white)),
                      ],
                    ),
            ),
          ),

          SizedBox(height: 24),

          // رابط تسجيل الدخول
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text('لديك حساب بالفعل؟', style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, color: Colors.grey[500])),
              SizedBox(width: 6),
              TextButton(
                onPressed: () => Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => LoginPage())),
                style: TextButton.styleFrom(foregroundColor: primaryBlue, padding: EdgeInsets.zero),
                child: Text('تسجيل الدخول', style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold)),
              ),
            ],
          ),

          SizedBox(height: 32),

          // شروط الاستخدام
          Container(
            padding: EdgeInsets.all(14),
            decoration: BoxDecoration(
              color: cardColor,
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: Color(0xFFE5E7EB)),
            ),
            child: Row(
              children: [
                Icon(Icons.shield_outlined, color: primaryBlue, size: 22),
                SizedBox(width: 10),
                Expanded(
                  child: Text(
                    'بحسابك، فإنك توافق على شروط الاستخدام وسياسة الخصوصية',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500]),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLabel(String text) {
    return Text(
      text,
      textDirection: TextDirection.rtl,
      style: TextStyle(
        fontFamily: 'Tajawal',
        fontSize: 14,
        color: darkBlue,
        fontWeight: FontWeight.w600,
      ),
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String hintText,
    required IconData icon,
    bool obscureText = false,
    Widget? suffixIcon,
  }) {
    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(color: Colors.black.withValues(alpha: 0.03), blurRadius: 8, offset: Offset(0, 2)),
        ],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: TextField(
        controller: controller,
        textDirection: TextDirection.rtl,
        obscureText: obscureText,
        decoration: InputDecoration(
          hintText: hintText,
          hintStyle: TextStyle(fontFamily: 'Tajawal', color: Colors.grey[400], fontSize: 14),
          border: InputBorder.none,
          contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 16),
          prefixIcon: Container(
            margin: EdgeInsets.all(8),
            padding: EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: lightBlue,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: primaryBlue, size: 20),
          ),
          suffixIcon: suffixIcon,
        ),
        style: TextStyle(fontFamily: 'Tajawal', fontSize: 15),
      ),
    );
  }

  @override
  void dispose() {
    _idCtrl.dispose();
    _usernameCtrl.dispose();
    _passCtrl.dispose();
    _confirmCtrl.dispose();
    _animationController.dispose();
    super.dispose();
  }
}