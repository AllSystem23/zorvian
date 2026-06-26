import 'package:flutter/material.dart';
import '../ds.dart';

/// A reusable period selector dropdown with built-in dark/light mode styling.
///
/// Usage:
/// ```dart
/// ZPeriodDropdown(
///   value: _selectedPeriod,
///   onChanged: (v) => setState(() => _selectedPeriod = v!),
/// )
/// ```
class ZPeriodDropdown extends StatelessWidget {
  final String value;
  final ValueChanged<String?> onChanged;
  final List<String> items;
  final IconData icon;
  final EdgeInsetsGeometry? padding;

  const ZPeriodDropdown({
    super.key,
    required this.value,
    required this.onChanged,
    this.items = const ['Hoy', 'Esta Semana', 'Este Mes', 'Este Trimestre'],
    this.icon = Icons.calendar_today_outlined,
    this.padding,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Container(
      padding: padding ?? const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: isDark ? ZColors.darkBorder : ZColors.border),
        boxShadow: [ZShadows.sm],
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 16, color: ZColors.neutral500),
          const SizedBox(width: 12),
          DropdownButton<String>(
            value: value,
            underline: const SizedBox(),
            isDense: true,
            icon: Icon(
              Icons.keyboard_arrow_down,
              size: 18,
              color: isDark ? ZColors.neutral300 : ZColors.neutral600,
            ),
            dropdownColor: Theme.of(context).colorScheme.surface,
            style: ZTypography.labelMedium.copyWith(
              fontWeight: FontWeight.w600,
              color: isDark ? ZColors.neutral100 : ZColors.neutral800,
            ),
            items: items
                .map((s) => DropdownMenuItem(value: s, child: Text(s)))
                .toList(),
            onChanged: onChanged,
          ),
        ],
      ),
    );
  }
}
