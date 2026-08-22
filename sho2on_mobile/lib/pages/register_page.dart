import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../utils/device_helper.dart';
import 'login_page.dart';

class RegisterPage extends StatefulWidget {
  const RegisterPage({super.key});
  @override
  State<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends State<RegisterPage> {
  final _idCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  final _confirmCtrl = TextEditingController();
  final AuthService _auth = AuthService();
  bool loading = false;
  bool _obscurePassword = true;
  bool _obscureConfirmPassword = true;

  // ألوان التصميم (متوافقة مع الصفحات الأخرى)
  final Color babyBlue = Color(0xFF89CFF0);
  final Color darkBlue = Color(0xFF1E3A8A);
  final Color lightGray = Color(0xFFF5F7FA);
  final Color successColor = Color(0xFF4CAF50);
  final Color errorColor = Color(0xFFF44336);

  Future<void> submit() async {
    if (_idCtrl.text.trim().isEmpty || _passCtrl.text.isEmpty || _confirmCtrl.text.isEmpty) {
      showError('يرجى ملء جميع الحقول');
      return;
    }
    
    
    if (_passCtrl.text != _confirmCtrl.text) {
      showError('كلمات المرور غير متطابقة');
      return;
    }
    
    // التحقق من قوة كلمة المرور
    if (_passCtrl.text.length < 6) {
      showError('كلمة المرور يجب أن تكون على الأقل 6 أحرف');
      return;
    }

    setState(() => loading = true);
    final deviceId = await DeviceHelper.getDeviceId();
    final res = await _auth.register(_idCtrl.text.trim(), _passCtrl.text, deviceId);
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
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('خطأ',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: errorColor,
                ),
              ),
              SizedBox(width: 10),
              Icon(Icons.error_outline, color: errorColor),
            ],
          ),
          content: Text(msg,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 14,
            ),
          ),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(
                foregroundColor: darkBlue,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text('موافق',
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

  void showSuccess(String msg) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text('نجاح',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: successColor,
                ),
              ),
              SizedBox(width: 10),
              Icon(Icons.check_circle_outline, color: successColor),
            ],
          ),
          content: Text(msg,
            textDirection: TextDirection.rtl,
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 14,
            ),
          ),
          actionsAlignment: MainAxisAlignment.start,
          actions: [
            TextButton(
              onPressed: () {
                Navigator.pop(context); // إغلاق نافذة النجاح
                Navigator.pushReplacement( // العودة إلى صفحة تسجيل الدخول
                  context,
                  MaterialPageRoute(builder: (_) => LoginPage()),
                );
              },
              style: TextButton.styleFrom(
                foregroundColor: darkBlue,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              child: Text('حسناً',
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

  void _togglePasswordVisibility() {
    setState(() {
      _obscurePassword = !_obscurePassword;
    });
  }

  void _toggleConfirmPasswordVisibility() {
    setState(() {
      _obscureConfirmPassword = !_obscureConfirmPassword;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: lightGray,
        appBar: AppBar(
          title: Text('إنشاء حساب جديد',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontWeight: FontWeight.bold,
            ),
          ),
          backgroundColor: babyBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
          leading: IconButton(
            icon: Icon(Icons.arrow_back),
            onPressed: () => Navigator.pop(context),
          ),
        ),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // رأس الصفحة
                Container(
                  margin: EdgeInsets.only(bottom: 40),
                  child: Column(
                    children: [
                      Icon(
                        Icons.person_add_alt_1,
                        size: 70,
                        color: darkBlue,
                      ),
                      SizedBox(height: 10),
                      Text('إنشاء حساب جديد',
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                          color: darkBlue,
                        ),
                      ),
                      SizedBox(height: 8),
                      Text('املأ النموذج أدناه لإنشاء حسابك',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 14,
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),

                // حقل الكود الوطني/الموظف
                Container(
                  margin: EdgeInsets.only(bottom: 20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text('كود الموظف',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 14,
                          color: darkBlue,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 8),
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black12,
                              blurRadius: 4,
                              offset: Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          controller: _idCtrl,
                          textDirection: TextDirection.rtl,
                          keyboardType: TextInputType.number,
                          decoration: InputDecoration(
                            hintText: 'أدخل كود الموظف',
                            hintStyle: TextStyle(
                              fontFamily: 'Tajawal',
                              color: Colors.grey[400],
                            ),
                            border: InputBorder.none,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 16,
                            ),
                            prefixIcon: Icon(
                              Icons.badge_outlined,
                              color: babyBlue,
                            ),
                          ),
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 16,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                // حقل كلمة المرور
                Container(
                  margin: EdgeInsets.only(bottom: 20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Text('كلمة المرور',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 14,
                              color: darkBlue,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          Text('(6 أحرف على الأقل)',
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 11,
                              color: Colors.grey[500],
                            ),
                          ),
                        ],
                      ),
                      SizedBox(height: 8),
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black12,
                              blurRadius: 4,
                              offset: Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          controller: _passCtrl,
                          textDirection: TextDirection.rtl,
                          obscureText: _obscurePassword,
                          decoration: InputDecoration(
                            hintText: 'أدخل كلمة المرور',
                            hintStyle: TextStyle(
                              fontFamily: 'Tajawal',
                              color: Colors.grey[400],
                            ),
                            border: InputBorder.none,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 16,
                            ),
                            prefixIcon: Icon(
                              Icons.lock_outline,
                              color: babyBlue,
                            ),
                            suffixIcon: IconButton(
                              icon: Icon(
                                _obscurePassword
                                    ? Icons.visibility_off
                                    : Icons.visibility,
                                color: Colors.grey[500],
                              ),
                              onPressed: _togglePasswordVisibility,
                            ),
                          ),
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 16,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                // حقل تأكيد كلمة المرور
                Container(
                  margin: EdgeInsets.only(bottom: 30),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text('تأكيد كلمة المرور',
                        textDirection: TextDirection.rtl,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 14,
                          color: darkBlue,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      SizedBox(height: 8),
                      Container(
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(12),
                          boxShadow: [
                            BoxShadow(
                              color: Colors.black12,
                              blurRadius: 4,
                              offset: Offset(0, 2),
                            ),
                          ],
                        ),
                        child: TextField(
                          controller: _confirmCtrl,
                          textDirection: TextDirection.rtl,
                          obscureText: _obscureConfirmPassword,
                          decoration: InputDecoration(
                            hintText: 'أعد إدخال كلمة المرور',
                            hintStyle: TextStyle(
                              fontFamily: 'Tajawal',
                              color: Colors.grey[400],
                            ),
                            border: InputBorder.none,
                            contentPadding: EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 16,
                            ),
                            prefixIcon: Icon(
                              Icons.lock_reset_outlined,
                              color: babyBlue,
                            ),
                            suffixIcon: IconButton(
                              icon: Icon(
                                _obscureConfirmPassword
                                    ? Icons.visibility_off
                                    : Icons.visibility,
                                color: Colors.grey[500],
                              ),
                              onPressed: _toggleConfirmPasswordVisibility,
                            ),
                          ),
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 16,
                          ),
                        ),
                      ),
                      SizedBox(height: 8),
                      Row(
                        children: [
                          Icon(
                            Icons.info_outline,
                            size: 14,
                            color: Colors.grey[500],
                          ),
                          SizedBox(width: 6),
                          Expanded(
                            child: Text('تأكد من تطابق كلمتي المرور',
                              style: TextStyle(
                                fontFamily: 'Tajawal',
                                fontSize: 11,
                                color: Colors.grey[500],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                // زر التسجيل
                Container(
                  height: 56,
                  margin: EdgeInsets.only(bottom: 20),
                  child: ElevatedButton(
                    onPressed: loading ? null : submit,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: babyBlue,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      elevation: 4,
                      padding: EdgeInsets.symmetric(vertical: 16),
                    ),
                    child: loading
                        ? SizedBox(
                            height: 24,
                            width: 24,
                            child: CircularProgressIndicator(
                              color: Colors.white,
                              strokeWidth: 2,
                            ),
                          )
                        : Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Icon(Icons.person_add, size: 20),
                              SizedBox(width: 10),
                              Text('إنشاء الحساب',
                                style: TextStyle(
                                  fontFamily: 'Tajawal',
                                  fontSize: 16,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                            ],
                          ),
                  ),
                ),

                // رابط العودة لتسجيل الدخول
                Container(
                  margin: EdgeInsets.only(bottom: 20),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text('لديك حساب بالفعل؟',
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 14,
                          color: Colors.grey[600],
                        ),
                      ),
                      SizedBox(width: 8),
                      TextButton(
                        onPressed: () => Navigator.pushReplacement(
                          context,
                          MaterialPageRoute(
                            builder: (_) => LoginPage(),
                          ),
                        ),
                        style: TextButton.styleFrom(
                          foregroundColor: darkBlue,
                          padding: EdgeInsets.zero,
                        ),
                        child: Text('تسجيل الدخول',
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 14,
                            fontWeight: FontWeight.bold,
                            decoration: TextDecoration.underline,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

                

                // المساحة السفلية
                SizedBox(height: 40),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _idCtrl.dispose();
    _passCtrl.dispose();
    _confirmCtrl.dispose();
    super.dispose();
  }
}