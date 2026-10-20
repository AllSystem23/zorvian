import 'package:flutter/material.dart';

import '../tokens/breakpoints.dart';
import '../tokens/spacing.dart';
import 'z_card.dart';

/// Línea de un carrito (POS, cotizaciones, compras, solicitudes).
///
/// En **teléfono** no caben el nombre del producto, la cantidad, un descuento
/// opcional, el total y el botón de borrar en una sola fila: el título y el
/// borrar van arriba y los controles de cantidad/total en una segunda fila.
/// Desde 600 px todo comparte la fila con el título expandido.
class ZCartLine extends StatelessWidget {
  const ZCartLine({
    super.key,
    required this.title,
    required this.quantity,
    required this.total,
    required this.onRemove,
    this.extra,
    this.removeTooltip = 'Quitar producto',
    this.padding = const EdgeInsets.all(8),
  });

  /// Nombre del producto y su precio unitario (se expande en escritorio).
  final Widget title;

  /// Control de cantidad: campo numérico o stepper −/+.
  final Widget quantity;

  /// Control opcional en la segunda posición (p. ej. descuento de la línea).
  final Widget? extra;

  /// Total de la línea, alineado a la derecha.
  final Widget total;

  final VoidCallback onRemove;
  final String removeTooltip;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    final isPhone = MediaQuery.sizeOf(context).width < ZBreakpoints.phone;
    final remove = IconButton(
      icon: const Icon(Icons.close, size: 18, color: Colors.red),
      tooltip: removeTooltip,
      onPressed: onRemove,
    );

    return ZCard(
      padding: padding,
      // `IntrinsicHeight` evita que un `TextField` en la línea (cantidad,
      // descuento) se estire a todo el alto disponible cuando la tarjeta vive
      // dentro de un contenedor de alto acotado (p. ej. un `Expanded`): sin
      // él, el campo se centraba a media pantalla.
      child: IntrinsicHeight(
        child: isPhone
            ? Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(children: [Expanded(child: title), remove]),
                  Row(
                    children: [
                      quantity,
                      if (extra != null) ...[
                        const SizedBox(width: ZSpacing.sm),
                        Flexible(child: extra!),
                      ],
                      const Spacer(),
                      total,
                    ],
                  ),
                ],
              )
            : Row(
                children: [
                  Expanded(child: title),
                  const SizedBox(width: ZSpacing.xs),
                  quantity,
                  if (extra != null) ...[
                    const SizedBox(width: ZSpacing.xs),
                    extra!,
                  ],
                  const SizedBox(width: ZSpacing.xs),
                  total,
                  remove,
                ],
              ),
      ),
    );
  }
}
