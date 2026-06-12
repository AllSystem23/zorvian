import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../../shared/ds/ds.dart';

/// A subtle animated particle background for premium screens.
class ParticleBackground extends StatefulWidget {
  const ParticleBackground({super.key});

  @override
  State<ParticleBackground> createState() => _ParticleBackgroundState();
}

class _ParticleBackgroundState extends State<ParticleBackground>
    with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  final List<Particle> _particles = [];
  final math.Random _random = math.Random();

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 10),
    )..repeat();

    // Initialize particles with normalized coordinates (0.0 to 1.0)
    for (int i = 0; i < 40; i++) {
      _particles.add(Particle(
        position: Offset(_random.nextDouble(), _random.nextDouble()),
        velocity: Offset(
          (_random.nextDouble() - 0.5) * 0.001,
          (_random.nextDouble() - 0.5) * 0.001,
        ),
        size: _random.nextDouble() * 3 + 1,
        color: _getRandomColor(),
        opacity: _random.nextDouble() * 0.2 + 0.05,
      ));
    }
  }

  Color _getRandomColor() {
    final colors = [
      ZColors.brandSecondary,
      ZColors.brandAccent,
      ZColors.moduleIa,
    ];
    return colors[_random.nextInt(colors.length)];
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
      builder: (context, child) {
        for (var particle in _particles) {
          particle.update();
        }
        return CustomPaint(
          painter: ParticlePainter(particles: _particles),
          size: Size.infinite,
        );
      },
    );
  }
}

class Particle {
  Offset position;
  Offset velocity;
  double size;
  Color color;
  double opacity;

  Particle({
    required this.position,
    required this.velocity,
    required this.size,
    required this.color,
    required this.opacity,
  });

  void update() {
    position += velocity;
    
    // Wrap around screen
    if (position.dx < 0) position = Offset(1.0, position.dy);
    if (position.dx > 1) position = Offset(0.0, position.dy);
    if (position.dy < 0) position = Offset(position.dx, 1.0);
    if (position.dy > 1) position = Offset(position.dx, 0.0);
  }
}

class ParticlePainter extends CustomPainter {
  final List<Particle> particles;

  ParticlePainter({required this.particles});

  @override
  void paint(Canvas canvas, Size size) {
    for (var particle in particles) {
      final paint = Paint()
        ..color = particle.color.withValues(alpha: particle.opacity)
        ..maskFilter = const MaskFilter.blur(BlurStyle.normal, 1);
      
      canvas.drawCircle(
        Offset(particle.position.dx * size.width, particle.position.dy * size.height),
        particle.size,
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant ParticlePainter oldDelegate) => true;
}
