import 'dart:convert';

import 'package:flutter/material.dart';
import '../services/chat_history_service.dart';
import 'chat_thread_page.dart';
import 'new_chat_page.dart';

class ChatListPage extends StatefulWidget {
  final Map user;
  final String chatToken;
  const ChatListPage({super.key, required this.user, required this.chatToken});

  @override
  State<ChatListPage> createState() => _ChatListPageState();
}

class _ChatListPageState extends State<ChatListPage> {
  final ChatHistoryService _service = ChatHistoryService();
  List<dynamic> _conversations = [];
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
    _load();
  }

  Future<void> _load() async {
    setState(() => _isLoading = true);
    try {
      final data = await _service.getConversations(widget.user['id']);
      setState(() => _conversations = data);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('$e', textDirection: TextDirection.rtl),
            backgroundColor: Color(0xFFEF4444),
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

  Future<void> _openNewChat() async {
    final result = await Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => NewChatPage(user: widget.user, chatToken: widget.chatToken),
      ),
    );
    if (result == true) {
      _load();
    }
  }

Widget _buildConversationAvatar(dynamic conversation) {
  final profileImage = conversation['profileImageData'] ?? conversation['profileImage'];
  final userName = conversation['username'] ?? '';

  
  if (profileImage != null && profileImage.toString().isNotEmpty) {
    return Container(
      width: 52,
      height: 52,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white, width: 2),
      ),
      child: CircleAvatar(
        radius: 26,
        backgroundImage: MemoryImage(base64Decode(profileImage.toString())),
      ),
    );
  }
  
  return Container(
    width: 52,
    height: 52,
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
        style: TextStyle(color: Colors.white, fontSize: 20, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'),
      ),
    ),
  );
}

  String _formatTime(dynamic time) {
    if (time == null) return '';
    try {
      final dateTime = DateTime.parse(time.toString());
      final now = DateTime.now();
      final today = DateTime(now.year, now.month, now.day);
      final messageDate = DateTime(dateTime.year, dateTime.month, dateTime.day);

      if (messageDate == today) {
        return '${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
      } else if (messageDate == today.subtract(Duration(days: 1))) {
        return 'أمس';
      } else {
        return '${dateTime.day}/${dateTime.month}';
      }
    } catch (e) {
      return '';
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
              Text(
                'المحادثات',
                style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.white),
              ),
              if (_conversations.isNotEmpty)
                Text(
                  '${_conversations.length} محادثة',
                  style: TextStyle(fontFamily: 'Tajawal', fontSize: 11, color: Colors.white.withValues(alpha: 0.8)),
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
            // زر محادثة جديدة
            Padding(
              padding: EdgeInsets.only(left: 8),
              child: Container(
                decoration: BoxDecoration(
                  color: Colors.white.withValues(alpha: 0.2),
                  shape: BoxShape.circle,
                ),
                child: IconButton(
                  icon: Icon(Icons.add_comment, color: Colors.white, size: 22),
                  onPressed: _openNewChat,
                  tooltip: 'محادثة جديدة',
                ),
              ),
            ),
          ],
        ),
        body: _isLoading
            ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    CircularProgressIndicator(color: primaryBlue),
                    SizedBox(height: 16),
                    Text('جاري تحميل المحادثات...', style: TextStyle(fontFamily: 'Tajawal', color: Colors.grey[500], fontSize: 13)),
                  ],
                ),
              )
            : RefreshIndicator(
                onRefresh: _load,
                color: primaryBlue,
                child: _conversations.isEmpty
                    ? _buildEmptyState()
                    : ListView.separated(
                        padding: EdgeInsets.all(16),
                        itemCount: _conversations.length,
                        separatorBuilder: (context, i) => SizedBox(height: 10),
                        itemBuilder: (context, i) {
                          final c = _conversations[i];
                          return _buildConversationCard(c);
                        },
                      ),
              ),
        // زر عائم لبدء محادثة جديدة
        floatingActionButton: _conversations.isNotEmpty
            ? FloatingActionButton(
                onPressed: _openNewChat,
                backgroundColor: primaryBlue,
                foregroundColor: Colors.white,
                elevation: 4,
                child: Icon(Icons.add_comment, size: 24),
              )
            : null,
      ),
    );
  }

  Widget _buildEmptyState() {
    return ListView(
      children: [
        SizedBox(height: 80),
        Container(
          width: 100,
          height: 100,
          decoration: BoxDecoration(
            color: lightBlue,
            shape: BoxShape.circle,
          ),
          child: Icon(Icons.chat_bubble_outline, size: 50, color: primaryBlue),
        ),
        SizedBox(height: 24),
        Text(
          'لا توجد محادثات بعد',
          textAlign: TextAlign.center,
          style: TextStyle(fontFamily: 'Tajawal', fontSize: 18, fontWeight: FontWeight.bold, color: Colors.grey[700]),
        ),
        SizedBox(height: 8),
        Text(
          'ابدأ محادثة جديدة للتواصل مع زملائك',
          textAlign: TextAlign.center,
          style: TextStyle(fontFamily: 'Tajawal', fontSize: 13, color: Colors.grey[500]),
        ),
        SizedBox(height: 32),
        Center(
          child: ElevatedButton.icon(
            onPressed: _openNewChat,
            style: ElevatedButton.styleFrom(
              backgroundColor: primaryBlue,
              foregroundColor: Colors.white,
              padding: EdgeInsets.symmetric(horizontal: 24, vertical: 14),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
            ),
            icon: Icon(Icons.add_comment, size: 20),
            label: Text('بدء محادثة', style: TextStyle(fontFamily: 'Tajawal', fontWeight: FontWeight.bold)),
          ),
        ),
      ],
    );
  }

  Widget _buildConversationCard(dynamic c) {
    final unread = c['unreadCount'] ?? 0;
    final lastMessage = c['lastMessage'] ?? '';
    final lastTime = c['lastMessageTime'];
    final userName = c['otherUserName'] ?? 'مستخدم';

    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(color: Colors.black.withValues(alpha: 0.04), blurRadius: 8, offset: Offset(0, 2)),
        ],
        border: Border.all(color: unread > 0 ? primaryBlue.withValues(alpha: 0.3) : Color(0xFFE5E7EB)),
      ),
      child: InkWell(
        onTap: () async {
          await Navigator.push(
            context,
            MaterialPageRoute(
              builder: (_) => ChatThreadPage(
                user: widget.user,
                chatToken: widget.chatToken,
                otherUserId: c['otherUserId'],
                otherUserName: userName,
                profileImageData: c['profileImageData'] ?? c['profileImage'] ?? '',
              ),
            ),
          );
          _load();
        },
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: EdgeInsets.all(14),
          child: Row(
            children: [
              // Avatar
              Stack(
                children: [
                  Container(
                    width: 52,
                    height: 52,
                    decoration: BoxDecoration(
                      gradient: LinearGradient(
                        begin: Alignment.topRight,
                        end: Alignment.bottomLeft,
                        colors: [primaryBlue, darkBlue],
                      ),
                      shape: BoxShape.circle,
                    ),
                    child: _buildConversationAvatar(c),
                  ),
                  // Online indicator
                  Positioned(
                    bottom: 0,
                    left: 0,
                    child: Container(
                      width: 14,
                      height: 14,
                      decoration: BoxDecoration(
                        color: greenColor,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 2),
                      ),
                    ),
                  ),
                ],
              ),
              SizedBox(width: 14),
              
              // Message content
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            userName,
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 15,
                              fontWeight: FontWeight.bold,
                              color: Colors.grey[800],
                            ),
                          ),
                        ),
                        if (lastTime != null)
                          Text(
                            _formatTime(lastTime),
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 11,
                              color: unread > 0 ? primaryBlue : Colors.grey[400],
                              fontWeight: unread > 0 ? FontWeight.bold : FontWeight.normal,
                            ),
                          ),
                      ],
                    ),
                    SizedBox(height: 4),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            lastMessage.isNotEmpty ? lastMessage : 'ابدأ المحادثة الآن',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: TextStyle(
                              fontFamily: 'Tajawal',
                              fontSize: 13,
                              color: unread > 0 ? Colors.grey[700] : Colors.grey[500],
                              fontWeight: unread > 0 ? FontWeight.w600 : FontWeight.normal,
                            ),
                          ),
                        ),
                        if (unread > 0) ...[
                          SizedBox(width: 8),
                          Container(
                            padding: EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                            decoration: BoxDecoration(
                              color: primaryBlue,
                              borderRadius: BorderRadius.circular(12),
                            ),
                            child: Text(
                              '$unread',
                              style: TextStyle(
                                color: Colors.white,
                                fontSize: 11,
                                fontWeight: FontWeight.bold,
                                fontFamily: 'Tajawal',
                              ),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],
                ),
              ),
              
              SizedBox(width: 8),
              Icon(Icons.chevron_left, color: Colors.grey[300], size: 20),
            ],
          ),
        ),
      ),
    );
  }
}