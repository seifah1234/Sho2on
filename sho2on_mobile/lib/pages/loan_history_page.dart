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
  List<dynamic> _filteredLoans = [];
  bool _isLoading = false;
  String _selectedStatus = 'All';
  final List<String> _statusOptions = ['All', 'Pending', 'Approved', 'Rejected', 'Paid', 'PartiallyPaid'];

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color pendingColor = Color(0xFFF59E0B);
  final Color approvedColor = Color(0xFF10B981);
  final Color rejectedColor = Color(0xFFEF4444);
  final Color paidColor = Color(0xFF10B981);
  final Color partialColor = Color(0xFF3B82F6);

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

      if (result['success'] && mounted) {
        setState(() {
          _loans = result['data'] ?? [];
          _filteredLoans = List.from(_loans);
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('خطأ في تحميل السلف', textDirection: TextDirection.rtl),
            backgroundColor: rejectedColor,
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.all(16),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Approved':
        return approvedColor;
      case 'Paid':
        return paidColor;
      case 'Pending':
        return pendingColor;
      case 'Rejected':
        return rejectedColor;
      case 'PartiallyPaid':
        return partialColor;
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

  IconData _getStatusIcon(String status) {
    switch (status) {
      case 'Approved':
        return Icons.check_circle;
      case 'Paid':
        return Icons.verified;
      case 'Pending':
        return Icons.hourglass_empty;
      case 'Rejected':
        return Icons.cancel;
      case 'PartiallyPaid':
        return Icons.timelapse;
      default:
        return Icons.help;
    }
  }

  String _formatDate(dynamic date) {
    if (date == null) return '—';
    try {
      final dateTime = DateTime.parse(date.toString());
      return '${dateTime.day}/${dateTime.month}/${dateTime.year}';
    } catch (e) {
      return date.toString();
    }
  }

  String _formatAmount(dynamic amount) {
    if (amount == null) return '0';
    return amount.toStringAsFixed(0);
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
              Text(
                'سجل السلف',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              if (_loans.isNotEmpty)
                Text(
                  '${_loans.length} سلفة',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8)),
                ),
            ],
          ),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(bottom: Radius.circular(20))),
        ),
        body: Column(
          children: [
            // Filter chips
            Container(
              height: 60,
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
              child: ListView(
                scrollDirection: Axis.horizontal,
                reverse: true,
                children: _statusOptions.map((status) {
                  final isSelected = _selectedStatus == status;
                  final label = status == 'All' ? 'الكل' : _getStatusText(status);
                  final color = status == 'All' ? primaryBlue : _getStatusColor(status);

                  return Padding(
                    padding: EdgeInsets.only(left: 8),
                    child: GestureDetector(
                      onTap: () {
                        setState(() => _selectedStatus = status);
                        _loadLoans();
                      },
                      child: Container(
                        padding: EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                        decoration: BoxDecoration(
                          color: isSelected ? color : cardColor,
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(color: isSelected ? color : Color(0xFFE5E7EB)),
                          boxShadow: isSelected
                              ? [BoxShadow(color: color.withValues(alpha: 0.3), blurRadius: 6, offset: Offset(0, 2))]
                              : null,
                        ),
                        child: Text(
                          label,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontSize: 12,
                            fontWeight: FontWeight.bold,
                            color: isSelected ? Colors.white : Colors.grey[600],
                          ),
                        ),
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),

            // Loans list
            Expanded(
              child: _isLoading
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          CircularProgressIndicator(color: primaryBlue),
                          SizedBox(height: 16),
                          Text('جاري تحميل السلف...', style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500])),
                        ],
                      ),
                    )
                  : _loans.isEmpty
                      ? _buildEmptyState()
                      : RefreshIndicator(
                          onRefresh: _loadLoans,
                          color: primaryBlue,
                          child: ListView.separated(
                            padding: EdgeInsets.all(16),
                            itemCount: _loans.length,
                            separatorBuilder: (context, i) => SizedBox(height: 12),
                            itemBuilder: (context, index) {
                              return _buildLoanCard(_loans[index]);
                            },
                          ),
                        ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Container(
            width: 80,
            height: 80,
            decoration: BoxDecoration(color: lightBlue, shape: BoxShape.circle),
            child: Icon(Icons.account_balance_wallet_outlined, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد سلف مسجلة',
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.grey[700]),
          ),
          SizedBox(height: 6),
          Text(
            'لم يتم تسجيل أي سلفة حتى الآن',
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500]),
          ),
        ],
      ),
    );
  }

  Widget _buildLoanCard(Map<String, dynamic> loan) {
    final status = loan['status'] ?? '';
    final statusColor = _getStatusColor(status);
    final statusText = _getStatusText(status);
    final statusIcon = _getStatusIcon(status);
    final loanAmount = loan['loanAmount'] ?? 0;
    final remainingAmount = loan['remainingAmount'] ?? 0;
    final monthlyInstallment = loan['monthlyInstallment'] ?? 0;
    final installmentCount = loan['installmentCount'] ?? 0;
    final progress = loanAmount > 0 ? ((loanAmount - remainingAmount) / loanAmount) : 0.0;

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: Column(
        children: [
          Padding(
            padding: EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Header
                Row(
                  children: [
                    Container(
                      padding: EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: statusColor.withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(statusIcon, color: statusColor, size: 24),
                    ),
                    SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            loan['loanNumber'] ?? 'سلفة',
                            style: TextStyle(fontFamily: 'Tajawal', fontSize: 15, fontWeight: FontWeight.bold, color: darkBlue),
                          ),
                          SizedBox(height: 2),
                          Text(
                            _formatDate(loan['loanDate']),
                            style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[400]),
                          ),
                        ],
                      ),
                    ),
                    Container(
                      padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: statusColor.withValues(alpha: 0.1),
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                      ),
                      child: Text(
                        statusText,
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 11,
                          fontWeight: FontWeight.bold,
                          color: statusColor,
                        ),
                      ),
                    ),
                  ],
                ),

                SizedBox(height: 16),

                // Amounts
                Row(
                  children: [
                    Expanded(
                      child: _buildAmountItem('المبلغ الكلي', loanAmount, darkBlue, Icons.money),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: _buildAmountItem('المتبقي', remainingAmount, remainingAmount > 0 ? rejectedColor : approvedColor, Icons.account_balance_wallet),
                    ),
                  ],
                ),

                SizedBox(height: 10),

                Row(
                  children: [
                    Expanded(
                      child: _buildAmountItem('القسط الشهري', monthlyInstallment, primaryBlue, Icons.calendar_today),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: _buildAmountItem('عدد الأقساط', installmentCount.toDouble(), Colors.grey[600]!, Icons.numbers, showCurrency: false),
                    ),
                  ],
                ),

                // Progress bar
                if (loanAmount > 0) ...[
                  SizedBox(height: 16),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Text('نسبة السداد', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500])),
                          Spacer(),
                          Text(
                            '${(progress * 100).toStringAsFixed(0)}%',
                            style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, fontWeight: FontWeight.bold, color: statusColor),
                          ),
                        ],
                      ),
                      SizedBox(height: 6),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(6),
                        child: LinearProgressIndicator(
                          value: progress.clamp(0.0, 1.0),
                          backgroundColor: Color(0xFFF1F5F9),
                          color: statusColor,
                          minHeight: 8,
                        ),
                      ),
                    ],
                  ),
                ],

                if (loan['reason'] != null && loan['reason'].toString().isNotEmpty) ...[
                  SizedBox(height: 12),
                  Divider(height: 1, color: Color(0xFFE5E7EB)),
                  SizedBox(height: 12),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Icon(Icons.info_outline, size: 16, color: Colors.grey[400]),
                      SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          loan['reason'].toString(),
                          style: TextStyle(fontFamily: 'Tajawal', fontSize: 12, color: Colors.grey[500]),
                          maxLines: 2,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                    ],
                  ),
                ],

                if (loan['approvedByName'] != null) ...[
                  SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.person_outline, size: 14, color: Colors.grey[400]),
                      SizedBox(width: 6),
                      Text(
                        'المدير: ${loan['approvedByName']}',
                        style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500]),
                      ),
                    ],
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAmountItem(String label, dynamic value, Color color, IconData icon, { bool showCurrency = true}) {
    return Container(
      padding: EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Color(0xFFF8FAFC),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 14, color: color),
              SizedBox(width: 4),
              Text(
                label,
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: Colors.grey[500]),
              ),
            ],
          ),
          SizedBox(height: 4),
          Text(
            showCurrency ? '${_formatAmount(value)} ج' : _formatAmount(value),
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, fontWeight: FontWeight.bold, color: color),
          ),
        ],
      ),
    );
  }
}