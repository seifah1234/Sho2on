import 'package:flutter/material.dart';
import '../services/permission_service.dart';

class PermissionHistoryPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const PermissionHistoryPage({super.key, required this.user});
  @override
  _PermissionHistoryPageState createState() => _PermissionHistoryPageState();
}

class _PermissionHistoryPageState extends State<PermissionHistoryPage> {
  final PermissionService _permissionService = PermissionService();
  
  List<dynamic> _permissions = [];
  bool _isLoading = false;
  String _selectedStatus = 'All';
  final List<String> _statusOptions = ['All', 'Pending', 'Approved', 'Rejected'];
  
  final Color primaryColor = Color(0xFF1976D2);
  
  @override
  void initState() {
    super.initState();
    _loadPermissions();
  }
  
  Future<void> _loadPermissions() async {
    setState(() => _isLoading = true);
    
    try {
      final result = await _permissionService.getEmployeePermissions(
        widget.user['id'],
        status: _selectedStatus == 'All' ? null : _selectedStatus,
      );
      
      if (result['success']) {
        setState(() {
          _permissions = result['data'] ?? [];
        });
      }
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'خطأ في تحميل السلف: $e',
            textDirection: TextDirection.rtl,
          ),
          backgroundColor: Colors.red,
        ),
      );
    } finally {
      setState(() => _isLoading = false);
    }
  }
  
  Color _getStatusColor(String status) {
    switch (status) {
      case 'Approved':
      case 'Paid':
        return Colors.green;
      case 'Pending':
        return Colors.orange;
      case 'Rejected':
        return Colors.red;
      default:
        return Colors.grey;
    }
  }
  
  String _getStatusText(String status) {
    switch (status) {
      case 'Pending':
        return 'قيد الانتظار';
      case 'Approved':
        return 'موافق';
      case 'Rejected':
        return 'مرفوض';
      default:
        return status;
    }
  }

  Widget _buildPermissionCard(Map<String, dynamic> permission) {
    return Card(
      elevation: 2,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
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
                Chip(
                  label: Text(
                    _getStatusText(permission['status']),
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  backgroundColor: _getStatusColor(permission['status']),
                ),
                Text(
                  permission['permissionNumber'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontSize: 16,
                  ),
                ),
              ],
            ),
            
            SizedBox(height: 8),

            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'من: ${DateTime.parse(permission['startDateTime']).toString().substring(0, 16)}',
                  textDirection: TextDirection.rtl,
                ),
                Text(
                  'إلى: ${DateTime.parse(permission['endDateTime']).toString().substring(0, 16)}',
                  textDirection: TextDirection.rtl,
                ),
              ],
            ),

            SizedBox(height: 8),

            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'عدد ساعات: ${permission['duration'].toStringAsFixed(2)} ساعات',
                  textDirection: TextDirection.rtl,
                ),
                Text(
                  'قيمة الخصم: ${permission['deductedAmount']}',
                  textDirection: TextDirection.rtl,
                ),
              ],
            ),
            
            SizedBox(height: 8),
            
            Divider(),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'تاريخ الطلب: ${permission['createdAt'].toString().substring(0, 10)}',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(fontSize: 12, color: Colors.grey),
                ),
                if (permission['approvedByName'] != null)
                  Text(
                    'المدير: ${permission['approvedByName']}',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
              ],
            ),

            SizedBox(height: 8),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'نوع الاذون: ${permission['permissionType']}',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                ),

                if (permission['reason'] != null && permission['reason'].isNotEmpty) ...[
                  Text(
                    'السبب: ${permission['reason']}',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ],
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
        appBar: AppBar(
          title: Text('سجل الاذونات'),
          backgroundColor: primaryColor,
          foregroundColor: Colors.white,
        ),
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16.0),
              child: Row(
                textDirection: TextDirection.rtl,
                children: [
                  Expanded(
                    child: DropdownButtonFormField<String>(
                      initialValue: _selectedStatus,
                      items: _statusOptions.map((status) {
                        return DropdownMenuItem<String>(
                          value: status,
                          child: Text(
                            status == 'All' ? 'الكل' : _getStatusText(status),
                            textDirection: TextDirection.rtl,
                          ),
                        );
                      }).toList(),
                      onChanged: (value) {
                        setState(() {
                          _selectedStatus = value!;
                          _loadPermissions();
                        });
                      },
                      decoration: InputDecoration(
                        labelText: 'تصفية حسب الحالة',
                        border: OutlineInputBorder(),
                      ),
                    ),
                  ),
                  SizedBox(width: 10),
                  IconButton(
                    icon: Icon(Icons.refresh),
                    onPressed: _loadPermissions,
                    tooltip: 'تحديث',
                  ),
                ],
              ),
            ),
            
            Expanded(
              child: _isLoading
                  ? Center(child: CircularProgressIndicator())
                  : _permissions.isEmpty
                      ? Center(
                          child: Text(
                            'لا توجد اذونات مسجلة',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(fontSize: 18, color: Colors.grey),
                          ),
                        )
                      : ListView.builder(
                          padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          itemCount: _permissions.length,
                          itemBuilder: (context, index) {
                            return Padding(
                              padding: const EdgeInsets.only(bottom: 8.0),
                              child: _buildPermissionCard(_permissions[index]),
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }
}