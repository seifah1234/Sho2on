import 'package:flutter/material.dart';
import '../services/auth_service.dart';
import '../utils/device_helper.dart';
import '../utils/local_storage.dart';
import 'main_page.dart'; // الصفحة القديمة للموظفين العاديين
import 'manager/manager_dashboard.dart'; // الصفحة الجديدة للمديرين
import 'register_page.dart';
import 'package:http/http.dart' as http;

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});
  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final _id = TextEditingController();
  final _pass = TextEditingController();
  final _auth = AuthService();
  bool loading = false;
  bool _obscurePassword = true;

  // ألوان التصميم (متوافقة مع MainPage)
  final Color babyBlue = Color(0xFF89CFF0);
  final Color darkBlue = Color(0xFF1E3A8A);
  final Color lightGray = Color(0xFFF5F7FA);

  // أضف هذه الدالة للاختبار
  Future<void> testConnection() async {
    try {
      final response = await http.get(Uri.parse("http://197.44.171.27:5000"));
      print("Connection test: ${response.statusCode}");
    } catch (e) {
      print("Connection test failed: $e");
    }
  }

  Future<void> doLogin() async {
    if (_id.text.isEmpty || _pass.text.isEmpty) {
      showError('يرجى ملء جميع الحقول');
      return;
    }
    
    setState(() => loading = true);
    final deviceId = await DeviceHelper.getDeviceId();
    final user = await _auth.login(_id.text.trim(), _pass.text, deviceId);
    setState(() => loading = false);

    if (user == null) {
      showError('بيانات الدخول غير صحيحة');
      return;
    }

    await LocalStorage.saveUser(user);
    
    // التحقق من صلاحية المدير وتوجيهه للصفحة المناسبة
    if (user['isManager'] ?? false) {
      // إذا كان مدير، توجهه للـ ManagerDashboard
      Navigator.pushReplacement(
        context, 
        MaterialPageRoute(builder: (_) => ManagerDashboard(user))
      );
    } else {
      // إذا كان موظف عادي، توجهه للـ MainPage القديمة
      Navigator.pushReplacement(
        context, 
        MaterialPageRoute(builder: (_) => MainPage(user))
      );
    }
  }

  void showError(String message) {
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
                  color: Colors.red,
                )
              ),
              SizedBox(width: 10),
              Icon(Icons.error_outline, color: Colors.red),
            ],
          ),
          content: Text(message,
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

  void _togglePasswordVisibility() {
    setState(() {
      _obscurePassword = !_obscurePassword;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: lightGray,
        appBar: AppBar(
          title: Text('تسجيل الدخول',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontWeight: FontWeight.bold,
            ),
          ),
          backgroundColor: babyBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          centerTitle: true,
        ),
        body: SafeArea(
          child: SingleChildScrollView(
            padding: EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // شعار أو صورة ترحيبية
                Container(
                  height: 180,
                  margin: EdgeInsets.only(bottom: 40),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(20),
                    boxShadow: [
                      BoxShadow(
                        color: babyBlue.withValues(alpha: 0.2),
                        blurRadius: 15,
                        offset: Offset(0, 5),
                      ),
                    ],
                  ),
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(
                          Icons.fingerprint,
                          size: 60,
                          color: babyBlue,
                        ),
                        SizedBox(height: 10),
                        Text('نظام البصمة الذكي',
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 18,
                            fontWeight: FontWeight.bold,
                            color: darkBlue,
                          ),
                        ),
                        SizedBox(height: 5),
                        Text('مرحباً بعودتك',
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 14,
                            color: Colors.grey[600],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),

                // حقل الكود
                Container(
                  margin: EdgeInsets.only(bottom: 20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text('الكود',
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
                          controller: _id,
                          textDirection: TextDirection.rtl,
                          keyboardType: TextInputType.number,
                          decoration: InputDecoration(
                            hintText: 'أدخل الكود الخاص بك',
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
                              Icons.person_outline,
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
                  margin: EdgeInsets.only(bottom: 30),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
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
                          controller: _pass,
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

                // زر الدخول
                Container(
                  height: 56,
                  margin: EdgeInsets.only(bottom: 20),
                  child: ElevatedButton(
                    onPressed: loading ? null : doLogin,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: babyBlue,
                      foregroundColor: Colors.white,
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12),
                      ),
                      elevation: 4,
                      shadowColor: babyBlue.withValues(alpha: 0.4),
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
                              Icon(Icons.login, size: 20),
                              SizedBox(width: 10),
                              Text('دخول',
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

                // رابط التسجيل
                Container(
                  margin: EdgeInsets.only(bottom: 20),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      TextButton(
                        onPressed: () => Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => const RegisterPage(),
                          ),
                        ),
                        style: TextButton.styleFrom(
                          foregroundColor: darkBlue,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(8),
                          ),
                        ),
                        child: Row(
                          children: [
                            Icon(Icons.person_add, size: 16),
                            SizedBox(width: 6),
                            Text('التسجيل كمستخدم جديد',
                              style: TextStyle(
                                fontFamily: 'Tajawal',
                                fontSize: 14,
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),

                // معلومات إضافية
                Container(
                  margin: EdgeInsets.only(top: 20),
                  padding: EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(
                      color: babyBlue.withValues(alpha: 0.2),
                      width: 1,
                    ),
                  ),
                  child: Column(
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.admin_panel_settings,
                            size: 16,
                            color: Color(0xFF673AB7),
                          ),
                          SizedBox(width: 8),
                          Text('واجهة مدير متقدمة',
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                          ),
                          SizedBox(width: 20),
                          Icon(
                            Icons.supervised_user_circle,
                            size: 16,
                            color: babyBlue,
                          ),
                          SizedBox(width: 8),
                          Text('واجهة موظف مبسطة',
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 12,
                              color: Colors.grey[600],
                            ),
                          ),
                        ],
                      ),
                      SizedBox(height: 10),
                      Text(
                        'سيتم توجيهك تلقائياً للواجهة المناسبة حسب صلاحياتك',
                        textDirection: TextDirection.rtl,
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 11,
                          color: Colors.grey[500],
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
    _id.dispose();
    _pass.dispose();
    super.dispose();
  }
}