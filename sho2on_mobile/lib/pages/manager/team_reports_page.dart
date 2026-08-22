import 'package:flutter/material.dart';

class TeamReportsPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const TeamReportsPage({super.key, required this.user});

  @override
  _TeamReportsPageState createState() => _TeamReportsPageState();
}

class _TeamReportsPageState extends State<TeamReportsPage> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('تقارير الفريق'),
      ),
      body: Center(
        child: Text('صفحة تقارير الفريق - تحت التطوير'),
      ),
    );
  }
}