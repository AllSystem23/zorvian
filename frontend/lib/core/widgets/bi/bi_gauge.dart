import 'dart:math' as math;

import 'package:flutter/material.dart';

class BiGauge extends StatelessWidget {
  final double value;
  final double min;
  final double max;
  final String label;
  final String? unit;
  final Color? color;

  const BiGauge({super.key, required this.value, this.min = 0, required this.max, required this.label, this.unit, this.color});

  @override
  Widget build(BuildContext context) {
    final pct = ((value - min) / (max - min)).clamp(0.0, 1.0);
    final c = color ?? (pct < 0.4 ? Colors.red : pct < 0.7 ? Colors.orange : Colors.green);
    return Card(
      margin: const EdgeInsets.all(4),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 8),
        child: LayoutBuilder(
          builder: (context, constraints) {
            // El círculo se dibujaba fijo a 72×72: en una tarjeta de 80 px (el
            // caso de un dashboard a 4 columnas en teléfono) el alto disponible
            // es ~37 px y el contenido desbordaba 9 px. Ahora el diámetro se
            // adapta al espacio real y el texto central escala con FittedBox.
            final maxHeight = constraints.hasBoundedHeight ? constraints.maxHeight : 96.0;
            final availableH = maxHeight - 19; // etiqueta + separación
            final diameter = math.max(
              24.0,
              math.min(72.0, math.min(constraints.maxWidth, availableH)),
            );
            final circle = Center(
              child: SizedBox(
                width: diameter,
                height: diameter,
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    CustomPaint(painter: _GaugePainter(pct, c), size: Size.square(diameter)),
                    SizedBox(
                      width: diameter * 0.66,
                      child: FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(value.toStringAsFixed(2), style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: c)),
                            if (unit != null) Text(unit!, style: TextStyle(fontSize: 9, color: Colors.grey[600])),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            );

            return Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(color: Colors.grey[600], fontSize: 11),
                ),
                const SizedBox(height: 4),
                if (constraints.hasBoundedHeight) Expanded(child: circle) else circle,
              ],
            );
          },
        ),
      ),
    );
  }
}

class _GaugePainter extends CustomPainter {
  final double pct;
  final Color color;
  _GaugePainter(this.pct, this.color);

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.width / 2 - 4;
    final bgPaint = Paint()..color = Colors.grey[300]!..style = PaintingStyle.stroke..strokeWidth = 8;
    final fgPaint = Paint()..color = color..style = PaintingStyle.stroke..strokeWidth = 8..strokeCap = StrokeCap.round;

    canvas.drawCircle(center, radius, bgPaint);
    canvas.drawArc(Rect.fromCircle(center: center, radius: radius), -1.5708, 6.2832 * pct, false, fgPaint);
  }

  @override
  bool shouldRepaint(covariant _GaugePainter old) => old.pct != pct;
}
