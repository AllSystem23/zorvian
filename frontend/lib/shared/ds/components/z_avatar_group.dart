import 'package:flutter/material.dart';
import '../ds.dart';

/// Avatar group component for showing multiple users
class ZAvatarGroup extends StatelessWidget {
  final List<ZAvatarData> avatars;
  final int maxVisible;
  final double size;
  final double overlap;

  const ZAvatarGroup({
    super.key,
    required this.avatars,
    this.maxVisible = 4,
    this.size = 32,
    this.overlap = 0.6,
  });

  @override
  Widget build(BuildContext context) {
    final visible = avatars.take(maxVisible).toList();
    final overflow = avatars.length - visible.length;

    return SizedBox(
      height: size + 4,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          for (int i = 0; i < visible.length; i++)
            Positioned(
              left: i * (size * (1 - overlap)),
              child: _buildAvatar(context, visible[i]),
            ),
          if (overflow > 0)
            Positioned(
              left: visible.length * (size * (1 - overlap)),
              child: _buildOverflowAvatar(context, overflow),
            ),
        ],
      ),
    );
  }

  Widget _buildAvatar(BuildContext context, ZAvatarData avatar) {
    return Container(
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: Theme.of(context).colorScheme.surface, width: 2),
      ),
      child: CircleAvatar(
        radius: size / 2,
        backgroundColor: avatar.color ?? _colorFromName(avatar.name),
        backgroundImage: avatar.imageUrl != null ? NetworkImage(avatar.imageUrl!) : null,
        child: avatar.imageUrl == null
            ? Text(_initials(avatar.name), style: TextStyle(color: Colors.white, fontSize: size / 2.5, fontWeight: FontWeight.w600))
            : null,
      ),
    );
  }

  Widget _buildOverflowAvatar(BuildContext context, int count) {
    return Container(
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        border: Border.all(color: Theme.of(context).colorScheme.surface, width: 2),
      ),
      child: CircleAvatar(
        radius: size / 2,
        backgroundColor: ZColors.neutral500,
        child: Text('+$count', style: TextStyle(color: Colors.white, fontSize: size / 3, fontWeight: FontWeight.w600)),
      ),
    );
  }

  String _initials(String name) {
    final parts = name.trim().split(' ');
    if (parts.length >= 2) return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
    return name.isEmpty ? '?' : name[0].toUpperCase();
  }

  Color _colorFromName(String name) {
    final colors = [
      Colors.blue, Colors.purple, Colors.teal, Colors.orange,
      Colors.pink, Colors.green, Colors.indigo, Colors.cyan,
    ];
    final hash = name.codeUnits.fold(0, (a, b) => a + b);
    return colors[hash % colors.length];
  }
}

class ZAvatarData {
  final String name;
  final String? imageUrl;
  final Color? color;

  const ZAvatarData({required this.name, this.imageUrl, this.color});
}

/// Single avatar with name (for comments, mentions, etc.)
class ZAvatarWithName extends StatelessWidget {
  final String name;
  final String? imageUrl;
  final String? subtitle;
  final Color? color;
  final double size;
  final VoidCallback? onTap;

  const ZAvatarWithName({
    super.key,
    required this.name,
    this.imageUrl,
    this.subtitle,
    this.color,
    this.size = 40,
    this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(ZRadii.md),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 4),
        child: Row(
          children: [
            CircleAvatar(
              radius: size / 2,
              backgroundColor: color ?? ZColors.brandPrimary,
              backgroundImage: imageUrl != null ? NetworkImage(imageUrl!) : null,
              child: imageUrl == null
                  ? Text(_initials(name), style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600))
                  : null,
            ),
            const SizedBox(width: ZSpacing.sm),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(name, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                  if (subtitle != null)
                    Text(subtitle!, style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
                ],
              ),
            ),
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
