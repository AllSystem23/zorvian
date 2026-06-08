import 'package:flutter/material.dart';

class ZTypography {
  // Families
  static const String family = 'Inter';
  static const String familyMono = 'JetBrains Mono';

  // Display
  static const TextStyle displayLarge = TextStyle(fontSize: 32, fontWeight: FontWeight.w700, height: 1.2, letterSpacing: -0.5);
  static const TextStyle displayMedium = TextStyle(fontSize: 28, fontWeight: FontWeight.w700, height: 1.25, letterSpacing: -0.25);
  static const TextStyle displaySmall = TextStyle(fontSize: 24, fontWeight: FontWeight.w600, height: 1.3);

  // Headings
  static const TextStyle headlineLarge = TextStyle(fontSize: 22, fontWeight: FontWeight.w600, height: 1.3);
  static const TextStyle headlineMedium = TextStyle(fontSize: 20, fontWeight: FontWeight.w600, height: 1.35);
  static const TextStyle headlineSmall = TextStyle(fontSize: 18, fontWeight: FontWeight.w600, height: 1.4);

  // Title
  static const TextStyle titleLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w600, height: 1.4);
  static const TextStyle titleMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w600, height: 1.4);
  static const TextStyle titleSmall = TextStyle(fontSize: 13, fontWeight: FontWeight.w600, height: 1.4);

  // Body
  static const TextStyle bodyLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w400, height: 1.5);
  static const TextStyle bodyMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w400, height: 1.5);
  static const TextStyle bodySmall = TextStyle(fontSize: 12, fontWeight: FontWeight.w400, height: 1.5);

  // Label
  static const TextStyle labelLarge = TextStyle(fontSize: 14, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle labelMedium = TextStyle(fontSize: 12, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle labelSmall = TextStyle(fontSize: 11, fontWeight: FontWeight.w500, height: 1.4, letterSpacing: 0.5);

  // Monospace (code, numbers)
  static const TextStyle monoLarge = TextStyle(fontSize: 16, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle monoMedium = TextStyle(fontSize: 14, fontWeight: FontWeight.w500, height: 1.4);
  static const TextStyle monoSmall = TextStyle(fontSize: 12, fontWeight: FontWeight.w500, height: 1.4);
}
