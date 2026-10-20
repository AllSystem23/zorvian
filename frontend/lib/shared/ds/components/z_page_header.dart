import 'package:flutter/material.dart';

import '../tokens/breakpoints.dart';
import '../tokens/spacing.dart';
import '../tokens/typography.dart';

/// Encabezado de página con acciones primarias.
///
/// En **teléfono** el título ocupa todo el ancho (puede partirse en dos líneas)
/// y las acciones bajan a su propia fila: un título como "Nueva Solicitud de
/// Cotización" junto a un botón "Guardar" no cabe en 360 px y desbordaba.
/// Desde 600 px el título y las acciones comparten la misma fila.
class ZPageHeader extends StatelessWidget {
  final String title;
  final String? subtitle;

  /// Adorno a la izquierda del título (icono en un recuadro con gradiente).
  final Widget? leading;
  final List<Widget> actions;
  final EdgeInsetsGeometry padding;

  const ZPageHeader({
    super.key,
    required this.title,
    this.subtitle,
    this.leading,
    this.actions = const [],
    this.padding = EdgeInsets.zero,
  });

  bool _isPhone(BuildContext context) =>
      MediaQuery.sizeOf(context).width < ZBreakpoints.phone;

  @override
  Widget build(BuildContext context) {
    final titles = Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(title, style: ZTypography.headlineSmall),
        if (subtitle != null) ...[
          const SizedBox(height: ZSpacing.xs),
          Text(
            subtitle!,
            style: ZTypography.bodySmall.copyWith(
              color: Theme.of(context).colorScheme.onSurfaceVariant,
            ),
          ),
        ],
      ],
    );

    final Widget titleBlock = leading == null
        ? titles
        : Row(
            crossAxisAlignment: CrossAxisAlignment.center,
            children: [
              leading!,
              const SizedBox(width: ZSpacing.md),
              Expanded(child: titles),
            ],
          );

    final Widget body;
    if (actions.isEmpty) {
      body = titleBlock;
    } else if (_isPhone(context)) {
      body = Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          titleBlock,
          const SizedBox(height: ZSpacing.sm),
          Wrap(spacing: ZSpacing.sm, runSpacing: ZSpacing.sm, children: actions),
        ],
      );
    } else {
      body = Row(
        children: [
          Expanded(child: titleBlock),
          for (final action in actions) ...[
            const SizedBox(width: ZSpacing.sm),
            action,
          ],
        ],
      );
    }

    if (padding == EdgeInsets.zero) return body;
    return Padding(padding: padding, child: body);
  }
}
