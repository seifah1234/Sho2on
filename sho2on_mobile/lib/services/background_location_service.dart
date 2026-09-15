import 'dart:async';
import 'dart:convert';
import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter_background_service/flutter_background_service.dart';
import 'package:flutter_background_service_android/flutter_background_service_android.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'api_config.dart';

/// خدمة تتبع الموقع في الخلفية (Background Service).
/// بتفضل شغالة وتبعت الموقع كل فترة حتى لو الموظف قفل التطبيق أو قفل الشاشة،
/// طول ما السيرفس شغالة (بتوقف بس لما هو يعمل Check-out أو يقفل السيرفس يدوي).
///
/// ملحوظات مهمة قبل التشغيل على جهاز حقيقي:
/// 1) لازم إذن الموقع يبقى "Allow all the time" مش "While using the app" —
///    الأندرويد مش بيدي الإذن ده من نافذة الطلب العادية بعد إصدارات معينة،
///    فمحتاجين نوجّه المستخدم لإعدادات التطبيق يدويًا (`ensureBackgroundPermission`).
/// 2) هيظهر إشعار ثابت (Foreground Notification) طول ما التتبع شغال —
///    ده مطلوب من أندرويد نفسه، مش اختياري.
/// 3) على iOS التتبع في الخلفية محدود جدًا بسياسات أبل، والإعداد التحتي هنا
///    مبني بشكل أساسي لأندرويد (وده المنصة المستهدفة في المشروع حاليًا).
class BackgroundLocationService {
  static const String _notificationChannelId = 'location_tracking_channel';
  static const int _notificationId = 888;
  static const Duration _interval = Duration(seconds: 45);

  /// ينادى مرة واحدة بس في main() قبل runApp()
  static Future<void> initialize() async {
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

  /// يتأكد إن إذن الموقع "دايمًا" (Always) متاح، ولو لأ بيوجه المستخدم للإعدادات
  static Future<bool> ensureBackgroundPermission(BuildContext context) async {
    var permission = await Geolocator.checkPermission();

    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }

    if (permission == LocationPermission.deniedForever) {
      await _showSettingsDialog(context);
      return false;
    }

    if (permission != LocationPermission.always) {
      // في الغالب الحالة دلوقتي whileInUse - نطلب ترقية للـ "Allow all the time"
      final upgraded = await Geolocator.requestPermission();
      if (upgraded != LocationPermission.always) {
        // الأندرويد مش دايمًا بيسمح بترقية مباشرة؛ نوجه المستخدم للإعدادات يدويًا
        await _showSettingsDialog(context);
        return upgraded == LocationPermission.always;
      }
    }

    return true;
  }

  static Future<void> _showSettingsDialog(BuildContext context) async {
    if (!context.mounted) return;
    await showDialog(
      context: context,
      builder: (_) => Directionality(
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
                Navigator.pop(context);
              },
              child: const Text('فتح الإعدادات'),
            ),
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('لاحقاً'),
            ),
          ],
        ),
      ),
    );
  }

  /// يبدأ السيرفس ويبعتله الـ userId
  static Future<void> start(int userId) async {
    final service = FlutterBackgroundService();
    final isRunning = await service.isRunning();
    if (!isRunning) {
      await service.startService();
    }
    // ناخد لحظة بسيطة عشان الـ isolate يبدأ يستقبل الأحداث
    await Future.delayed(const Duration(milliseconds: 300));
    service.invoke('setUserId', {'userId': userId});
  }

  static Future<void> stop() async {
    final service = FlutterBackgroundService();
    if (await service.isRunning()) {
      service.invoke('stopService');
    }
  }

  static Future<bool> isRunning() async {
    return FlutterBackgroundService().isRunning();
  }

  // ==================== نقطة دخول الـ Isolate الخلفية ====================

  @pragma('vm:entry-point')
  static void _onStart(ServiceInstance service) async {
    DartPluginRegistrant.ensureInitialized();

    int? userId;
    Timer? timer;

    if (service is AndroidServiceInstance) {
      service.on('setAsForeground').listen((_) {
        service.setAsForegroundService();
      });
      service.on('setAsBackground').listen((_) {
        service.setAsBackgroundService();
      });
    }

    service.on('setUserId').listen((event) {
      userId = event?['userId'] as int?;
    });

    service.on('stopService').listen((event) {
      timer?.cancel();
      service.stopSelf();
    });

    Future<void> sendLocationOnce() async {
      final uid = userId;
      if (uid == null) return;

      try {
        final serviceEnabled = await Geolocator.isLocationServiceEnabled();
        if (!serviceEnabled) return;

        final position = await Geolocator.getCurrentPosition(
          desiredAccuracy: LocationAccuracy.high,
          timeLimit: const Duration(seconds: 15),
        );

        await http
            .post(
              Uri.parse('${ApiConfig.baseUrl}/Location/Update'),
              headers: {
                'Content-Type': 'application/json',
                'Authorization': 'Bearer YOUR_TOKEN',
              },
              body: json.encode({
                'userId': uid,
                'latitude': position.latitude,
                'longitude': position.longitude,
                'accuracy': position.accuracy,
                'isOnShift': true,
              }),
            )
            .timeout(const Duration(seconds: 12));

        if (service is AndroidServiceInstance) {
          final now = TimeOfDay.fromDateTime(DateTime.now());
          await service.setForegroundNotificationInfo(
            title: 'تتبع الموقع أثناء الوردية',
            content:
                'آخر تحديث: ${now.hour.toString().padLeft(2, '0')}:${now.minute.toString().padLeft(2, '0')}',
          );
        }
      } catch (_) {
        // فشل تحديث واحد مش لازم يوقف السيرفس، هيتحاول تاني في الدورة الجاية
      }
    }

    // إرسال فوري أول ما تبدأ السيرفس، وبعدين كل فترة
    await sendLocationOnce();
    timer = Timer.periodic(_interval, (_) => sendLocationOnce());
  }

  @pragma('vm:entry-point')
  static bool _onIosBackground(ServiceInstance service) {
    // iOS بيدي وقت محدود جدًا للتنفيذ في الخلفية، فبنكتفي بمحاولة إرسال واحدة
    return true;
  }
}
