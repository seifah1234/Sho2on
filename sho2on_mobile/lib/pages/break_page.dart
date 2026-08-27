import 'package:flutter/material.dart';
import '../services/break_service.dart';

class BreakPage extends StatefulWidget {
  final Map user;
  const BreakPage({super.key, required this.user});

  @override
  _BreakPageState createState() => _BreakPageState();
}

class _BreakPageState extends State<BreakPage> {
  final BreakService _breakService = BreakService();

  bool _isLoading = true;
  bool _isActing = false;

  bool _hasBreak = false;
  String? _breakName;
  String? _breakType; // "Fixed" أو "Flexible"
  int? _durationMinutes;

  bool _isActive = false;
  DateTime? _activeStartTime;

  List<dynamic> _todayLogs = [];

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  int get _userId => widget.user['id'];

  Future<void> _loadData() async {
    setState(() => _isLoading = true);
    try {
      final typeData = await _breakService.getMyBreakType(_userId);
      final activeData = await _breakService.getActiveBreak(_userId);
      final logs = await _breakService.getTodayBreaks(_userId);

      setState(() {
        _hasBreak = typeData['hasBreak'] ?? typeData['HasBreak'] ?? false;
        _breakName = typeData['name'] ?? typeData['Name'];
        _breakType = typeData['type'] ?? typeData['Type'];
        _durationMinutes = typeData['durationMinutes'] ?? typeData['DurationMinutes'];

        _isActive = activeData['isActive'] ?? activeData['IsActive'] ?? false;
        final startStr = activeData['startTime'] ?? activeData['StartTime'];
        _activeStartTime = startStr != null ? DateTime.tryParse(startStr) : null;

        _todayLogs = logs;
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('حدث خطأ أثناء تحميل البيانات: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  Future<void> _startBreak() async {
    setState(() => _isActing = true);
    try {
      await _breakService.startBreak(_userId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('تم بدء الاستراحة')),
        );
      }
      await _loadData();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('$e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isActing = false);
    }
  }

  Future<void> _endBreak() async {
    setState(() => _isActing = true);
    try {
      final result = await _breakService.endBreak(_userId);
      if (mounted) {
        final exceeded = result['exceededLimit'] ?? result['ExceededLimit'] ?? false;
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(exceeded
                ? 'تم إنهاء الاستراحة — تجاوزت المدة المسموحة'
                : 'تم إنهاء الاستراحة'),
            backgroundColor: exceeded ? Colors.orange : Colors.green,
          ),
        );
      }
      await _loadData();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('$e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isActing = false);
    }
  }

  String _formatDuration(Duration d) {
    final h = d.inHours;
    final m = d.inMinutes.remainder(60);
    if (h > 0) return '$h س $m د';
    return '$m د';
  }

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.rtl,
      child: Scaffold(
        appBar: AppBar(title: const Text('البريك')),
        body: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : RefreshIndicator(
                onRefresh: _loadData,
                child: ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    if (!_hasBreak)
                      Card(
                        color: Colors.grey.shade100,
                        child: const Padding(
                          padding: EdgeInsets.all(20),
                          child: Text(
                            'لا يوجد نظام استراحة مربوط بحسابك حاليًا. تواصل مع الموارد البشرية.',
                            textAlign: TextAlign.center,
                          ),
                        ),
                      )
                    else if (_breakType == 'Fixed')
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(20),
                          child: Column(
                            children: [
                              const Icon(Icons.schedule, size: 40, color: Colors.blueGrey),
                              const SizedBox(height: 10),
                              Text(_breakName ?? '', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                              const SizedBox(height: 6),
                              const Text(
                                'نظامك معاد ثابت، لا يحتاج تسجيل بدء/إنهاء يدوي.',
                                textAlign: TextAlign.center,
                                style: TextStyle(color: Colors.grey),
                              ),
                            ],
                          ),
                        ),
                      )
                    else
                      _buildFlexibleBreakCard(),

                    const SizedBox(height: 20),
                    const Text('سجل اليوم', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
                    const SizedBox(height: 10),
                    if (_todayLogs.isEmpty)
                      const Padding(
                        padding: EdgeInsets.symmetric(vertical: 20),
                        child: Center(child: Text('لا توجد استراحات اليوم', style: TextStyle(color: Colors.grey))),
                      )
                    else
                      ..._todayLogs.map((log) => _buildLogTile(log)),
                  ],
                ),
              ),
      ),
    );
  }

  Widget _buildFlexibleBreakCard() {
    return Card(
      color: _isActive ? Colors.orange.shade50 : Colors.green.shade50,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            Icon(
              _isActive ? Icons.hourglass_bottom : Icons.coffee,
              size: 44,
              color: _isActive ? Colors.orange : Colors.green,
            ),
            const SizedBox(height: 10),
            Text(_breakName ?? 'استراحة', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            if (_durationMinutes != null)
              Padding(
                padding: const EdgeInsets.only(top: 4),
                child: Text('المدة المسموحة: $_durationMinutes دقيقة', style: const TextStyle(color: Colors.grey)),
              ),
            const SizedBox(height: 16),
            if (_isActive && _activeStartTime != null)
              _ActiveBreakTimer(startTime: _activeStartTime!),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: _isActing ? null : (_isActive ? _endBreak : _startBreak),
                icon: Icon(_isActive ? Icons.stop_circle : Icons.play_circle),
                label: Text(_isActing
                    ? 'جاري التنفيذ...'
                    : (_isActive ? 'إنهاء الاستراحة' : 'بدء الاستراحة')),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _isActive ? Colors.red : Colors.green,
                  foregroundColor: Colors.white,
                  padding: const EdgeInsets.symmetric(vertical: 14),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildLogTile(dynamic log) {
    final start = DateTime.tryParse(log['startTime'] ?? log['StartTime'] ?? '');
    final endStr = log['endTime'] ?? log['EndTime'];
    final end = endStr != null ? DateTime.tryParse(endStr) : null;
    final exceeded = log['exceededLimit'] ?? log['ExceededLimit'] ?? false;

    String durationText = '—';
    if (start != null && end != null) {
      durationText = _formatDuration(end.difference(start));
    } else if (start != null) {
      durationText = 'جارية...';
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        leading: Icon(
          end == null ? Icons.hourglass_bottom : Icons.check_circle,
          color: exceeded ? Colors.orange : (end == null ? Colors.blue : Colors.green),
        ),
        title: Text(start != null
            ? '${start.hour.toString().padLeft(2, '0')}:${start.minute.toString().padLeft(2, '0')}'
                ' — ${end != null ? '${end.hour.toString().padLeft(2, '0')}:${end.minute.toString().padLeft(2, '0')}' : 'جارية'}'
            : '—'),
        subtitle: exceeded ? const Text('تجاوز المدة المسموحة', style: TextStyle(color: Colors.orange)) : null,
        trailing: Text(durationText, style: const TextStyle(fontWeight: FontWeight.bold)),
      ),
    );
  }
}

// عداد بسيط لعرض الوقت المنقضي من بداية الاستراحة الحالية
class _ActiveBreakTimer extends StatefulWidget {
  final DateTime startTime;
  const _ActiveBreakTimer({required this.startTime});

  @override
  State<_ActiveBreakTimer> createState() => _ActiveBreakTimerState();
}

class _ActiveBreakTimerState extends State<_ActiveBreakTimer> {
  late Duration _elapsed;
  late final Stream<int> _ticker;

  @override
  void initState() {
    super.initState();
    _elapsed = DateTime.now().difference(widget.startTime);
    _ticker = Stream.periodic(const Duration(seconds: 1), (i) => i);
    _ticker.listen((_) {
      if (mounted) {
        setState(() => _elapsed = DateTime.now().difference(widget.startTime));
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final m = _elapsed.inMinutes.remainder(60).toString().padLeft(2, '0');
    final s = _elapsed.inSeconds.remainder(60).toString().padLeft(2, '0');
    final h = _elapsed.inHours;
    return Text(
      h > 0 ? '$h:$m:$s' : '$m:$s',
      style: const TextStyle(fontSize: 28, fontWeight: FontWeight.bold, fontFeatures: [FontFeature.tabularFigures()]),
    );
  }
}