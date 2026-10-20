import 'package:flutter/material.dart';

import '../ds.dart';

/// Rejilla de tarjetas de KPI (`ZStatCard`) que **no desborda en teléfono**.
///
/// `GridView.count(childAspectRatio: x)` deriva el alto de la tarjeta del ancho
/// disponible: en un teléfono de 360 px con 2 columnas cada tarjeta mide ~158 px
/// y con `childAspectRatio` 1.4–2.2 el alto queda en 72–113 px, mientras que la
/// tarjeta en modo compacto necesita ~92 px (padding 12, icono 32 y la fila del
/// valor). Resultado: `RenderFlex overflowed` de 20–40 px en todas las tarjetas.
///
/// Por debajo de [ZBreakpoints.phone] la rejilla usa `Wrap` con **alto
/// intrínseco**: cada tarjeta mide lo que su contenido necesita. Desde 600 px
/// se conserva el `GridView` con el [aspectRatio] recibido (el diseño de
/// escritorio no cambia), salvo que la columna quede por debajo de
/// [compactWidth] —ahí la tarjeta entra en modo compacto y otra vez conviene el
/// alto intrínseco—.
class ZStatGrid extends StatelessWidget {
  final List<Widget> children;

  /// Columnas en teléfono (<600 px).
  final int phoneColumns;

  /// Columnas desde 600 px.
  final int tabletColumns;

  /// Columnas desde 900 px.
  final int desktopColumns;

  /// Relación ancho/alto usada por el `GridView` (solo ≥600 px).
  final double aspectRatio;

  final double spacing;

  /// Por debajo de este ancho de tarjeta no se fuerza alto (modo compacto).
  static const compactWidth = 200.0;

  const ZStatGrid({
    super.key,
    required this.children,
    this.phoneColumns = 2,
    this.tabletColumns = 4,
    this.desktopColumns = 5,
    this.aspectRatio = 1.4,
    this.spacing = ZSpacing.md,
  });

  @override
  Widget build(BuildContext context) {
    if (children.isEmpty) return const SizedBox.shrink();

    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final columns = width < ZBreakpoints.phone
            ? phoneColumns
            : width < 900
                ? tabletColumns
                : desktopColumns;
        final itemWidth = (width - spacing * (columns - 1)) / columns;

        // Alto intrínseco: teléfono o columnas tan angostas que la tarjeta
        // entra en modo compacto (un alto fijo desbordaría).
        if (width < ZBreakpoints.phone || itemWidth < compactWidth) {
          return Wrap(
            spacing: spacing,
            runSpacing: spacing,
            children: [
              for (final child in children)
                SizedBox(width: itemWidth.floorToDouble(), child: child),
            ],
          );
        }

        return GridView.count(
          shrinkWrap: true,
          physics: const NeverScrollableScrollPhysics(),
          crossAxisCount: columns,
          mainAxisSpacing: spacing,
          crossAxisSpacing: spacing,
          childAspectRatio: aspectRatio,
          children: children,
        );
      },
    );
  }
}
