import 'dart:convert';

import 'package:flutter/material.dart';
import '../services/chat_service.dart';
import '../services/chat_history_service.dart';

class ChatThreadPage extends StatefulWidget {
  final Map user;
  final String chatToken;
  final int otherUserId;
  final String otherUserName;
  final String profileImageData;


  const ChatThreadPage({
    super.key,
    required this.user,
    required this.chatToken,
    required this.otherUserId,
    required this.otherUserName,
    this.profileImageData = '',
  });

  @override
  State<ChatThreadPage> createState() => _ChatThreadPageState();
}

class _ChatThreadPageState extends State<ChatThreadPage> {
  final ChatHistoryService _history = ChatHistoryService();
  late final ChatService _chat;
  final _controller = TextEditingController();
  final _scrollController = ScrollController();

  List<dynamic> _messages = [];
  bool _isLoading = true;
  final Set<String> _receivedMessageKeys = {}; // لمنع التكرار

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color lightBlue = Color(0xFFDBEAFE);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color myMessageColor = Color(0xFF2563EB);
  final Color otherMessageColor = Colors.white;

  int get _myId => widget.user['id'];

  @override
  void initState() {
    super.initState();
    _chat = ChatService(chatToken: widget.chatToken);
    _init();
  }

  Future<void> _init() async {
  try {
    // تحميل الرسائل أولاً
    final msgs = await _history.getDirectMessages(_myId, widget.otherUserId);
    if (mounted) {
      setState(() {
        _messages = msgs;
        for (var m in msgs) {
          if (m['id'] != null) {
            _receivedMessageKeys.add(m['id'].toString());
          }
        }
        _isLoading = false;
      });
      _scrollToBottom();
    }

    // محاولة الاتصال في الخلفية - لا تمنع الواجهة
    _tryConnectToChat();
  } catch (e) {
    if (mounted) {
      setState(() => _isLoading = false);
      _showMessage('فشل تحميل الرسائل');
    }
  }
}

Future<void> _tryConnectToChat() async {
  try {
    await _chat.connect();
    
    // إذا نجح الاتصال، استمع للرسائل
    _chat.onMessageReceived.listen((m) {
      if (!mounted) return;
      
      final messageId = m['messageId']?.toString() ?? '';
      final messageText = m['message']?.toString() ?? '';
      final sentAt = m['sentAt']?.toString() ?? '';

      if (messageId.isNotEmpty && _receivedMessageKeys.contains(messageId)) {
        return;
      }
      if (messageId.isNotEmpty) {
        _receivedMessageKeys.add(messageId);
      }

      final fromUserId = m['fromUserId'];
      final toUserId = m['toUserId'];

      if (fromUserId == widget.otherUserId || toUserId == widget.otherUserId) {
        setState(() {
          _messages.add({
            'id': messageId,
            'senderId': fromUserId ?? _myId,
            'message': messageText,
            'sentAt': sentAt,
          });
        });
        _scrollToBottom();
      }
    });
  } catch (e) {
    // الاتصال فشل - لا تعرض رسالة خطأ مزعجة
    print('Chat connection failed silently: $e');
  }
}

void _showMessage(String message) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message, textDirection: TextDirection.rtl),
      backgroundColor: Color(0xFFEF4444),
      behavior: SnackBarBehavior.floating,
      margin: EdgeInsets.all(16),
      duration: Duration(seconds: 2),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
    ),
  );
}
  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _send() async {
  final text = _controller.text.trim();
  if (text.isEmpty) return;
  _controller.clear();
  _scrollToBottom();

  try {
    await _chat.sendMessage(widget.otherUserId, text);
  } catch (e) {
    if (mounted) {
      // إعادة النص للحقل
      _controller.text = text;
      
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('تعذر إرسال الرسالة - حاول مرة أخرى', textDirection: TextDirection.rtl),
          backgroundColor: Color(0xFFF59E0B),
          behavior: SnackBarBehavior.floating,
          margin: EdgeInsets.all(16),
          duration: Duration(seconds: 2),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
        ),
      );
    }
  }
}

  String _formatTime(dynamic time) {
    if (time == null) return '';
    try {
      final dateTime = DateTime.parse(time.toString());
      return '${dateTime.hour.toString().padLeft(2, '0')}:${dateTime.minute.toString().padLeft(2, '0')}';
    } catch (e) {
      return '';
    }
  }

  @override
  void dispose() {
    _chat.disconnect();
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        appBar: AppBar(
          title: Row(
            children: [
              // Avatar
              Container(
                width: 40,
                height: 40,
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topRight,
                    end: Alignment.bottomLeft,
                    colors: [Colors.white.withValues(alpha: 0.3), Colors.white.withValues(alpha: 0.1)],
                  ),
                  shape: BoxShape.circle,
                  border: Border.all(color: Colors.white.withValues(alpha: 0.4), width: 1.5),
                ),
                child: _buildChatAvatar()
              ),
              SizedBox(width: 12),
              // Name and status
              Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.otherUserName,
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: Colors.white,
                    ),
                  ),
                  SizedBox(height: 2),
                  Row(
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        decoration: BoxDecoration(
                          color: Color(0xFF10B981),
                          shape: BoxShape.circle,
                        ),
                      ),
                      SizedBox(width: 4),
                      Text(
                        'متصل الآن',
                        style: TextStyle(
                          fontFamily: 'Tajawal',
                          fontSize: 11,
                          color: Colors.white.withValues(alpha: 0.8),
                        ),
                      ),
                    ],
                  ),
                ],
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
            Expanded(
              child: _isLoading
                  ? Center(
                      child: CircularProgressIndicator(color: primaryBlue),
                    )
                  : _messages.isEmpty
                      ? _buildEmptyChat()
                      : ListView.builder(
                          controller: _scrollController,
                          padding: EdgeInsets.all(16),
                          itemCount: _messages.length,
                          itemBuilder: (context, i) {
                            final m = _messages[i];
                            final isMine = (m['senderId'] ?? m['SenderId']) == _myId;
                            final messageText = m['message'] ?? '';
                            final time = _formatTime(m['sentAt']);

                            return _buildMessageBubble(
                              message: messageText,
                              isMine: isMine,
                              time: time,
                            );
                          },
                        ),
            ),
            _buildMessageInput(),
          ],
        ),
      ),
    );
  }

  // في AppBar - استبدال avatar
