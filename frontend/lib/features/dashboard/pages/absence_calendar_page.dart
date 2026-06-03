import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/calendar_provider.dart';

class AbsenceCalendarPage extends ConsumerStatefulWidget {
  const AbsenceCalendarPage({super.key});

  @override
  ConsumerState<AbsenceCalendarPage> createState() => _AbsenceCalendarPageState();
}

class _AbsenceCalendarPageState extends ConsumerState<AbsenceCalendarPage> {
  DateTime _currentMonth = DateTime.now();

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(calendarProvider.notifier).load());
  }

  void _prevMonth() => setState(() => _currentMonth = DateTime(_currentMonth.year, _currentMonth.month - 1));
  void _nextMonth() => setState(() => _currentMonth = DateTime(_currentMonth.year, _currentMonth.month + 1));

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cal = ref.watch(calendarProvider);
    final events = cal.events;

    final firstDay = DateTime(_currentMonth.year, _currentMonth.month, 1);
    final lastDay = DateTime(_currentMonth.year, _currentMonth.month + 1, 0);
    final weeks = _buildWeeks(firstDay, lastDay);

    return Scaffold(
      appBar: AppBar(title: const Text('Calendario de Ausencias')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                IconButton(icon: const Icon(Icons.chevron_left), onPressed: _prevMonth),
                Text(
                  '${_monthName(_currentMonth.month)} ${_currentMonth.year}',
                  style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.bold),
                ),
                IconButton(icon: const Icon(Icons.chevron_right), onPressed: _nextMonth),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceAround,
              children: ['Lu', 'Ma', 'Mi', 'Ju', 'Vi', 'Sa', 'Do']
                  .map((d) => SizedBox(width: 36, child: Text(d, textAlign: TextAlign.center, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12))))
                  .toList(),
            ),
          ),
          const SizedBox(height: 4),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: ListView.builder(
                itemCount: weeks.length,
                itemBuilder: (_, i) => Padding(
                  padding: const EdgeInsets.symmetric(vertical: 2),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: weeks[i].map((day) {
                      if (day == null) return const SizedBox(width: 36);
                      final hasEvent = events.any((e) {
                        final s = DateTime.parse(e.startDate);
                        final end = DateTime.parse(e.endDate);
                        return _isSameDay(day, s) || _isSameDay(day, end) || (day.isAfter(s) && day.isBefore(end));
                      });
                      final isToday = _isSameDay(day, DateTime.now());
                      return GestureDetector(
                        onTap: hasEvent ? () => _showDayEvents(context, day, events) : null,
                        child: Container(
                          width: 36,
                          height: 36,
                          decoration: BoxDecoration(
                            color: hasEvent ? const Color(0xFFD97706).withValues(alpha: 0.2) : null,
                            shape: BoxShape.circle,
                            border: isToday ? Border.all(color: theme.colorScheme.primary, width: 2) : null,
                          ),
                          child: Center(child: Text('${day.day}', style: TextStyle(fontSize: 13, fontWeight: hasEvent ? FontWeight.bold : null))),
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ),
            ),
          ),
          if (events.isNotEmpty)
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3)),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Ausencias este mes', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  ...events.take(5).map((e) => ListTile(
                    dense: true,
                    leading: Icon(e.type == 'vacation' ? Icons.beach_access : Icons.description, size: 20, color: const Color(0xFFD97706)),
                    title: Text(e.employeeName, style: const TextStyle(fontSize: 13)),
                    subtitle: Text('${e.startDate} - ${e.endDate}', style: const TextStyle(fontSize: 11)),
                  )),
                ],
              ),
            ),
        ],
      ),
    );
  }

  List<List<DateTime?>> _buildWeeks(DateTime first, DateTime last) {
    final weeks = <List<DateTime?>>[];
    var start = first.subtract(Duration(days: (first.weekday + 6) % 7));
    while (start.isBefore(last) || start.month == _currentMonth.month) {
      weeks.add(List.generate(7, (i) => start.add(Duration(days: i))));
      start = start.add(const Duration(days: 7));
      if (weeks.length > 5) break;
    }
    return weeks;
  }

  bool _isSameDay(DateTime a, DateTime b) => a.year == b.year && a.month == b.month && a.day == b.day;

  String _monthName(int m) => ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre'][m - 1];

  void _showDayEvents(BuildContext context, DateTime day, List<CalendarEvent> events) {
    final dayEvents = events.where((e) {
      final s = DateTime.parse(e.startDate);
      final end = DateTime.parse(e.endDate);
      return _isSameDay(day, s) || _isSameDay(day, end) || (day.isAfter(s) && day.isBefore(end));
    }).toList();

    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: Text('${day.day} de ${_monthName(day.month)}'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: dayEvents.map((e) => ListTile(
            dense: true,
            title: Text(e.employeeName),
            subtitle: Text('${e.startDate} - ${e.endDate}'),
          )).toList(),
        ),
        actions: [TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cerrar'))],
      ),
    );
  }
}
