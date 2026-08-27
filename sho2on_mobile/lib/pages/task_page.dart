import 'package:flutter/material.dart';
import '../services/task_service.dart';

class TasksPage extends StatefulWidget {
  final Map user;
  const TasksPage({super.key, required this.user});

  @override
  _TasksPageState createState() => _TasksPageState();
}

class _TasksPageState extends State<TasksPage> {
  final TaskService _taskService = TaskService();
  
  List<dynamic> _tasks = [];
  bool _isLoading = true;
  String _activeTab = 'assigned-to-me';
  dynamic _taskToDelete; // المهمة المراد حذفها

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color completedColor = Color(0xFF10B981);
  final Color inProgressColor = Color(0xFFF59E0B);
  final Color onHoldColor = Color(0xFFEF4444);
  final Color sentColor = Color(0xFF64748B);
  final Color receivedColor = Color(0xFF3B82F6);

  @override
  void initState() {
    super.initState();
    _loadTasks();
  }

  Future<void> _loadTasks() async {
    setState(() => _isLoading = true);
    
    try {
      final result = _activeTab == 'assigned-to-me'
          ? await _taskService.getAssignedToMe(widget.user['id'])
          : await _taskService.getAssignedByMe(widget.user['id']);
      
      if (result['success'] && mounted) {
        setState(() {
          _tasks = result['data'] ?? [];
        });
      }
    } catch (e) {
      if (mounted) {
        _showSnackBar('خطأ في تحميل المهام', Colors.red);
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  void _showSnackBar(String message, Color color) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message, textDirection: TextDirection.rtl, style: TextStyle(fontFamily: 'Tajawal')),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  // ✅ دالة طلب الحذف مع تأكيد
  Future<void> _requestDelete(dynamic task) async {
    showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          backgroundColor: Colors.white,
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'تأكيد الحذف',
                style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold, color: onHoldColor),
              ),
              SizedBox(width: 10),
              Container(
                padding: EdgeInsets.all(8),
                decoration: BoxDecoration(color: onHoldColor.withValues(alpha: 0.1), shape: BoxShape.circle),
                child: Icon(Icons.delete_outline, color: onHoldColor, size: 20),
              ),
            ],
          ),
          content: Text(
            'هل تريد حذف هذه المهمة؟',
            textDirection: TextDirection.rtl,
            style: TextStyle(fontFamily: 'Tajawal', fontSize: 14),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              style: TextButton.styleFrom(foregroundColor: Colors.grey[600]),
              child: Text('إلغاء', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
            ElevatedButton(
              onPressed: () {
                Navigator.pop(context);
                _confirmDelete(task);
              },
              style: ElevatedButton.styleFrom(
                backgroundColor: onHoldColor,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: Text('حذف', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  // ✅ دالة تأكيد الحذف
  Future<void> _confirmDelete(dynamic task) async {
    try {
      final result = await _taskService.deleteTask(task['id']);
      if (result['success'] && mounted) {
        setState(() {
          _tasks.removeWhere((t) => t['id'] == task['id']);
        });
        _showSnackBar('تم حذف المهمة بنجاح', completedColor);
      } else {
        _showSnackBar(result['message'] ?? 'فشل حذف المهمة', onHoldColor);
      }
    } catch (e) {
      _showSnackBar('خطأ في حذف المهمة', onHoldColor);
    }
  }

  String _getStatusText(dynamic status) {
    switch (status) {
      case 4: return 'مُرسلة';
      case 0: return 'مستلمة';
      case 1: return 'معلّقة';
      case 2: return 'جارية';
      case 3: return 'مكتملة';
      default: return 'غير معروف';
    }
  }

  Color _getStatusColor(dynamic status) {
    switch (status) {
      case 4: return sentColor;
      case 0: return receivedColor;
      case 1: return onHoldColor;
      case 2: return inProgressColor;
      case 3: return completedColor;
      default: return Colors.grey;
    }
  }

  IconData _getStatusIcon(dynamic status) {
    switch (status) {
      case 4: return Icons.send;
      case 0: return Icons.download_done;
      case 1: return Icons.pause_circle;
      case 2: return Icons.play_circle;
      case 3: return Icons.check_circle;
      default: return Icons.help;
    }
  }

  String _getTypeText(dynamic type) {
    return type == 2 ? 'طلب' : 'مهمة';
  }

  String _formatDate(dynamic date) {
    if (date == null) return '—';
    try {
      final dt = DateTime.parse(date.toString());
      return '${dt.day}/${dt.month}/${dt.year}';
    } catch (e) {
      return date.toString();
    }
  }

  bool _isOverdue(dynamic dueDate) {
    if (dueDate == null) return false;
    try {
      final dt = DateTime.parse(dueDate.toString());
      return dt.isBefore(DateTime.now());
    } catch (e) {
      return false;
    }
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
              Text('المهام', style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white)),
              if (_tasks.isNotEmpty)
                Text('${_tasks.length} مهمة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8))),
            ],
          ),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.vertical(bottom: Radius.circular(20))),
        ),
        body: Column(
          children: [
            // Tabs
            Container(
              padding: EdgeInsets.all(8),
              margin: EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: cardColor,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Color(0xFFE5E7EB)),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: GestureDetector(
                      onTap: () {
                        setState(() => _activeTab = 'assigned-to-me');
                        _loadTasks();
                      },
                      child: Container(
                        padding: EdgeInsets.symmetric(vertical: 12),
                        decoration: BoxDecoration(
                          color: _activeTab == 'assigned-to-me' ? primaryBlue : Colors.transparent,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          'مهامي',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontWeight: FontWeight.bold,
                            color: _activeTab == 'assigned-to-me' ? Colors.white : Colors.grey[600],
                          ),
                        ),
                      ),
                    ),
                  ),
                  Expanded(
                    child: GestureDetector(
                      onTap: () {
                        setState(() => _activeTab = 'assigned-by-me');
                        _loadTasks();
                      },
                      child: Container(
                        padding: EdgeInsets.symmetric(vertical: 12),
                        decoration: BoxDecoration(
                          color: _activeTab == 'assigned-by-me' ? primaryBlue : Colors.transparent,
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          'مهام كلّفت بها',
                          textAlign: TextAlign.center,
                          style: TextStyle(
                            fontFamily: 'Tajawal',
                            fontWeight: FontWeight.bold,
                            color: _activeTab == 'assigned-by-me' ? Colors.white : Colors.grey[600],
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),

            // Tasks list
            Expanded(
              child: _isLoading
                  ? Center(child: CircularProgressIndicator(color: primaryBlue))
                  : _tasks.isEmpty
                      ? _buildEmptyState()
                      : RefreshIndicator(
                          onRefresh: _loadTasks,
                          color: primaryBlue,
                          child: ListView.separated(
                            padding: EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                            itemCount: _tasks.length,
                            separatorBuilder: (context, i) => SizedBox(height: 10),
                            itemBuilder: (context, index) {
                              return _buildTaskCard(_tasks[index]);
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
            child: Icon(Icons.task_alt, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text('لا توجد مهام', style: TextStyle(fontFamily: 'Tajawal', fontSize: 16, fontWeight: FontWeight.bold, color: Colors.grey[700])),
        ],
      ),
    );
  }

  Widget _buildTaskCard(dynamic task) {
    final status = task['status'] ?? 0;
    final statusColor = _getStatusColor(status);
    final statusIcon = _getStatusIcon(status);
    final statusText = _getStatusText(status);
    final type = task['type'] ?? 0;
    final typeText = _getTypeText(type);
    final isOverdue = _isOverdue(task['dueDate']);

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2))],
        border: Border.all(color: isOverdue ? onHoldColor.withValues(alpha: 0.3) : Color(0xFFE5E7EB)),
      ),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: type == 1 ? Colors.purple.withValues(alpha: 0.1) : lightBlue,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text(
                    typeText,
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: type == 1 ? Colors.purple : primaryBlue,
                    ),
                  ),
                ),
                Spacer(),
                // ✅ زر الحذف - يظهر فقط في "مهام كلّفت بها"
                if (_activeTab == 'assigned-by-me')
                  GestureDetector(
                    onTap: () => _requestDelete(task),
                    child: Container(
                      padding: EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: onHoldColor.withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(Icons.delete_outline, size: 18, color: onHoldColor),
                    ),
                  ),
                SizedBox(width: 8),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: statusColor.withValues(alpha: 0.3)),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(statusIcon, size: 12, color: statusColor),
                      SizedBox(width: 4),
                      Text(
                        statusText,
                        style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, fontWeight: FontWeight.bold, color: statusColor),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            SizedBox(height: 12),
            Text(
              task['description'] ?? '',
              style: TextStyle(fontFamily: 'Tajawal', fontSize: 14, color: Colors.grey[800]),
            ),
            SizedBox(height: 12),
            Divider(height: 1, color: Color(0xFFE5E7EB)),
            SizedBox(height: 10),
            Row(
              children: [
                Icon(Icons.person_outline, size: 14, color: Colors.grey[400]),
                SizedBox(width: 4),
                Text(
                  _activeTab == 'assigned-to-me'
                      ? 'من: ${task['assignedBy'] ?? ''}'
                      : 'إلى: ${task['assignedBy'] ?? ''}',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.grey[500]),
                ),
                Spacer(),
                if (task['dueDate'] != null) ...[
                  Icon(
                    Icons.calendar_today,
                    size: 13,
                    color: isOverdue ? onHoldColor : Colors.grey[400],
                  ),
                  SizedBox(width: 4),
                  Text(
                    _formatDate(task['dueDate']),
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 11,
                      color: isOverdue ? onHoldColor : Colors.grey[500],
                      fontWeight: isOverdue ? FontWeight.bold : FontWeight.normal,
                    ),
                  ),
                  if (isOverdue) ...[
                    SizedBox(width: 4),
                    Text(
                      '(متأخرة)',
                      style: TextStyle(fontFamily: 'Tajawal', fontSize: 10, color: onHoldColor, fontWeight: FontWeight.bold),
                    ),
                  ],
                ],
              ],
            ),
            if (_activeTab == 'assigned-to-me') ...[
              SizedBox(height: 12),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 12),
                decoration: BoxDecoration(
                  color: Color(0xFFF8FAFC),
                  borderRadius: BorderRadius.circular(10),
                  border: Border.all(color: Color(0xFFE5E7EB)),
                ),
                child: DropdownButtonHideUnderline(
                  child: DropdownButton<int>(
                    value: status as int?,
                    isExpanded: true,
                    dropdownColor: Colors.white,
                    icon: Icon(Icons.arrow_drop_down, color: primaryBlue),
                    items: [
                      DropdownMenuItem(value: 0, child: Text('مستلمة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                      DropdownMenuItem(value: 4, child: Text('مُرسلة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                      DropdownMenuItem(value: 1, child: Text('معلّقة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                      DropdownMenuItem(value: 2, child: Text('جارية', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                      DropdownMenuItem(value: 3, child: Text('مكتملة', style: TextStyle(fontFamily: 'Tajawal', fontSize: 12))),
                    ],
                    onChanged: (newStatus) async {
                      if (newStatus != null) {
                        final result = await _taskService.updateStatus(task['id'], newStatus);
                        if (result['success'] && mounted) {
                          setState(() {
                            task['status'] = newStatus;
                          });
                          _showSnackBar('تم تحديث الحالة', completedColor);
                        }
                      }
                    },
                  ),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}