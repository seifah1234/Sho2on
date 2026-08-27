import 'package:flutter/material.dart';
import '../services/announcement_service.dart';

class AnnouncementsPage extends StatefulWidget {
  final Map user;
  const AnnouncementsPage({super.key, required this.user});

  @override
  _AnnouncementsPageState createState() => _AnnouncementsPageState();
}

class _AnnouncementsPageState extends State<AnnouncementsPage> {
  final AnnouncementService _announcementService = AnnouncementService();

  List<dynamic> _announcements = [];
  List<dynamic> _types = [];
  bool _isLoading = true;
  bool _isManager = false;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color errorColor = Color(0xFFEF4444);
  final Color successColor = Color(0xFF10B981);

  @override
  void initState() {
    super.initState();
    _isManager = widget.user['isManager'] ?? false;
    _loadData();
  }

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      await Future.wait([_loadAnnouncements(), _loadTypes()]);
    } catch (e) {
      _showSnackBar('فشل تحميل البيانات', errorColor);
    } finally {
      setState(() => _isLoading = false);
    }
  }

  Future<void> _loadAnnouncements() async {
    final result = await _announcementService.getAnnouncements();
    if (result['success'] && mounted) {
      setState(() => _announcements = result['data']);
    }
  }

  Future<void> _loadTypes() async {
    final result = await _announcementService.getAnnouncementTypes();
    if (result['success'] && mounted) {
      setState(() => _types = result['data']);
    }
  }

  void _showSnackBar(String message, Color color) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  String _getTypeName(dynamic typeId) {
    for (var type in _types) {
      if (type['id'] == typeId) return type['name'] ?? 'عام';
    }
    return 'عام';
  }

  Color _getTypeColor(dynamic typeId) {
    final typeName = _getTypeName(typeId);
    switch (typeName) {
      case 'عاجل':
        return errorColor;
      case 'مهم':
        return Color(0xFFF59E0B);
      case 'اجتماع':
        return primaryBlue;
      case 'تحديث':
        return successColor;
      default:
        return Color(0xFF6B7280); // رمادي
    }
  }

  Color _hexToColor(String hex) {
    hex = hex.replaceAll('#', ''); // إزالة # إذا وجدت

    // إذا كان الطول 6 (بدون Alpha) أضف FF
    if (hex.length == 6) {
      hex = 'FF$hex';
    }

    return Color(int.parse(hex, radix: 16));
  }

  IconData _getTypeIcon(dynamic typeId) {
    final typeName = _getTypeName(typeId);
    switch (typeName) {
      case 'عاجل':
        return Icons.warning_amber;
      case 'مهم':
        return Icons.star;
      case 'اجتماع':
        return Icons.people;
      case 'تحديث':
        return Icons.refresh;
      default:
        return Icons.campaign;
    }
  }

  String _formatDate(dynamic date) {
    if (date == null) return '';
    try {
      final dt = DateTime.parse(date.toString());
      return '${dt.day}/${dt.month}/${dt.year}';
    } catch (e) {
      return '';
    }
  }

  void _showCreateDialog() {
    showDialog(
      context: context,
      builder: (context) => _CreateAnnouncementDialog(
        types: _types,
        userId: widget.user['id'],
        announcementService: _announcementService,
        onCreated: () {
          _loadAnnouncements();
          _showSnackBar('تم إنشاء الإعلان بنجاح', successColor);
        },
      ),
    );
  }

  void _showDeleteDialog(dynamic announcement) {
    showDialog(
      context: context,
      builder: (context) => Directionality(
        textDirection: TextDirection.rtl,
        child: AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          title: Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              Text(
                'تأكيد الحذف',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontWeight: FontWeight.bold,
                  color: errorColor,
                ),
              ),
              SizedBox(width: 10),
              Icon(Icons.delete_outline, color: errorColor),
            ],
          ),
          content: Text(
            'هل تريد حذف هذا الإعلان؟',
            style: TextStyle(fontFamily: 'Tajawal'),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: Text('إلغاء', style: TextStyle(fontFamily: 'Tajawal')),
            ),
            ElevatedButton(
              onPressed: () async {
                Navigator.pop(context);
                final result = await _announcementService.deleteAnnouncement(
                  announcement['id'],
                );
                if (result['success']) {
                  _showSnackBar('تم حذف الإعلان', successColor);
                  _loadAnnouncements();
                } else {
                  _showSnackBar(result['message'] ?? 'فشل الحذف', errorColor);
                }
              },
              style: ElevatedButton.styleFrom(backgroundColor: errorColor),
              child: Text('حذف', style: TextStyle(fontFamily: 'Tajawal')),
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
        appBar: AppBar(
          title: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'الإعلانات',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
              if (_announcements.isNotEmpty)
                Text(
                  '${_announcements.length} إعلان',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 11,
                    color: Colors.white.withValues(alpha: 0.8),
                  ),
                ),
            ],
          ),
          backgroundColor: primaryBlue,
          foregroundColor: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.vertical(bottom: Radius.circular(20)),
          ),
          actions: [
            IconButton(icon: Icon(Icons.refresh), onPressed: _loadData),
          ],
        ),
        body: _isLoading
            ? Center(child: CircularProgressIndicator(color: primaryBlue))
            : _announcements.isEmpty
            ? _buildEmptyState()
            : RefreshIndicator(
                onRefresh: _loadData,
                color: primaryBlue,
                child: ListView.separated(
                  padding: EdgeInsets.all(16),
                  itemCount: _announcements.length,
                  separatorBuilder: (context, i) => SizedBox(height: 12),
                  itemBuilder: (context, i) =>
                      _buildAnnouncementCard(_announcements[i]),
                ),
              ),
        floatingActionButton: _isManager
            ? FloatingActionButton(
                onPressed: _showCreateDialog,
                backgroundColor: primaryBlue,
                child: Icon(Icons.add, color: Colors.white),
              )
            : null,
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
            child: Icon(Icons.campaign, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'لا توجد إعلانات',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: Colors.grey[700],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAnnouncementCard(dynamic announcement) {
    print('Announcement: $announcement');
    final typeId = announcement['announcementTypeId'];
    final createdByName = announcement['createdByName'];

    final color = _hexToColor(announcement['color'] ?? '#6B7280');
    final icon = _getTypeIcon(typeId);
    final typeName = _getTypeName(typeId);

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.04),
            blurRadius: 8,
            offset: Offset(0, 2),
          ),
        ],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 40,
                  height: 40,
                  decoration: BoxDecoration(
                    color: color,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Icon(icon, color: Colors.white, size: 20),
                ),
                SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        (createdByName ?? '') +
                            " : " +
                            (announcement['title'] ?? ''),
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 15,
                          fontWeight: FontWeight.bold,
                          color: Colors.grey[800],
                        ),
                      ),
                      SizedBox(height: 2),
                      Text(
                        _formatDate(announcement['createdAt']),
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 11,
                          color: Colors.grey[400],
                        ),
                      ),
                    ],
                  ),
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: color.withValues(alpha: 0.3)),
                  ),
                  child: Text(
                    typeName,
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 10,
                      fontWeight: FontWeight.bold,
                      color: color,
                    ),
                  ),
                ),
                if (_isManager) ...[
                  SizedBox(width: 8),
                  GestureDetector(
                    onTap: () => _showDeleteDialog(announcement),
                    child: Container(
                      padding: EdgeInsets.all(6),
                      decoration: BoxDecoration(
                        color: errorColor.withValues(alpha: 0.1),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(
                        Icons.delete_outline,
                        size: 16,
                        color: errorColor,
                      ),
                    ),
                  ),
                ],
              ],
            ),
            SizedBox(height: 12),
            Divider(height: 1, color: Color(0xFFE5E7EB)),
            SizedBox(height: 12),
            Text(
              announcement['content'] ?? announcement['description'] ?? '',
              style: TextStyle(
                fontFamily: 'Tajawal',
                fontSize: 13,
                color: Colors.grey[600],
                height: 1.6,
              ),
            ),
            if (announcement['expireDate'] != null) ...[
              SizedBox(height: 10),
              Row(
                children: [
                  Icon(Icons.schedule, size: 13, color: Colors.grey[400]),
                  SizedBox(width: 4),
                  Text(
                    'ينتهي في: ${_formatDate(announcement['expireDate'])}',
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 11,
                      color: Colors.grey[400],
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

// ==================== Dialog إنشاء إعلان ====================
class _CreateAnnouncementDialog extends StatefulWidget {
  final List<dynamic> types;
  final int userId;
  final AnnouncementService announcementService;
  final VoidCallback onCreated;

  const _CreateAnnouncementDialog({
    required this.types,
    required this.userId,
    required this.announcementService,
    required this.onCreated,
  });

  @override
  _CreateAnnouncementDialogState createState() =>
      _CreateAnnouncementDialogState();
}

class _CreateAnnouncementDialogState extends State<_CreateAnnouncementDialog> {
  final _titleController = TextEditingController();
  final _contentController = TextEditingController();
  int? _selectedTypeId;
  DateTime? _expireDate;
  bool _isSubmitting = false;

  @override
  void dispose() {
    _titleController.dispose();
    _contentController.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_titleController.text.trim().isEmpty) {
      _showError('أدخل العنوان');
      return;
    }
    if (_contentController.text.trim().isEmpty) {
      _showError('أدخل المحتوى');
      return;
    }
    if (_selectedTypeId == null) {
      _showError('اختر نوع الإعلان');
      return;
    }

    setState(() => _isSubmitting = true);

    final result = await widget.announcementService.createAnnouncement(
      title: _titleController.text.trim(),
      content: _contentController.text.trim(),
      typeId: _selectedTypeId!,
      createdByUserId: widget.userId,
      expireDate: _expireDate,
    );

    if (mounted) {
      setState(() => _isSubmitting = false);
      if (result['success']) {
        Navigator.pop(context);
        widget.onCreated();
      } else {
        _showError(result['message'] ?? 'فشل الإنشاء');
      }
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(
          message,
          textDirection: TextDirection.rtl,
          style: TextStyle(fontFamily: 'Tajawal'),
        ),
        backgroundColor: Color(0xFFEF4444),
        behavior: SnackBarBehavior.floating,
        margin: EdgeInsets.all(16),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Dialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        child: Padding(
          padding: EdgeInsets.all(20),
          child: SingleChildScrollView(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'إعلان جديد',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: Color(0xFF1E40AF),
                  ),
                ),
                SizedBox(height: 16),
                // نوع الإعلان
                Text(
                  'نوع الإعلان *',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                SizedBox(height: 8),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 12),
                  decoration: BoxDecoration(
                    border: Border.all(color: Color(0xFFE5E7EB)),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: DropdownButton<int>(
                    value: _selectedTypeId,
                    isExpanded: true,
                    underline: SizedBox(),
                    hint: Text(
                      'اختر النوع',
                      style: TextStyle(fontFamily: 'Tajawal'),
                    ),
                    items: widget.types.map((type) {
                      return DropdownMenuItem<int>(
                        value: type['id'],
                        child: Text(
                          type['name'] ?? '',
                          style: TextStyle(fontFamily: 'Tajawal'),
                        ),
                      );
                    }).toList(),
                    onChanged: (value) =>
                        setState(() => _selectedTypeId = value),
                  ),
                ),
                SizedBox(height: 16),
                // العنوان
                Text(
                  'العنوان *',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                SizedBox(height: 8),
                TextField(
                  controller: _titleController,
                  decoration: InputDecoration(
                    hintText: 'عنوان الإعلان',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
                SizedBox(height: 16),
                // المحتوى
                Text(
                  'المحتوى *',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                SizedBox(height: 8),
                TextField(
                  controller: _contentController,
                  maxLines: 4,
                  decoration: InputDecoration(
                    hintText: 'محتوى الإعلان...',
                    border: OutlineInputBorder(
                      borderRadius: BorderRadius.circular(10),
                    ),
                  ),
                ),
                SizedBox(height: 16),
                // تاريخ الانتهاء
                Text(
                  'تاريخ الانتهاء (اختياري)',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 13,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                SizedBox(height: 8),
                GestureDetector(
                  onTap: () async {
                    final date = await showDatePicker(
                      context: context,
                      initialDate: DateTime.now(),
                      firstDate: DateTime.now(),
                      lastDate: DateTime.now().add(Duration(days: 365)),
                    );
                    if (date != null) setState(() => _expireDate = date);
                  },
                  child: Container(
                    padding: EdgeInsets.symmetric(horizontal: 12, vertical: 14),
                    decoration: BoxDecoration(
                      border: Border.all(color: Color(0xFFE5E7EB)),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          Icons.calendar_today,
                          size: 16,
                          color: Color(0xFF2563EB),
                        ),
                        SizedBox(width: 8),
                        Text(
                          _expireDate != null
                              ? '${_expireDate!.day}/${_expireDate!.month}/${_expireDate!.year}'
                              : 'اختر التاريخ',
                          style: TextStyle(fontFamily: 'Tajawal', fontSize: 13),
                        ),
                      ],
                    ),
                  ),
                ),
                SizedBox(height: 24),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => Navigator.pop(context),
                        style: OutlinedButton.styleFrom(
                          padding: EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                        child: Text(
                          'إلغاء',
                          style: TextStyle(fontFamily: 'Tajawal'),
                        ),
                      ),
                    ),
                    SizedBox(width: 10),
                    Expanded(
                      child: ElevatedButton(
                        onPressed: _isSubmitting ? null : _submit,
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Color(0xFF2563EB),
                          padding: EdgeInsets.symmetric(vertical: 14),
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                        child: _isSubmitting
                            ? SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  color: Colors.white,
                                  strokeWidth: 2,
                                ),
                              )
                            : Text(
                                'نشر',
                                style: TextStyle(
                                  fontFamily: 'Tajawal',
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
