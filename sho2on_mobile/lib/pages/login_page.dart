import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../utils/device_helper.dart';
import '../utils/local_storage.dart';
import 'main_page.dart';
import 'register_page.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage>
    with SingleTickerProviderStateMixin {
  final _username = TextEditingController();
  final _pass = TextEditingController();
  final _auth = AuthService();
  bool loading = false;
  bool _obscurePassword = true;

  late AnimationController _animationController;
  late Animation<double> _fadeAnimation;
  late Animation<Offset> _slideAnimation;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;

  @override
  void initState() {
    super.initState();
    _animationController = AnimationController(
      vsync: this,
      duration: Duration(milliseconds: 800),
    );
    _fadeAnimation = CurvedAnimation(
      parent: _animationController,
      curve: Curves.easeIn,
    );
    _slideAnimation = Tween<Offset>(begin: Offset(0, 0.1), end: Offset.zero)
        .animate(
          CurvedAnimation(parent: _animationController, curve: Curves.easeOut),
        );
    _animationController.forward();
  }

  Future<void> doLogin() async {
    if (_username.text.isEmpty || _pass.text.isEmpty) {
      showError('يرجى ملء جميع الحقول');
      return;
    }

    setState(() => loading = true);
    final deviceId = await DeviceHelper.getDeviceId();
    final result = await _auth.login(
      _username.text.trim(),
      _pass.text,
      deviceId,
    );

    if (result.containsKey("500")) {
      setState(() => loading = false);
      showError('تأكد من اتصالك بقاعدة البيانات وحاول مرة أخرى');
      return;
    }
    setState(() => loading = false);

    if (!result.containsKey("200")) {
      showError(result.keys.first);
      return;
    }

    final user = result["200"];
    if (user == null) {
      showError('بيانات الدخول غير صحيحة');
      return;
    }

    await LocalStorage.saveUser(user);

    Navigator.pushReplacement(
      context,
      MaterialPageRoute(builder: (_) => MainPage(user)),
    );
  }

  void showError(String message) {
    showDialog(
      context: context,
      builder: (_) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          backgroundColor: Colors.white,
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'خطأ',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: Color(0xFFEF4444),
                ),
              ),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: Color(0xFFEF4444).withValues(alpha: 0.1),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Icons.error_outline,
                  color: Color(0xFFEF4444),
                  size: 20,
                ),
              ),
            ],
          ),
          content: Text(
            message,
            textDirection: TextDirection.rtl,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 14),
          ),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(foregroundColor: primaryBlue),
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
                child: Column(children: [_buildHeader(), _buildLoginForm()]),
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
      padding: EdgeInsets.only(top: 40, bottom: 40, left: 24, right: 24),
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
          BoxShadow(
            color: primaryBlue.withValues(alpha: 0.3),
            blurRadius: 20,
            offset: Offset(0, 10),
          ),
        ],
      ),
      child: Column(
        children: [
          // Logo
          Container(
            width: 90,
            height: 90,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.15),
              shape: BoxShape.circle,
              border: Border.all(
                color: Colors.white.withValues(alpha: 0.3),
                width: 3,
              ),
            ),
            child: Icon(Icons.fingerprint, size: 50, color: Colors.white),
          ),
          SizedBox(height: 20),
          Text(
            'نظام البصمة الذكي',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 26,
              fontWeight: FontWeight.bold,
              color: Colors.white,
            ),
          ),
          SizedBox(height: 8),
          Text(
            'مرحباً بعودتك 👋',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 15,
              color: Colors.white.withValues(alpha: 0.9),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildLoginForm() {
    return Padding(
      padding: EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(height: 10),

          // Username field
          _buildLabel('اسم المستخدم'),
          SizedBox(height: 10),
          _buildTextField(
            controller: _username,
            hintText: 'أدخل اسم المستخدم',
            icon: Icons.person_outline,
          ),

          SizedBox(height: 24),

          // Password field
          _buildLabel('كلمة المرور'),
          SizedBox(height: 10),
          _buildTextField(
            controller: _pass,
            hintText: 'أدخل كلمة المرور',
            icon: Icons.lock_outline,
            obscureText: _obscurePassword,
            suffixIcon: IconButton(
              icon: Icon(
                _obscurePassword ? Icons.visibility_off : Icons.visibility,
                color: Colors.grey[400],
                size: 20,
              ),
              onPressed: () =>
                  setState(() => _obscurePassword = !_obscurePassword),
            ),
          ),

          SizedBox(height: 32),

          // Login button
          Container(
            height: 56,
            decoration: BoxDecoration(
              gradient: LinearGradient(colors: [primaryBlue, darkBlue]),
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(
                  color: primaryBlue.withValues(alpha: 0.4),
                  blurRadius: 15,
                  offset: Offset(0, 6),
                ),
              ],
            ),
            child: ElevatedButton(
              onPressed: loading ? null : doLogin,
              style: ElevatedButton.styleFrom(
                backgroundColor: Colors.transparent,
                shadowColor: Colors.transparent,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(16),
                ),
              ),
              child: loading
                  ? SizedBox(
                      width: 24,
                      height: 24,
                      child: CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2,
                      ),
                    )
                  : Row(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.login, size: 20, color: Colors.white),
                        SizedBox(width: 10),
                        Text(
                          'تسجيل الدخول',
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 16,
                            fontWeight: FontWeight.bold,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
            ),
          ),

          SizedBox(height: 24),

          // Register link
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Text(
                'ليس لديك حساب؟',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 14,
                  color: Colors.grey[500],
                ),
              ),
              SizedBox(width: 6),
              TextButton(
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(builder: (_) => const RegisterPage()),
                ),
                style: TextButton.styleFrom(
                  foregroundColor: primaryBlue,
                  padding: EdgeInsets.zero,
                ),
                child: Text(
                  'إنشاء حساب',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ],
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
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: TextField(
        controller: controller,
        textDirection: TextDirection.rtl,
        obscureText: obscureText,
        decoration: InputDecoration(
          hintText: hintText,
          hintStyle: TextStyle(
            fontFamily: 'Tajawal',
            color: Colors.grey[400],
            fontSize: 14,
          ),
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
    _username.dispose();
    _pass.dispose();
    _animationController.dispose();
    super.dispose();
  }
}
