import 'package:flutter/material.dart';
import 'dart:async';
import 'login_page.dart';
import 'main_page.dart';
import 'manager/manager_dashboard.dart';
import '../utils/local_storage.dart';

class SplashPage extends StatefulWidget {
  const SplashPage({super.key});
  @override
  State<SplashPage> createState() => _SplashPageState();
}

class _SplashPageState extends State<SplashPage> {
  @override
  void initState() {
    super.initState();
    Timer(const Duration(milliseconds: 800), checkLogin);
  }

  Future<void> checkLogin() async {
    final userJson = await LocalStorage.getUser();
    if (userJson != null) {
      if (userJson['isManager'] ?? false) {
        Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => ManagerDashboard(userJson)));
        return;
      }
      Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => MainPage(userJson)));
    } else {
      Navigator.pushReplacement(context, MaterialPageRoute(builder: (_) => LoginPage()));
    }
  }

  @override
  Widget build(BuildContext context) {
    return const Scaffold(
      body: Center(child: CircularProgressIndicator()),
    );
  }
}
