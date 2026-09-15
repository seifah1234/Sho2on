import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:latlong2/latlong.dart';
import '../../services/team_location_service.dart';

class TeamLocationPage extends StatefulWidget {
  final Map user;
  const TeamLocationPage({required this.user, super.key});

  @override
  State<TeamLocationPage> createState() => _TeamLocationPageState();
}

class _TeamLocationPageState extends State<TeamLocationPage> {
  final TeamLocationService _service = TeamLocationService();
  final MapController _mapController = MapController();

  List<dynamic> _employees = [];
  bool _isLoading = true;
  String? _error;
  Timer? _refreshTimer;
  int? _selectedUserId;

  final Color primaryBlue = Color(0xFF2563EB);
  final Color darkBlue = Color(0xFF1E40AF);
  final Color backgroundColor = Color(0xFFF8FAFC);
  final Color cardColor = Colors.white;
  final Color successColor = Color(0xFF10B981);
  final Color greyColor = Color(0xFF9CA3AF);

  static const LatLng _egyptCenter = LatLng(30.0444, 31.2357);

  @override
  void initState() {
    super.initState();
    _loadLocations(showSpinner: true);
    // تحديث تلقائي كل 20 ثانية طول ما الشاشة مفتوحة
    _refreshTimer = Timer.periodic(
      const Duration(seconds: 20),
      (_) => _loadLocations(showSpinner: false),
    );
  }

  @override
  void dispose() {
    _refreshTimer?.cancel();
    super.dispose();
  }

  Future<void> _loadLocations({required bool showSpinner}) async {
    if (showSpinner) setState(() => _isLoading = true);
    try {
      final result = await _service.getTeamLocations(
        managerId: widget.user['id'],
      );
      if (!mounted) return;
      if (result['success']) {
        setState(() {
          _employees = result['data'] ?? [];
          _error = null;
        });
      } else {
        setState(() => _error = result['message']);
      }
    } catch (e) {
      if (mounted) setState(() => _error = 'تعذر تحميل المواقع');
    } finally {
      if (mounted && showSpinner) setState(() => _isLoading = false);
    }
  }

  String _timeAgo(String isoUtc) {
    try {
      final updated = DateTime.parse(isoUtc).toLocal();
      final diff = DateTime.now().difference(updated);
      if (diff.inSeconds < 60) return 'الآن';
      if (diff.inMinutes < 60) return 'من ${diff.inMinutes} دقيقة';
      if (diff.inHours < 24) return 'من ${diff.inHours} ساعة';
      return 'من ${diff.inDays} يوم';
    } catch (_) {
      return '';
    }
  }

