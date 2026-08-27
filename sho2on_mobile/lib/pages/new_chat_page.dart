import 'package:flutter/material.dart';
import '../services/chat_history_service.dart';
import 'chat_thread_page.dart';

class NewChatPage extends StatefulWidget {
  final Map user;
  final String chatToken;
  const NewChatPage({super.key, required this.user, required this.chatToken});

  @override
  State<NewChatPage> createState() => _NewChatPageState();
}

class _NewChatPageState extends State<NewChatPage> {
  final ChatHistoryService _service = ChatHistoryService();
  final TextEditingController _searchController = TextEditingController();

  List<dynamic> _users = [];
  List<dynamic> _filteredUsers = [];
  bool _isLoading = true;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color greenColor = Color(0xFF10B981);

  @override
  void initState() {
    super.initState();
    _loadUsers();
  }

  Future<void> _loadUsers() async {
    setState(() => _isLoading = true);
    try {
      final users = await _service.getAllUsers();
      if (mounted) {
        setState(() {
          _users = users.where((u) => u['id'] != widget.user['id']).toList();
          _filteredUsers = List.from(_users);
          _isLoading = false;
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('فشل تحميل المستخدمين', textDirection: TextDirection.rtl),
            backgroundColor: Color(0xFFEF4444),
            behavior: SnackBarBehavior.floating,
            margin: EdgeInsets.all(16),
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
          ),
        );
        setState(() => _isLoading = false);
      }
    }
  }

  void _searchUsers(String query) {
    setState(() {
      if (query.trim().isEmpty) {
        _filteredUsers = List.from(_users);
      } else {
        _filteredUsers = _users.where((u) {
          final name = (u['fullName'] ?? '').toLowerCase();
          final code = (u['code'] ?? '').toLowerCase();
          final department = (u['department'] ?? '').toString().toLowerCase();
          final searchQuery = query.toLowerCase().trim();
          return name.contains(searchQuery) || 
                 code.contains(searchQuery) || 
                 department.contains(searchQuery);
        }).toList();
      }
    });
  }

