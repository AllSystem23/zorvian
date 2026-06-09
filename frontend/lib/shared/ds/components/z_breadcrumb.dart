import 'package:flutter/material.dart';
import '../ds.dart';

/// Breadcrumb navigation component for contextual navigation
class ZBreadcrumb extends StatelessWidget {
  final List<ZBreadcrumbItem> items;

  const ZBreadcrumb({super.key, required this.items});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      child: Row(
        children: [
          for (int i = 0; i < items.length; i++) ...[
            if (i > 0)
              const Padding(
                padding: EdgeInsets.symmetric(horizontal: ZSpacing.xs),
                child: Icon(Icons.chevron_right, size: 16, color: ZColors.neutral400),
              ),
            if (i == items.length - 1)
              Text(
                items[i].label,
                style: theme.textTheme.bodySmall?.copyWith(
                  fontWeight: FontWeight.w600,
                  color: theme.colorScheme.primary,
                ),
              )
            else
              InkWell(
                onTap: items[i].onTap,
                child: Text(
                  items[i].label,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: ZColors.neutral500,
                    decoration: TextDecoration.underline,
                    decorationColor: ZColors.neutral300,
                  ),
                ),
              ),
          ],
        ],
      ),
    );
  }
}

class ZBreadcrumbItem {
  final String label;
  final VoidCallback? onTap;

  const ZBreadcrumbItem({required this.label, this.onTap});
}