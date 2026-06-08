import 'package:flutter/material.dart';
import '../ds.dart';

final class TimelineItem {
  final String title;
  final String? subtitle;
  final String? date;
  final IconData? icon;
  final Color? iconColor;
  final bool isLast;

  const TimelineItem({
    required this.title,
    this.subtitle,
    this.date,
    this.icon,
    this.iconColor,
    this.isLast = false,
  });
}

class ZTimeline extends StatelessWidget {
  final List<TimelineItem> items;

  const ZTimeline({super.key, required this.items});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: items.map((item) => _buildItem(item)).toList(),
    );
  }

  Widget _buildItem(TimelineItem item) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 40,
            child: Column(
              children: [
                Container(
                  width: 32, height: 32,
                  decoration: BoxDecoration(
                    color: (item.iconColor ?? ZColors.brandAccent).withAlpha(25),
                    shape: BoxShape.circle,
                  ),
                  child: Icon(item.icon ?? Icons.circle, size: 14, color: item.iconColor ?? ZColors.brandAccent),
                ),
                if (!item.isLast)
                  Expanded(
                    child: Container(width: 2, color: ZColors.neutral200),
                  ),
              ],
            ),
          ),
          const SizedBox(width: ZSpacing.sm),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: ZSpacing.xl),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (item.date != null)
                    Text(item.date!, style: const TextStyle(fontSize: 11, color: ZColors.neutral400)),
                  const SizedBox(height: 2),
                  Text(item.title, style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500, color: ZColors.neutral900)),
                  if (item.subtitle != null) ...[
                    const SizedBox(height: 2),
                    Text(item.subtitle!, style: const TextStyle(fontSize: 12, color: ZColors.neutral500)),
                  ],
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
