import 'package:flutter/material.dart';
import '../ds.dart';

/// Helper info tooltip that shows a popup with detailed info
class ZInfoTooltip extends StatelessWidget {
  final String message;
  final String? title;
  final IconData icon;
  final Color? color;
  final TooltipTriggerMode triggerMode;

  const ZInfoTooltip({
    super.key,
    required this.message,
    this.title,
    this.icon = Icons.info_outline,
    this.color,
    this.triggerMode = TooltipTriggerMode.tap,
  });

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: message,
      triggerMode: triggerMode,
      preferBelow: true,
      decoration: BoxDecoration(
        color: ZColors.neutral800,
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      textStyle: const TextStyle(color: Colors.white, fontSize: 12),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      child: Icon(icon, size: 18, color: color ?? ZColors.neutral400),
    );
  }
}

/// Help icon button that opens a detailed info dialog
class ZHelpButton extends StatelessWidget {
  final String title;
  final String content;
  final List<({String label, String description})>? sections;
  final String? docsUrl;
  final IconData icon;

  const ZHelpButton({
    super.key,
    required this.title,
    required this.content,
    this.sections,
    this.docsUrl,
    this.icon = Icons.help_outline,
  });

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: Icon(icon, size: 20),
      tooltip: 'Ayuda',
      onPressed: () => _showHelpDialog(context),
    );
  }

  void _showHelpDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (_) => AlertDialog(
        title: Row(
          children: [
            Icon(icon, color: Theme.of(context).colorScheme.primary),
            const SizedBox(width: 8),
            Expanded(child: Text(title)),
          ],
        ),
        content: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 500, maxHeight: 500),
          child: SingleChildScrollView(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(content, style: Theme.of(context).textTheme.bodyMedium),
                if (sections != null) ...[
                  const SizedBox(height: ZSpacing.lg),
                  for (final section in sections!) ...[
                    Text(section.label, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w600)),
                    const SizedBox(height: 4),
                    Text(section.description, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
                    const SizedBox(height: ZSpacing.md),
                  ],
                ],
              ],
            ),
          ),
        ),
        actions: [
          if (docsUrl != null)
            TextButton.icon(
              icon: const Icon(Icons.open_in_new, size: 16),
              label: const Text('Ver documentación'),
              onPressed: () {
                // Open docsUrl
              },
            ),
          TextButton(onPressed: () => Navigator.of(context).pop(), child: const Text('Cerrar')),
        ],
      ),
    );
  }
}

/// Form field with help text and tooltip
class ZFieldWithHelp extends StatelessWidget {
  final String label;
  final String? hint;
  final String? helpText;
  final Widget child;

  const ZFieldWithHelp({
    super.key,
    required this.label,
    required this.child,
    this.hint,
    this.helpText,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Text(label, style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500)),
            if (helpText != null) ...[
              const SizedBox(width: 4),
              ZInfoTooltip(message: helpText!),
            ],
          ],
        ),
        const SizedBox(height: 6),
        child,
        if (hint != null) ...[
          const SizedBox(height: 4),
          Text(hint!, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
        ],
      ],
    );
  }
}
