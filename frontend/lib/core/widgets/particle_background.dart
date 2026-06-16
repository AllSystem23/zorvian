import 'dart:math';
import 'package:flutter/material.dart';

class ParticleBackground extends StatefulWidget {
  final int particleCount;
  final List<Color> colors;
  final double speed;

  const ParticleBackground({
    super.key,
    this.particleCount = 50,
    this.colors = const [
      Color(0xFF7C4DFF),
      Color(0xFF00E5FF),
      Color(0xFF2EE59D),
    ],
    this.speed = 1.0,
  });

  @override
  State<ParticleBackground> createState() => _ParticleBackgroundState();
}

class _ParticleBackgroundState extends State<ParticleBackground>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final List<_Particle> _particles;
  final _rng = Random();

  @override
  void initState() {
    super.initState();
    _particles = List.generate(
      widget.particleCount,
      (_) => _Particle._(_rng),
    );
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 40),
    )..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (_, _) => CustomPaint(
        painter: _ParticlePainter(
          particles: _particles,
          colors: widget.colors,
          progress: _controller.value,
          speed: widget.speed,
        ),
        size: Size.zero,
      ),
    );
  }
}

class _Particle {
  double x, y;
  double vx, vy;
  double size;
  double baseOpacity;
  double opacityPhase;
  int colorIndex;

  _Particle._(Random rng)
      : x = rng.nextDouble(),
        y = rng.nextDouble(),
        vx = (rng.nextDouble() - 0.5) * 0.004,
        vy = (rng.nextDouble() - 0.5) * 0.004,
        size = rng.nextDouble() * 2.5 + 1.0,
        baseOpacity = rng.nextDouble() * 0.25 + 0.05,
        opacityPhase = rng.nextDouble() * 2 * pi,
        colorIndex = rng.nextInt(3);
}

class _ParticlePainter extends CustomPainter {
  final List<_Particle> particles;
  final List<Color> colors;
  final double progress;
  final double speed;

  _ParticlePainter({
    required this.particles,
    required this.colors,
    required this.progress,
    required this.speed,
  });

  @override
  void paint(Canvas canvas, Size size) {
    for (final p in particles) {
      p.x += p.vx * speed;
      p.y += p.vy * speed;

      if (p.x < -0.05) p.x = 1.05;
      if (p.x > 1.05) p.x = -0.05;
      if (p.y < -0.05) p.y = 1.05;
      if (p.y > 1.05) p.y = -0.05;

      final opacity = p.baseOpacity *
          (0.6 + 0.4 * sin(progress * 2 * pi * 0.5 + p.opacityPhase));

      final px = p.x * size.width;
      final py = p.y * size.height;

      final paint = Paint()
        ..color = colors[p.colorIndex].withValues(alpha: opacity)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 3);

      canvas.drawCircle(Offset(px, py), p.size, paint);
    }
  }

  @override
  bool shouldRepaint(covariant _ParticlePainter oldDelegate) => true;
}
