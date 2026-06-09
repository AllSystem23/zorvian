import 'package:flutter/material.dart';
import 'package:zorvian/core/utils/formatters.dart';
import '../ds.dart';

/// Activity feed item
class ZActivityItem {
  final String id;
  final String userName;
  final String? userAvatar;
  final String action;
  final String? entityType;
  final String? entityId;
  final String? entityName;
  final DateTime timestamp;
  final IconData icon;
  final Color? iconColor;
  final Map<String, dynamic>? metadata;

  const ZActivityItem({
    required this.id,
    required this.userName,
    this.userAvatar,
    required this.action,
    this.entityType,
    this.entityId,
    this.entityName,
    required this.timestamp,
    this.icon = Icons.history,
    this.iconColor,
    this.metadata,
  });
}

/// Activity feed widget
class ZActivityFeed extends StatelessWidget {
  final List<ZActivityItem> activities;
  final bool showUserAvatar;
  final bool compact;
  final VoidCallback? onLoadMore;
  final bool hasMore;

  const ZActivityFeed({
    super.key,
    required this.activities,
    this.showUserAvatar = true,
    this.compact = false,
    this.onLoadMore,
    this.hasMore = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    if (activities.isEmpty) {
      return const ZEmptyState(
        icon: Icons.history,
        title: 'Sin actividad',
        subtitle: 'No hay actividad reciente',
      );
    }

    return Column(
      children: [
        ListView.separated(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          itemCount: activities.length,
          separatorBuilder: (_, _) => Divider(height: 1, color: ZColors.neutral100),
          itemBuilder: (_, index) => _buildItem(theme, activities[index]),
        ),
        if (hasMore && onLoadMore != null)
          Padding(
            padding: const EdgeInsets.all(ZSpacing.md),
            child: TextButton(onPressed: onLoadMore, child: const Text('Cargar más')),
          ),
      ],
    );
  }

  Widget _buildItem(ThemeData theme, ZActivityItem item) {
    return InkWell(
      onTap: () {
        // Navigate to entity
      },
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (showUserAvatar) ...[
              CircleAvatar(
                radius: 18,
                backgroundColor: theme.colorScheme.primaryContainer,
                backgroundImage: item.userAvatar != null ? NetworkImage(item.userAvatar!) : null,
                child: item.userAvatar == null
                    ? Text(_initials(item.userName), style: TextStyle(fontSize: 12, color: theme.colorScheme.primary, fontWeight: FontWeight.bold))
                    : null,
              ),
              const SizedBox(width: ZSpacing.sm),
            ] else
              Container(
                width: 36, height: 36,
                decoration: BoxDecoration(
                  color: (item.iconColor ?? theme.colorScheme.primary).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(ZRadii.md),
                ),
                child: Icon(item.icon, size: 20, color: item.iconColor ?? theme.colorScheme.primary),
              ),
            const SizedBox(width: ZSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text.rich(
                    TextSpan(
                      children: [
                        TextSpan(text: item.userName, style: const TextStyle(fontWeight: FontWeight.w600)),
                        TextSpan(text: ' ${item.action} '),
                        if (item.entityName != null)
                          TextSpan(text: item.entityName, style: const TextStyle(fontWeight: FontWeight.w500, color: ZColors.brandPrimary)),
                      ],
                    ),
                    style: theme.textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 2),
                  Text(
                    ZFormatters.relative(item.timestamp),
                    style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral400),
                  ),
                ],
              ),
            ),
            if (!compact)
              Icon(Icons.chevron_right, size: 18, color: ZColors.neutral300),
          ],
        ),
      ),
    );
  }

  String _initials(String name) {
    final parts = name.trim().split(' ');
    if (parts.length >= 2) return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    return name.isEmpty ? '?' : name[0].toUpperCase();
  }
}
