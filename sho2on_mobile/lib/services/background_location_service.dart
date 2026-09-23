import 'dart:async';
import 'dart:convert';
import 'dart:ui';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_background_service_android/flutter_background_service_android.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

import 'api_config.dart';

@pragma('vm:entry-point')
class BackgroundLocationService {
  static const String _notificationChannelId = 'location_tracking_channel';
  static const String _notificationChannelName = 'تتبع الموقع';
  static const String _notificationChannelDesc =
      'قناة إشعارات تتبع الموقع أثناء الوردية';
  static const int _notificationId = 888;
  static const Duration _interval = Duration(seconds: 5);

  static const String _prefsUserIdKey = 'bg_service_user_id';
  static const String _prefsTokenKey = 'bg_service_token';

  // ==================== التهيئة ====================

  /// ⚠️ لازم تتنادى مرة واحدة في main() بعد
  /// WidgetsFlutterBinding.ensureInitialized()
  static Future<void> initialize() async {
    // ✅ 1) إنشاء قناة الإشعارات قبل أي startService
    await _createNotificationChannel();

    // ✅ 2) تهيئة FlutterLocalNotificationsPlugin (بيستخدمها الـ plugin داخلياً)
    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const iosInit = DarwinInitializationSettings();
    const initSettings = InitializationSettings(
      android: androidInit,
      iOS: iosInit,
    );
    await FlutterLocalNotificationsPlugin().initialize(initSettings);

    // ✅ 3) تكوين FlutterBackgroundService
    final service = FlutterBackgroundService();
    await service.configure(
      androidConfiguration: AndroidConfiguration(
        onStart: _onStart,
        autoStart: false,
        isForegroundMode: true,
        notificationChannelId: _notificationChannelId,
        initialNotificationTitle: 'تتبع الموقع أثناء الوردية',
        initialNotificationContent: 'التطبيق بيحدّث موقعك بشكل دوري',
        foregroundServiceNotificationId: _notificationId,
        foregroundServiceTypes: [AndroidForegroundType.location],
      ),
      iosConfiguration: IosConfiguration(
        autoStart: false,
        onForeground: _onStart,
        onBackground: _onIosBackground,
      ),
    );
  }

  static Future<void> _createNotificationChannel() async {
    const channel = AndroidNotificationChannel(
      _notificationChannelId,
      _notificationChannelName,
      description: _notificationChannelDesc,
      importance: Importance.low, // منخفض كفاية لتجنب الإزعاج
      playSound: false,
      enableVibration: false,
    );

    await FlutterLocalNotificationsPlugin()
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >()
        ?.createNotificationChannel(channel);
  }

  // ==================== طلب الإذن ====================