Widget _buildChatAvatar() {
  final profileImage = widget.profileImageData ?? '';
  
  if (profileImage.toString().isNotEmpty) {
    return Container(
      width: 40,
      height: 40,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: Colors.white.withValues(alpha: 0.4), width: 1.5),
      ),
      child: CircleAvatar(
        radius: 20,
        backgroundImage: MemoryImage(base64Decode(profileImage.toString())),
      ),
    );
  }
  
  return Container(
    width: 40,
    height: 40,
    decoration: BoxDecoration(
      gradient: LinearGradient(
        begin: Alignment.topRight,
        end: Alignment.bottomLeft,
        colors: [Colors.white.withValues(alpha: 0.3), Colors.white.withValues(alpha: 0.1)],
      ),
      shape: BoxShape.circle,
      border: Border.all(color: Colors.white.withValues(alpha: 0.4), width: 1.5),
    ),
    child: Center(
      child: Text(
        widget.otherUserName.isNotEmpty ? widget.otherUserName[0] : '?',
        style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold, fontFamily: 'Tajawal'),
      ),
    ),
  );
}

  Widget _buildEmptyChat() {
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
            child: Icon(Icons.chat_bubble_outline, size: 40, color: primaryBlue),
          ),
          SizedBox(height: 16),
          Text(
            'ابدأ المحادثة',
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 16,
              fontWeight: FontWeight.bold,
              color: Colors.grey[700],
            ),
          ),
          SizedBox(height: 6),
          Text(
            'أرسل رسالة للتواصل مع ${widget.otherUserName}',
            textAlign: TextAlign.center,
            style: TextStyle(
              fontFamily: 'Tajawal',
              fontSize: 13,
              color: Colors.grey[500],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMessageBubble({
    required String message,
    required bool isMine,
    required String time,
  }) {
    return Align(
      alignment: isMine ? Alignment.centerLeft : Alignment.centerRight,
      child: Container(
        margin: EdgeInsets.symmetric(vertical: 4),
        padding: EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        constraints: BoxConstraints(
          maxWidth: MediaQuery.of(context).size.width * 0.75,
        ),
        decoration: BoxDecoration(
          color: isMine ? myMessageColor : otherMessageColor,
          borderRadius: BorderRadius.only(
            topLeft: Radius.circular(16),
            topRight: Radius.circular(16),
            bottomLeft: Radius.circular(isMine ? 16 : 4),
            bottomRight: Radius.circular(isMine ? 4 : 16),
          ),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 4,
              offset: Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              message,
              style: TextStyle(
                color: isMine ? Colors.white : Colors.grey[800],
                fontSize: 14,
                fontFamily: 'Tajawal',
              ),
            ),
            if (time.isNotEmpty) ...[
              SizedBox(height: 4),
              Text(
                time,
                style: TextStyle(
                  color: isMine ? Colors.white.withValues(alpha: 0.7) : Colors.grey[400],
                  fontSize: 10,
                  fontFamily: 'Tajawal',
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildMessageInput() {
    return SafeArea(
      child: Container(
        padding: EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: cardColor,
          boxShadow: [
            BoxShadow(
              color: Colors.black.withValues(alpha: 0.05),
              blurRadius: 10,
              offset: Offset(0, -2),
            ),
          ],
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Expanded(
              child: Container(
                decoration: BoxDecoration(
                  color: backgroundColor,
                  borderRadius: BorderRadius.circular(24),
                  border: Border.all(color: Color(0xFFE5E7EB)),
                ),
                child: TextField(
                  controller: _controller,
                  textDirection: TextDirection.rtl,
                  minLines: 1,
                  maxLines: 4,
                  decoration: InputDecoration(
                    hintText: 'اكتب رسالة...',
                    hintStyle: TextStyle(
                      color: Colors.grey[400],
                      fontFamily: 'Tajawal',
                      fontSize: 14,
                    ),
                    border: InputBorder.none,
                    contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                  ),
                  onSubmitted: (_) => _send(),
                ),
              ),
            ),
            SizedBox(width: 8),
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
                boxShadow: [
                  BoxShadow(
                    color: primaryBlue.withValues(alpha: 0.4),
                    blurRadius: 8,
                    offset: Offset(0, 3),
                  ),
                ],
              ),
              child: IconButton(
                icon: Icon(Icons.send_rounded, color: Colors.white, size: 22),
                onPressed: _send,
              ),
            ),
          ],
        ),
      ),
    );
  }
}