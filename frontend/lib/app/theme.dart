import 'package:flutter/material.dart';
import '../shared/ds/ds.dart';

class ZorvianTheme {
  static ThemeData light() => ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    colorSchemeSeed: ZColors.brandPrimary,
    scaffoldBackgroundColor: ZColors.background,
    appBarTheme: AppBarTheme(
      elevation: 0,
      centerTitle: false,
      backgroundColor: ZColors.surface,
      foregroundColor: ZColors.brandPrimary,
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: const BorderSide(color: ZColors.border),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: ZColors.surface,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.border),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.border),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.brandAccent, width: 2),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg, vertical: ZSpacing.md),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: ZColors.brandAccent,
        foregroundColor: ZColors.brandPrimary,
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    textTheme: TextTheme(
      displayLarge: ZTypography.displayLarge,
      displayMedium: ZTypography.displayMedium,
      displaySmall: ZTypography.displaySmall,
      headlineLarge: ZTypography.headlineLarge,
      headlineMedium: ZTypography.headlineMedium,
      headlineSmall: ZTypography.headlineSmall,
      titleLarge: ZTypography.titleLarge,
      titleMedium: ZTypography.titleMedium,
      titleSmall: ZTypography.titleSmall,
      bodyLarge: ZTypography.bodyLarge,
      bodyMedium: ZTypography.bodyMedium,
      bodySmall: ZTypography.bodySmall,
      labelLarge: ZTypography.labelLarge,
      labelMedium: ZTypography.labelMedium,
      labelSmall: ZTypography.labelSmall,
    ),
  );

  static ThemeData dark() => ThemeData(
    useMaterial3: true,
    brightness: Brightness.dark,
    colorSchemeSeed: ZColors.brandAccent,
    scaffoldBackgroundColor: ZColors.darkBackground,
    appBarTheme: const AppBarTheme(
      elevation: 0,
      centerTitle: false,
      backgroundColor: ZColors.darkSurface,
      foregroundColor: ZColors.neutral200,
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: const BorderSide(color: ZColors.darkBorder),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: ZColors.darkSurface,
      labelStyle: const TextStyle(color: ZColors.neutral300),
      hintStyle: const TextStyle(color: ZColors.neutral400),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.darkBorder),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.darkBorder),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.brandAccent, width: 2),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg, vertical: ZSpacing.md),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: ZColors.brandAccent,
        foregroundColor: ZColors.brandPrimary,
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
      ),
    ),
    textTheme: TextTheme(
      displayLarge: ZTypography.displayLarge,
      displayMedium: ZTypography.displayMedium,
      displaySmall: ZTypography.displaySmall,
      headlineLarge: ZTypography.headlineLarge,
      headlineMedium: ZTypography.headlineMedium,
      headlineSmall: ZTypography.headlineSmall,
      titleLarge: ZTypography.titleLarge,
      titleMedium: ZTypography.titleMedium,
      titleSmall: ZTypography.titleSmall,
      bodyLarge: ZTypography.bodyLarge.copyWith(color: ZColors.neutral300),
      bodyMedium: ZTypography.bodyMedium.copyWith(color: ZColors.neutral300),
      bodySmall: ZTypography.bodySmall.copyWith(color: ZColors.neutral400),
      labelLarge: ZTypography.labelLarge.copyWith(color: ZColors.neutral300),
      labelMedium: ZTypography.labelMedium.copyWith(color: ZColors.neutral400),
      labelSmall: ZTypography.labelSmall.copyWith(color: ZColors.neutral400),
    ),
  );
}
