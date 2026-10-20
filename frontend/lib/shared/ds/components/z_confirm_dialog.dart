import 'package:flutter/material.dart';
import '../ds.dart';

/// Reusable confirmation dialog for destructive actions
class ZConfirmDialog extends StatelessWidget {
  final String title;
  final String message;
  final String confirmLabel;
  final String cancelLabel;
  final IconData? icon;
  final Color? confirmColor;
  final bool isDestructive;

  const ZConfirmDialog({
    super.key,
    required this.title,
    required this.message,
    this.confirmLabel = 'Confirmar',
    this.cancelLabel = 'Cancelar',
    this.icon,
    this.confirmColor,
    this.isDestructive = false,
  });

  /// Shows a confirm dialog and returns true if confirmed.
  ///
  /// En teléfono se presenta como hoja inferior a pantalla completa
  /// (`showZAdaptiveDialog`), no como `AlertDialog` angosto.
  static Future<bool> show(
    BuildContext context, {
    required String title,
    required String message,
    String confirmLabel = 'Confirmar',
    String cancelLabel = 'Cancelar',
    IconData? icon,
    Color? confirmColor,
    bool isDestructive = false,
  }) async {
    final result = await showZAdaptiveDialog<bool>(
      context,
      builder: (_) => ZConfirmDialog(
        title: title,
        message: message,
        confirmLabel: confirmLabel,
        cancelLabel: cancelLabel,
        icon: icon,
        confirmColor: confirmColor,
        isDestructive: isDestructive,
      ),
    );
    return result ?? false;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final effectiveConfirmColor = confirmColor ?? (isDestructive ? ZColors.danger : theme.colorScheme.primary);

    return ZResponsiveDialog(
      icon: icon,
      iconColor: effectiveConfirmColor,
      title: title,
      maxWidth: 460,
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(false),
          child: Text(cancelLabel),
        ),
        FilledButton(
          onPressed: () => Navigator.of(context).pop(true),
          style: FilledButton.styleFrom(
            backgroundColor: effectiveConfirmColor,
          ),
          child: Text(confirmLabel),
        ),
      ],
      child: Text(
        message,
        style: ZTypography.bodyMedium.copyWith(
          color: theme.brightness == Brightness.dark
              ? ZColors.neutral300
              : ZColors.neutral600,
        ),
      ),
    );
  }
}