import 'dart:async';
import 'package:signalr_netcore/signalr_client.dart';
import 'api_config.dart';

class ChatService {
  HubConnection? _hub;
  final String hubUrl;
  final String chatToken;

  final _messageController = StreamController<Map<String, dynamic>>.broadcast();
  Stream<Map<String, dynamic>> get onMessageReceived => _messageController.stream;

  final _groupMessageController = StreamController<Map<String, dynamic>>.broadcast();
  Stream<Map<String, dynamic>> get onGroupMessageReceived => _groupMessageController.stream;

  ChatService({required this.chatToken})
      : hubUrl = ApiConfig.chatHubUrl;

  Future<void> connect() async {
    _hub = HubConnectionBuilder()
        .withUrl(
          hubUrl,
          options: HttpConnectionOptions(
            accessTokenFactory: () async => chatToken,
          ),
        )
        .withAutomaticReconnect()
        .build();

    _hub!.on('ReceiveMessage', (args) {
      if (args == null || args.length < 5) return;
      _messageController.add({
        'fromUserId': args[0],
        'toUserId': args[1],
        'message': args[2],
        'sentAt': args[3],
        'messageId': args[4],
      });
    });

    _hub!.on('MessageSent', (args) {
      if (args == null || args.length < 4) return;
      _messageController.add({
        'toUserId': args[0],
        'message': args[1],
        'sentAt': args[2],
        'messageId': args[3],
        'isMine': true,
      });
    });

    _hub!.on('ReceiveGroupMessage', (args) {
      if (args == null || args.length < 5) return;
      _groupMessageController.add({
        'groupId': args[0],
        'fromUserId': args[1],
        'message': args[2],
        'sentAt': args[3],
        'messageId': args[4],
      });
    });

    await _hub!.start();
  }

  Future<void> sendMessage(int toUserId, String message) async {
    await _hub?.invoke('SendMessageToUser', args: [toUserId, message]);
  }

  Future<void> sendGroupMessage(int groupId, String message) async {
    await _hub?.invoke('SendGroupMessage', args: [groupId, message]);
  }

  Future<void> joinGroup(int groupId) async {
    await _hub?.invoke('JoinGroup', args: [groupId]);
  }

  Future<void> disconnect() async {
    await _hub?.stop();
  }
}