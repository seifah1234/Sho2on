import 'package:geolocator/geolocator.dart';
import 'package:geocoding/geocoding.dart';
import 'location_cache.dart';
import 'package:flutter/material.dart';

class LocationResult {
  final double latitude;
  final double longitude;
  final String locationName;

  LocationResult({
    required this.latitude,
    required this.longitude,
    required this.locationName,
  });
}

class LocationService {
  static Future<LocationResult?> getCurrent() async {
    try {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) return null;
      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return null;
      }

    final position = await Geolocator.getCurrentPosition(
      desiredAccuracy: LocationAccuracy.best,
      timeLimit: const Duration(seconds: 10),
    );

    String locationName = 'غير معروف';

    try {
      final placemarks = await placemarkFromCoordinates(
        position.latitude,
        position.longitude,
      );

      if (placemarks.isNotEmpty) {
        final p = placemarks.first;
        locationName =
            '${p.street ?? ''}, ${p.subLocality ?? ''}, ${p.locality ?? ''}, ${p.country ?? ''}'
                .replaceAll(RegExp(', +'), ', ')
                .replaceAll(RegExp(',\$'), '');

        await LocationCache.save(locationName);
      }
    } catch (_) {
      final cached = await LocationCache.get();
      if (cached != null) {
        locationName = cached;
      }
    }

    return LocationResult(
      latitude: position.latitude,
      longitude: position.longitude,
      locationName: locationName,
    );
    } catch (_) {
      return null;
    }
  }

  static Future<bool> ensureLocationEnabled(BuildContext context) async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      // ✅ تأكد إن الـ context لسه valid قبل showDialog
      if (!context.mounted) return false;

      await showDialog(
        context: context,
        builder: (dialogContext) => Directionality(  // ✅ استخدم dialogContext جوه الـ builder
          textDirection: TextDirection.rtl,
          child: AlertDialog(
            title: const Text('تشغيل الموقع'),
            content: const Text('من فضلك فعّل خدمة الموقع قبل تسجيل الحضور'),
            actions: [
              TextButton(
                onPressed: () {
                  Geolocator.openLocationSettings();
                  Navigator.pop(dialogContext);  // ✅
                },
                child: const Text('فتح الإعدادات'),
              ),
              TextButton(
                onPressed: () => Navigator.pop(dialogContext),  // ✅
                child: const Text('إلغاء'),
              ),
            ],
          ),
        ),
      );
      return false;
    }
    return true;
  }
}
