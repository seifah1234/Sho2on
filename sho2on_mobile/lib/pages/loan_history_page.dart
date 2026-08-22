import 'package:flutter/material.dart';
import '../services/loan_service.dart';

class LoanHistoryPage extends StatefulWidget {
  final Map<dynamic, dynamic> user;
  const LoanHistoryPage({super.key, required this.user});

  @override
  _LoanHistoryPageState createState() => _LoanHistoryPageState();
}

class _LoanHistoryPageState extends State<LoanHistoryPage> {
  final LoanService _loanService = LoanService();
  
  List<dynamic> _loans = [];
  bool _isLoading = false;
  String _selectedStatus = 'All';
  final List<String> _statusOptions = ['All', 'Pending', 'Approved', 'Rejected', 'Paid', 'PartiallyPaid'];
  
  final Color primaryColor = Color(0xFF1976D2);
  
  @override
  void initState() {
    super.initState();
    _loadLoans();
  }
  
  Future<void> _loadLoans() async {
    setState(() => _isLoading = true);
    
    try {
      final result = await _loanService.getEmployeeLoans(
        widget.user['id'],
        status: _selectedStatus == 'All' ? null : _selectedStatus,
      );
      
      if (result['success']) {
        setState(() {
          _loans = result['data'] ?? [];
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
      case 'PartiallyPaid':
        return Colors.blue;
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
      case 'Paid':
        return 'مسدد بالكامل';
      case 'PartiallyPaid':
        return 'مسدد جزئياً';
      default:
        return status;
    }
  }
  
  Widget _buildLoanCard(Map<String, dynamic> loan) {
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
                    _getStatusText(loan['status']),
                    style: TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  backgroundColor: _getStatusColor(loan['status']),
                ),
                Text(
                  loan['loanNumber'] ?? '',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: primaryColor,
                    fontSize: 16,
                  ),
                ),
              ],
            ),
            
            SizedBox(height: 10),
            
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              textDirection: TextDirection.rtl,
              children: [
                Text(
                  'المبلغ: ${loan['loanAmount'].toStringAsFixed(2)} جنيه',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(fontWeight: FontWeight.bold),
                ),
                Text(
                  'المتبقي: ${loan['remainingAmount'].toStringAsFixed(2)} جنيه',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: loan['remainingAmount'] > 0 ? Colors.red : Colors.green,
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
                  'القسط الشهري: ${loan['monthlyInstallment'].toStringAsFixed(2)} جنيه',
                  textDirection: TextDirection.rtl,
                ),
                Text(
                  'عدد الأقساط: ${loan['installmentCount']}',
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
                  'تاريخ الطلب: ${loan['loanDate'].toString().substring(0, 10)}',
                  textDirection: TextDirection.rtl,
                  style: TextStyle(fontSize: 12, color: Colors.grey),
                ),
                if (loan['approvedByName'] != null)
                  Text(
                    'المدير: ${loan['approvedByName']}',
                    textDirection: TextDirection.rtl,
                    style: TextStyle(fontSize: 12, color: Colors.grey),
                  ),
              ],
            ),
            
            if (loan['reason'] != null && loan['reason'].isNotEmpty) ...[
              SizedBox(height: 8),
              Text(
                'السبب: ${loan['reason']}',
                textDirection: TextDirection.rtl,
                style: TextStyle(fontSize: 12, color: Colors.grey[600]),
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
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
          title: Text('سجل السلف'),
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
                          _loadLoans();
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
                    onPressed: _loadLoans,
                    tooltip: 'تحديث',
                  ),
                ],
              ),
            ),
            
            Expanded(
              child: _isLoading
                  ? Center(child: CircularProgressIndicator())
                  : _loans.isEmpty
                      ? Center(
                          child: Text(
                            'لا توجد سلف مسجلة',
                            textDirection: TextDirection.rtl,
                            style: TextStyle(fontSize: 18, color: Colors.grey),
                          ),
                        )
                      : ListView.builder(
                          padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                          itemCount: _loans.length,
                          itemBuilder: (context, index) {
                            return Padding(
                              padding: const EdgeInsets.only(bottom: 8.0),
                              child: _buildLoanCard(_loans[index]),
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