  List<Marker> _buildMarkers() {
    return _employees.map<Marker>((e) {
      final isActive = e['isRecentlyActive'] == true;
      final userId = e['userId'];
      final isSelected = _selectedUserId == userId;
      final color = isActive ? successColor : greyColor;

      return Marker(
        point: LatLng(
          (e['latitude'] as num).toDouble(),
          (e['longitude'] as num).toDouble(),
        ),
        width: 46,
        height: 46,
        child: GestureDetector(
          onTap: () => _focusOn(e),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: color,
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Colors.white,
                    width: isSelected ? 3 : 2,
                  ),
                  boxShadow: [
                    BoxShadow(
                      color: color.withValues(alpha: 0.4),
                      blurRadius: 8,
                    ),
                  ],
                ),
                child: const Icon(
                  Icons.person,
                  color: Colors.white,
                  size: 18,
                ),
              ),
            ],
          ),
        ),
      );
    }).toList();
  }

  void _focusOn(Map employee) {
    setState(() => _selectedUserId = employee['userId']);
    _mapController.move(
      LatLng(
        (employee['latitude'] as num).toDouble(),
        (employee['longitude'] as num).toDouble(),
      ),
      15,
    );
  }

  LatLng get _initialCenter {
    if (_employees.isEmpty) return _egyptCenter;
    final first = _employees.first;
    return LatLng(
      (first['latitude'] as num).toDouble(),
      (first['longitude'] as num).toDouble(),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        backgroundColor: backgroundColor,
        appBar: AppBar(
          title: const Text(
            'تتبع مواقع الموظفين',
            style: TextStyle(fontFamily: 'Tajawal'),
          ),
          backgroundColor: darkBlue,
          foregroundColor: Colors.white,
          actions: [
            IconButton(
              icon: const Icon(Icons.refresh),
              onPressed: () => _loadLocations(showSpinner: true),
            ),
          ],
        ),
        body: _isLoading
            ? Center(child: CircularProgressIndicator(color: primaryBlue))
            : _error != null
                ? _buildErrorState()
                : _employees.isEmpty
                    ? _buildEmptyState()
                    : Column(
                        children: [
                          Expanded(flex: 3, child: _buildMap()),
                          Expanded(flex: 2, child: _buildEmployeeList()),
                        ],
                      ),
      ),
    );
  }

  Widget _buildMap() {
    return FlutterMap(
      mapController: _mapController,
      options: MapOptions(
        initialCenter: _initialCenter,
        initialZoom: 12,
      ),
      children: [
        TileLayer(
          urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
          userAgentPackageName: 'com.sho2on.mobile',
        ),
        MarkerLayer(markers: _buildMarkers()),
      ],
    );
  }

  Widget _buildEmployeeList() {
    return Container(
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.06),
            blurRadius: 10,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: Column(
        children: [
          Container(
            margin: const EdgeInsets.symmetric(vertical: 8),
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: Colors.grey[300],
              borderRadius: BorderRadius.circular(10),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                Text(
                  'الموظفون (${_employees.length})',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                    color: darkBlue,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: ListView.builder(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              itemCount: _employees.length,
              itemBuilder: (context, index) =>
                  _buildEmployeeTile(_employees[index]),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildEmployeeTile(Map employee) {
    final isActive = employee['isRecentlyActive'] == true;
    final isSelected = _selectedUserId == employee['userId'];
    final color = isActive ? successColor : greyColor;

    return GestureDetector(
      onTap: () => _focusOn(employee),
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: isSelected ? primaryBlue.withValues(alpha: 0.06) : Colors.grey[50],
          borderRadius: BorderRadius.circular(14),
          border: Border.all(
            color: isSelected ? primaryBlue.withValues(alpha: 0.3) : Colors.grey[200]!,
          ),
        ),
        child: Row(
          children: [
            Stack(
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundColor: primaryBlue.withValues(alpha: 0.1),
                  child: Icon(Icons.person, color: primaryBlue),
                ),
                Positioned(
                  bottom: 0,
                  right: 0,
                  child: Container(
                    width: 12,
                    height: 12,
                    decoration: BoxDecoration(
                      color: color,
                      shape: BoxShape.circle,
                      border: Border.all(color: Colors.white, width: 2),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    employee['fullName'] ?? '',
                    style: const TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 13,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    employee['jobTitleName'] ?? employee['departmentName'] ?? '',
                    style: TextStyle(
                      fontFamily: 'Tajawal',
                      fontSize: 11,
                      color: Colors.grey[500],
                    ),
                  ),
                ],
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  isActive ? 'أونلاين' : 'غير نشط',
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 11,
                    fontWeight: FontWeight.bold,
                    color: color,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  _timeAgo(employee['updatedAtUtc'] ?? ''),
                  style: TextStyle(
                    fontFamily: 'Tajawal',
                    fontSize: 10,
                    color: Colors.grey[400],
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildErrorState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.error_outline, color: Colors.grey[400], size: 48),
            const SizedBox(height: 12),
            Text(
              _error ?? 'حدث خطأ',
              textAlign: TextAlign.center,
              style: const TextStyle(fontFamily: 'Tajawal'),
            ),
            const SizedBox(height: 16),
            ElevatedButton(
              onPressed: () => _loadLocations(showSpinner: true),
              child: const Text('إعادة المحاولة',
                  style: TextStyle(fontFamily: 'Tajawal')),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildEmptyState() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.location_off, color: Colors.grey[400], size: 48),
            const SizedBox(height: 12),
            const Text(
              'لا توجد مواقع متاحة لموظفينك حالياً\nهتظهر أول ما حد منهم يسجل حضور',
              textAlign: TextAlign.center,
              style: TextStyle(fontFamily: 'Tajawal', color: Colors.grey),
            ),
          ],
        ),
      ),
    );
  }
}
