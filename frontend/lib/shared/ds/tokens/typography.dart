import 'package:flutter/material.dart';

class ZTypography {
  // Families - Using modern Inter with specific adjustments
  static const String family = 'Inter';
  static const String familyMono = 'JetBrains Mono';

  // Display - Ultra aggressive and clean
  static const TextStyle displayLarge = TextStyle(fontSize: 32, fontWeight: FontWeight.w800, height: 1.1, letterSpacing: -1.0);
  static const TextStyle displayMedium = TextStyle(fontSize: 28, fontWeight: FontWeight.w700, height: 1.15, letterSpacing: -0.5);
  static const TextStyle displaySmall = TextStyle(fontSize: 24, fontWeight: FontWeight.w700, height: 1.2);

  // Headings
  static const TextStyle headlineLarge = TextStyle(fontSize: 22, fontWeight: FontWeight.w600, height: 1.25, letterSpacing: -0.2);
  static const TextStyle headlineMedium = TextStyle(fontSize: 20, fontWeight: FontWeight.w600, height: 1.3);
  static const TextStyle headlineSmall = TextStyle(fontSize: 18, fontWeight: FontWeight.w600, height: 1.3);

  // Title
  static const TextStyle titleLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w600, height: 1.4);
  static const TextStyle titleMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w600, height: 1.4);
  static const TextStyle titleSmall = TextStyle(fontSize: 13, fontWeight: FontWeight.w600, height: 1.4);

  // Body - Optimized for corporate reading
  static const TextStyle bodyLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w400, height: 1.6, letterSpacing: 0.1);
  static const TextStyle bodyMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w400, height: 1.6, letterSpacing: 0.1);
  static const TextStyle bodySmall = TextStyle(fontSize: 12, fontWeight: FontWeight.w400, height: 1.6);

  // Label
  static const TextStyle labelLarge = TextStyle(fontSize: 14, fontWeight: FontWeight.w500, height: 1.4, letterSpacing: 1.1); // CAPS compatible
  static const TextStyle labelMedium = TextStyle(fontSize: 12, fontWeight: FontWeight.w500, height: 1.4, letterSpacing: 1.0);
  static const TextStyle labelSmall = TextStyle(fontSize: 10, fontWeight: FontWeight.w600, height: 1.4, letterSpacing: 1.2);

  // Monospace
  static const TextStyle monoLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle monoMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle monoSmall = TextStyle(fontSize: 12, fontWeight: FontWeight.w500, height: 1.4);
}
