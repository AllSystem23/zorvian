import 'package:flutter/material.dart';
import '../ds.dart';

/// Date range filter widget with quick presets
class ZDateRangeFilter extends StatefulWidget {
  final DateTimeRange? initialRange;
  final ValueChanged<DateTimeRange> onChanged;

  const ZDateRangeFilter({
    super.key,
    this.initialRange,
    required this.onChanged,
  });

  @override
  State<ZDateRangeFilter> createState() => _ZDateRangeFilterState();
}

class _ZDateRangeFilterState extends State<ZDateRangeFilter> {
  late DateTimeRange _range;

  @override
  void initState() {
    super.initState();
    _range = widget.initialRange ?? _preset(7); // Last 7 days default
  }

  DateTimeRange _preset(int days) {
    final end = DateTime.now();
    final start = end.subtract(Duration(days: days));
    return DateTimeRange(start: start, end: end);
  }

  void _applyPreset(int days) {
    setState(() => _range = _preset(days));
    widget.onChanged(_range);
  }

  void _applyCurrentMonth() {
    final now = DateTime.now();
    setState(() => _range = DateTimeRange(
      start: DateTime(now.year, now.month, 1),
      end: DateTime(now.year, now.month + 1, 0),
    ));
    widget.onChanged(_range);
  }

  void _applyLastMonth() {
    final now = DateTime.now();
    final lastMonth = DateTime(now.year, now.month - 1, 1);
    setState(() => _range = DateTimeRange(
      start: lastMonth,
      end: DateTime(now.year, now.month, 0),
    ));
    widget.onChanged(_range);
  }

  Future<void> _pickRange() async {
    final picked = await showDateRangePicker(
      context: context,
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
      initialDateRange: _range,
    );
    if (picked != null) {
      setState(() => _range = picked);
      widget.onChanged(picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(ZSpacing.md),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.date_range, size: 20, color: theme.colorScheme.primary),
              const SizedBox(width: ZSpacing.sm),
              Text('Período', style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
              const Spacer(),
              TextButton.icon(
                onPressed: _pickRange,
                icon: const Icon(Icons.edit_calendar, size: 18),
                label: const Text('Personalizado'),
              ),
            ],
          ),
          const SizedBox(height: ZSpacing.sm),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _buildChip('Hoy', () => _applyPreset(0)),
              _buildChip('7 días', () => _applyPreset(7)),
              _buildChip('30 días', () => _applyPreset(30)),
              _buildChip('90 días', () => _applyPreset(90)),
              _buildChip('Este mes', _applyCurrentMonth),
              _buildChip('Mes anterior', _applyLastMonth),
            ],
          ),
          const SizedBox(height: ZSpacing.md),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: theme.colorScheme.primaryContainer.withValues(alpha: 0.2),
              borderRadius: BorderRadius.circular(ZRadii.sm),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.calendar_today, size: 14, color: theme.colorScheme.primary),
                const SizedBox(width: 6),
                Text(
                  '${_formatDate(_range.start)} → ${_formatDate(_range.end)}',
                  style: theme.textTheme.bodySmall?.copyWith(color: theme.colorScheme.primary, fontWeight: FontWeight.w500),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildChip(String label, VoidCallback onPressed) {
    return ActionChip(
      label: Text(label, style: const TextStyle(fontSize: 12)),
      onPressed: onPressed,
    );
  }

  String _formatDate(DateTime d) {
    return '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
  }
}
