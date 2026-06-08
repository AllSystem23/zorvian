import 'package:flutter/material.dart';
import '../ds.dart';

final class CalendarDay {
  final int day;
  final bool isSelected;
  final bool isToday;
  final bool hasEvent;
  final Color? eventColor;
  final String? label;

  const CalendarDay({
    required this.day,
    this.isSelected = false,
    this.isToday = false,
    this.hasEvent = false,
    this.eventColor,
    this.label,
  });
}

class ZCalendar extends StatelessWidget {
  final int year;
  final int month;
  final List<CalendarDay>? days;
  final ValueChanged<int>? onDaySelected;
  final int? selectedDay;

  static const _monthNames = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre'];

  const ZCalendar({
    super.key,
    required this.year,
    required this.month,
    this.days,
    this.onDaySelected,
    this.selectedDay,
  });

  int get _daysInMonth {
    return DateTime(year, month + 1, 0).day;
  }

  int get _startWeekday {
    final first = DateTime(year, month, 1);
    return first.weekday % 7;
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _buildHeader(),
        const SizedBox(height: ZSpacing.sm),
        _buildWeekdays(),
        const SizedBox(height: 4),
        _buildGrid(),
      ],
    );
  }

  Widget _buildHeader() {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text('${_monthNames[month - 1]} $year', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: ZColors.neutral900)),
        Row(
          children: [
            IconButton(icon: const Icon(Icons.chevron_left, size: 20), onPressed: null, tooltip: 'Mes anterior'),
            const SizedBox(width: 8),
            IconButton(icon: const Icon(Icons.chevron_right, size: 20), onPressed: null, tooltip: 'Mes siguiente'),
          ],
        ),
      ],
    );
  }

  Widget _buildWeekdays() {
    const weekdays = ['Dom','Lun','Mar','Mié','Jue','Vie','Sáb'];
    return Row(
      children: weekdays.map((d) => Expanded(
        child: Center(child: Text(d, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: ZColors.neutral400))),
      )).toList(),
    );
  }

  Widget _buildGrid() {
    final dayMap = <int, CalendarDay>{};
    if (days != null) {
      for (final d in days!) {
        dayMap[d.day] = d;
      }
    }

    final cells = <Widget>[];
    for (var i = 0; i < _startWeekday; i++) {
      cells.add(const Expanded(child: SizedBox(height: 48)));
    }

    for (var day = 1; day <= _daysInMonth; day++) {
      final cd = dayMap[day];
      final isSelected = day == selectedDay;
      final isToday = cd?.isToday ?? false;
      final hasEvent = cd?.hasEvent ?? false;
      final eventColor = cd?.eventColor;

      cells.add(Expanded(
        child: GestureDetector(
          onTap: onDaySelected != null ? () => onDaySelected!(day) : null,
          child: Container(
            height: 48,
            decoration: BoxDecoration(
              color: isSelected ? ZColors.brandAccent.withAlpha(30) : null,
              shape: BoxShape.circle,
            ),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  '$day',
                  style: TextStyle(
                    fontSize: 13,
                    fontWeight: isToday ? FontWeight.w700 : FontWeight.w500,
                    color: isToday ? ZColors.brandAccent : ZColors.neutral900,
                  ),
                ),
                if (hasEvent)
                  Container(
                    width: 5, height: 5,
                    decoration: BoxDecoration(
                      color: eventColor ?? ZColors.brandAccent,
                      shape: BoxShape.circle,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ));
    }

    return Wrap(children: cells);
  }
}