  static Future<bool> ensureBackgroundPermission(BuildContext context) async {
    try {
      if (!context.mounted) return false;

      // ✅ اطلب إذن الإشعارات (Android 13+)
      await _requestNotificationPermissionIfNeeded();

      var permission = await Geolocator.checkPermission();
      if (!context.mounted) return false;

      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (!context.mounted) return false;
      }

      if (permission == LocationPermission.deniedForever) {
        await _showSettingsDialog(context);
        return false;
      }

      if (permission != LocationPermission.always) {
        final upgraded = await Geolocator.requestPermission();
        if (!context.mounted) return false;

        if (upgraded != LocationPermission.always) {
          await _showSettingsDialog(context);
          return upgraded == LocationPermission.always;
        }
      }

      return true;
    } catch (e) {
      print('ensureBackgroundPermission error: $e');
      return false;
    }
  }

  static Future<void> _requestNotificationPermissionIfNeeded() async {
    try {
      final plugin = FlutterLocalNotificationsPlugin()
          .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin
          >();
      final granted = await plugin?.requestNotificationsPermission();
      debugPrint('Notification permission granted: $granted');
    } catch (e) {
      debugPrint('requestNotificationsPermission error: $e');
    }
  }

  static Future<void> _showSettingsDialog(BuildContext context) async {
    if (!context.mounted) return;
    try {
      await showDialog(
        context: context,
        builder: (dialogContext) => Directionality(
          textDirection: TextDirection.rtl,
          child: AlertDialog(
            title: const Text('إذن الموقع في الخلفية'),
            content: const Text(
              'عشان تتبع موقعك يفضل شغال حتى لو التطبيق مقفول، من فضلك افتح '
              'إعدادات التطبيق واختار إذن الموقع "Allow all the time".',
            ),
            actions: [
              TextButton(
                onPressed: () {
                  Geolocator.openAppSettings();
                  Navigator.of(dialogContext).pop();
                },
                child: const Text('فتح الإعدادات'),
              ),
              TextButton(
                onPressed: () => Navigator.of(dialogContext).pop(),
                child: const Text('لاحقاً'),
              ),
            ],
          ),
        ),
      );
    } catch (e) {
      debugPrint('_showSettingsDialog error: $e');
    }
  }

  // ==================== تشغيل/إيقاف السيرفس ====================

  /// [token] اختياري: لو عندك توكن بيتبعت مع كل request
  static Future<void> start(int userId, {String? token}) async {
    try {
      // ✅ 1) خزّن userId + token في SharedPreferences
      //    عشان الـ isolate الخلفي يقرأهم حتى لو الـ invoke ضاع
      final prefs = await SharedPreferences.getInstance();
      await prefs.setInt(_prefsUserIdKey, userId);
      if (token != null) {
        await prefs.setString(_prefsTokenKey, token);
      }

      final service = FlutterBackgroundService();
      final isRunning = await service.isRunning();
      debugPrint('BackgroundLocationService.start isRunning=$isRunning');

      if (!isRunning) {
        await service.startService();
      }

      // ✅ 2) نبعت الـ userId كمان كـ invoke (للحالة اللي السيرفس شغال بالفعل)
      //    وبعدها نكررها كام مرة كـ redundancy
      await _invokeUserIdWithRetry(service, userId);
    } catch (e, st) {
      debugPrint('BackgroundLocationService.start error: $e\n$st');
    }
  }

  static Future<void> _invokeUserIdWithRetry(
    FlutterBackgroundService service,
    int userId, {
    int attempts = 3,
    Duration delay = const Duration(milliseconds: 400),
  }) async {
    for (int i = 0; i < attempts; i++) {
      try {
        debugPrint('invoke setUserId attempt $i');
        service.invoke('setUserId', {'userId': userId});
      } catch (e) {
        debugPrint('invoke setUserId attempt $i failed: $e');
      }
      await Future.delayed(delay);
    }
  }

  static Future<void> stop() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      await prefs.remove(_prefsUserIdKey);
      // ما نمسحش التوكن — ممكن نحتاجه لاحقاً

      final service = FlutterBackgroundService();
      if (await service.isRunning()) {
        service.invoke('stopService');
      }
    } catch (e) {
      debugPrint('BackgroundLocationService.stop error: $e');
    }
  }

  static Future<bool> isRunning() async {
    try {
      return await FlutterBackgroundService().isRunning();
    } catch (_) {
      return false;
    }
  }

  // ==================== نقطة دخول الـ Isolate الخلفية ====================

  @pragma('vm:entry-point')
  static void _onStart(ServiceInstance service) async {
    DartPluginRegistrant.ensureInitialized();

    int? userId;
    String? token;
    Timer? timer;
    bool isSending = false;

    // ✅ 1) اقرأ userId من التخزين أول ما الـ isolate يبدأ
    //    حتى لو الـ invoke مش وصل
    try {
      final prefs = await SharedPreferences.getInstance();
      userId = prefs.getInt(_prefsUserIdKey);
      token = prefs.getString(_prefsTokenKey);
    } catch (e) {
      debugPrint('onStart: failed to read prefs: $e');
    }

    if (service is AndroidServiceInstance) {
      service.on('setAsForeground').listen((_) {
        service.setAsForegroundService();
      });
      service.on('setAsBackground').listen((_) {
        service.setAsBackgroundService();
      });
    }

    service.on('setUserId').listen((event) {
      final newUserId = event?['userId'] as int?;
      debugPrint('setUserId event received: $newUserId');
      if (newUserId != null) {
        userId = newUserId;
        debugPrint('onStart: userId set to $newUserId');
        SharedPreferences.getInstance().then(
          (p) => p.setInt(_prefsUserIdKey, newUserId),
        );
      }
    });

    service.on('stopService').listen((_) {
      debugPrint('onStart: stopService received');
      timer?.cancel();
      timer = null;
      userId = null;
      service.stopSelf();
    });

    Future<Position?> tryGetPosition() async {
      try {
        return await Geolocator.getCurrentPosition(
          desiredAccuracy: LocationAccuracy.medium,
          timeLimit: const Duration(seconds: 25),
        );
      } catch (e) {
        debugPrint('getCurrentPosition failed: $e');
      }
      try {
        final last = await Geolocator.getLastKnownPosition();
        if (last != null) {
          final age = DateTime.now().difference(last.timestamp);
          if (age.inMinutes < 10) {
            debugPrint('Using last known position (age: ${age.inMinutes}m)');
            return last;
          }
        }
      } catch (e) {
        debugPrint('getLastKnownPosition failed: $e');
      }
      return null;
    }

    Future<void> sendLocationOnce() async {
      // ⬅️ منع التراكب
      debugPrint('sendLocationOnce: called, isSending=$isSending');

      if (isSending) {
        debugPrint('sendLocationOnce: already sending, skip');
        return;
      }
      isSending = true;

      try {
        final uid = userId;
        debugPrint('sendLocationOnce: uid=$uid, token=${token != null}');
        if (uid == null) return;

        final serviceEnabled = await Geolocator.isLocationServiceEnabled();
        if (!serviceEnabled) {
          debugPrint('Location service disabled');
          return;
        }

        final position = await tryGetPosition();
        if (position == null) {
          debugPrint('No position available');
          return;
        }

        debugPrint(
          'Position: ${position.latitude},${position.longitude} '
          'acc=${position.accuracy}',
        );

        final headers = <String, String>{'Content-Type': 'application/json'};
        if (token != null && token.isNotEmpty) {
          headers['Authorization'] = 'Bearer $token';
        }

        final response = await http
            .post(
              Uri.parse('${ApiConfig.baseUrl}/Location/Update'),
              headers: headers,
              body: json.encode({
                'userId': uid,
                'latitude': position.latitude,
                'longitude': position.longitude,
                'accuracy': position.accuracy,
                'isOnShift': true,
              }),
            )
            .timeout(const Duration(seconds: 15));

        debugPrint('Location sent: status=${response.statusCode} uid=$uid');
        debugPrint('Response body: ${response.body}');

        if (service is AndroidServiceInstance) {
          final now = TimeOfDay.fromDateTime(DateTime.now());
          final hh = now.hour.toString().padLeft(2, '0');
          final mm = now.minute.toString().padLeft(2, '0');
          await service.setForegroundNotificationInfo(
            title: 'تتبع الموقع أثناء الوردية',
            content: 'آخر تحديث: $hh:$mm',
          );
        }
      } catch (e, st) {
        debugPrint('sendLocationOnce error: $e\n$st');
      } finally {
        isSending = false;
      }
    }

    timer = Timer.periodic(_interval, (_) async {
      await sendLocationOnce();
    });

    // ✅ استنى شوية لو الـ userId لسه موصلش
    if (userId == null) {
      for (int i = 0; i < 10 && userId == null; i++) {
        await Future.delayed(const Duration(milliseconds: 500));
      }
    }

    // ✅ أول تحديث بعد ما نتأكد إن الـ userId موجود
    await sendLocationOnce();

    print('onStart: timer started with interval $_interval');
  }

  @pragma('vm:entry-point')
  static bool _onIosBackground(ServiceInstance service) {
    DartPluginRegistrant.ensureInitialized();
    return true;
  }
}