  void _startChat(Map<String, dynamic> otherUser) {
    Navigator.pushReplacement(
      context,
      MaterialPageRoute(
        builder: (_) => ChatThreadPage(
          user: widget.user,
          chatToken: widget.chatToken,
          otherUserId: otherUser['id'],
          otherUserName: otherUser['fullName'] ?? 'مستخدم',
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
                'محادثة جديدة',
                style: TextStyle(
                  fontFamily: 'Tajawal',
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                  color: Colors.white,
                ),
              ),
              Text(
                'اختر مستخدم للتواصل معه',
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
        ),
        body: Column(
          children: [
            // Search bar
            Padding(
              padding: EdgeInsets.all(16),
              child: Container(
                decoration: BoxDecoration(
                  color: cardColor,
                  borderRadius: BorderRadius.circular(14),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.04),
                      blurRadius: 8,
                      offset: Offset(0, 2),
                    ),
                  ],
                  border: Border.all(color: Color(0xFFE5E7EB)),
                ),
                child: TextField(
                  controller: _searchController,
                  textDirection: TextDirection.rtl,
                  decoration: InputDecoration(
                    hintText: 'ابحث بالاسم أو الكود...',
                    hintStyle: TextStyle(
                      color: Colors.grey[400],
                      fontFamily: 'Tajawal',
                      fontSize: 14,
                    ),
                    prefixIcon: Container(
                      margin: EdgeInsets.all(8),
                      padding: EdgeInsets.all(8),
                      decoration: BoxDecoration(
                        color: lightBlue,
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Icon(Icons.search, color: primaryBlue, size: 20),
                    ),
                    suffixIcon: _searchController.text.isNotEmpty
                        ? IconButton(
                            icon: Icon(Icons.clear, color: Colors.grey[400], size: 20),
                            onPressed: () {
                              _searchController.clear();
                              _searchUsers('');
                            },
                          )
                        : null,
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 14),
                  ),
                  onChanged: _searchUsers,
                ),
              ),
            ),

            // Users count
            if (!_isLoading && _filteredUsers.isNotEmpty)
              Padding(
                padding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                child: Row(
                  children: [
                    Text(
                      '${_filteredUsers.length} مستخدم',
                      style: TextStyle(
                        fontFamily: 'Tajawal',
                        fontSize: 12,
                        color: Colors.grey[500],
                      ),
                    ),
                    Spacer(),
                    if (_filteredUsers.length < _users.length)
                      Text(
                        'من أصل ${_users.length}',
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 11,
                          color: Colors.grey[400],
                        ),
                      ),
                  ],
                ),
              ),

            // Users list
            Expanded(
              child: _isLoading
                  ? Center(
                      child: Column(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          CircularProgressIndicator(color: primaryBlue),
                          SizedBox(height: 16),
                          Text(
                            'جاري تحميل المستخدمين...',
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 13,
                              color: Colors.grey[500],
                            ),
                          ),
                        ],
                      ),
                    )
                  : _filteredUsers.isEmpty
                      ? _buildEmptyState()
                      : ListView.separated(
                          padding: EdgeInsets.all(16),
                          itemCount: _filteredUsers.length,
                          separatorBuilder: (context, i) => SizedBox(height: 8),
                          itemBuilder: (context, i) {
                            final user = _filteredUsers[i];
                            return _buildUserCard(user);
                          },
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
            decoration: BoxDecoration(
              color: lightBlue,
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.person_search, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'لا يوجد مستخدمين',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: Colors.grey[700],
            ),
          ),
          SizedBox(height: 6),
          Text(
            'جرب البحث باسم أو كود مختلف',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 13,
              color: Colors.grey[500],
            ),
          ),
          SizedBox(height: 24),
          OutlinedButton.icon(
            onPressed: () {
              _searchController.clear();
              _searchUsers('');
            },
            style: OutlinedButton.styleFrom(
              foregroundColor: primaryBlue,
              side: BorderSide(color: primaryBlue.withValues(alpha: 0.3)),
              padding: EdgeInsets.symmetric(horizontal: 20, vertical: 12),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            ),
            icon: Icon(Icons.refresh, size: 18),
            label: Text(
              'إعادة التحميل',
              style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildUserCard(dynamic user) {
    final userName = user['fullName'] ?? 'مستخدم';
    final userCode = user['code'] ?? '';
    final userDepartment = user['department'] ?? '';

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(14),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.03),
            blurRadius: 6,
            offset: Offset(0, 2),
          ),
        ],
        border: Border.all(color: Color(0xFFE5E7EB)),
      ),
      child: InkWell(
        onTap: () => _startChat(Map<String, dynamic>.from(user)),
        borderRadius: BorderRadius.circular(14),
        child: Padding(
          padding: EdgeInsets.all(12),
          child: Row(
            children: [
              // Avatar
              Stack(
                children: [
                  Container(
                    width: 48,
                    height: 48,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topRight,
                        end: Alignment.bottomLeft,
                        colors: [primaryBlue, darkBlue],
                      ),
                      shape: BoxShape.circle,
                    ),
                    child: Center(
                      child: Text(
                        userName.isNotEmpty ? userName[0] : '?',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                          fontFamily: 'Tajawal',
                        ),
                      ),
                    ),
                  ),
                  // Online indicator
                  Positioned(
                    bottom: 0,
                    left: 0,
                    child: Container(
                      width: 12,
                      height: 12,
                      decoration: BoxDecoration(
                        color: greenColor,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 2),
                      ),
                    ),
                  ),
                ],
              ),
              SizedBox(width: 12),
              
              // User info
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      userName,
                      style: TextStyle(
                        fontFamily: 'Tajawal',
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                        color: Colors.grey[800],
                      ),
                    ),
                    SizedBox(height: 3),
                    Row(
                      children: [
                        if (userCode.isNotEmpty) ...[
                          Icon(Icons.badge_outlined, size: 13, color: Colors.grey[400]),
                          SizedBox(width: 4),
                          Text(
                            userCode,
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 12,
                              color: Colors.grey[500],
                            ),
                          ),
                        ],
                        if (userDepartment.isNotEmpty) ...[
                          SizedBox(width: 10),
                          Icon(Icons.business_outlined, size: 13, color: Colors.grey[400]),
                          SizedBox(width: 4),
                          Text(
                            userDepartment,
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 12,
                              color: Colors.grey[500],
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              
              // Chat button
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  color: lightBlue,
                  shape: BoxShape.circle,
                ),
                child: Icon(Icons.chat_bubble_outline, color: primaryBlue, size: 20),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }
}