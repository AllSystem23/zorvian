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
        child: Column(
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
            Expanded(
              child: Center(
                child: Stack(
                  alignment: Alignment.center,
                  children: [
                    SizedBox(width: 72, height: 72, child: CustomPaint(painter: _GaugePainter(pct, c))),
                    Column(mainAxisSize: MainAxisSize.min, children: [
                      Text(value.toStringAsFixed(2), style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: c)),
                      if (unit != null) Text(unit!, style: TextStyle(fontSize: 9, color: Colors.grey[600])),
                    ]),
                  ],
                ),
              ),
            ),
          ],
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
