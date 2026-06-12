import 'package:flutter/material.dart';
import '../shared/ds/ds.dart';

class ZorvianTheme {
  static ThemeData light() => ThemeData(
    useMaterial3: true,
    brightness: Brightness.light,
    colorScheme: ColorScheme.fromSeed(
      seedColor: ZColors.brandPrimary,
      primary: ZColors.brandPrimary,
      secondary: ZColors.brandAccent,
      tertiary: ZColors.brandSecondary,
      brightness: Brightness.light,
    ),
    scaffoldBackgroundColor: ZColors.background,
    appBarTheme: AppBarTheme(
      elevation: 0,
      centerTitle: false,
      backgroundColor: ZColors.surface,
      foregroundColor: ZColors.brandPrimary,
      titleTextStyle: const TextStyle(
        color: ZColors.brandPrimary,
        fontSize: 18,
        fontWeight: FontWeight.w600,
        fontFamily: 'Inter',
      ),
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: const BorderSide(color: ZColors.border),
      ),
      color: ZColors.surface,
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
        borderSide: const BorderSide(color: ZColors.brandSecondary, width: 2),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.danger, width: 1.5),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg, vertical: ZSpacing.md),
      labelStyle: const TextStyle(color: ZColors.neutral600, fontSize: 14),
      hintStyle: const TextStyle(color: ZColors.neutral400, fontSize: 14),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: ZColors.brandAccent,
        foregroundColor: Colors.white,
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
        textStyle: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
        elevation: 0,
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: ZColors.brandPrimary,
        side: const BorderSide(color: ZColors.brandPrimary),
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: ZColors.brandAccent,
        textStyle: const TextStyle(fontWeight: FontWeight.w600),
      ),
    ),
    chipTheme: ChipThemeData(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.sm)),
      side: const BorderSide(color: ZColors.border),
      backgroundColor: ZColors.neutral50,
      labelStyle: const TextStyle(fontSize: 13, color: ZColors.neutral700),
    ),
    dividerTheme: const DividerThemeData(
      color: ZColors.border,
      thickness: 1,
      space: 1,
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
    ),
    floatingActionButtonTheme: FloatingActionButtonThemeData(
      backgroundColor: ZColors.brandAccent,
      foregroundColor: Colors.white,
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.lg)),
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
    colorScheme: ColorScheme.fromSeed(
      seedColor: ZColors.brandSecondary,
      primary: ZColors.brandSecondary,
      secondary: ZColors.brandAccent,
      tertiary: ZColors.brandPrimaryLight,
      brightness: Brightness.dark,
    ),
    scaffoldBackgroundColor: ZColors.darkBackground,
    appBarTheme: const AppBarTheme(
      elevation: 0,
      centerTitle: false,
      backgroundColor: ZColors.darkSurface,
      foregroundColor: ZColors.neutral200,
      titleTextStyle: TextStyle(
        color: ZColors.neutral200,
        fontSize: 18,
        fontWeight: FontWeight.w600,
        fontFamily: 'Inter',
      ),
    ),
    cardTheme: CardThemeData(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: const BorderSide(color: ZColors.darkBorder),
      ),
      color: ZColors.darkCard,
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
        borderSide: const BorderSide(color: ZColors.brandSecondary, width: 2),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(ZRadii.md),
        borderSide: const BorderSide(color: ZColors.danger, width: 1.5),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.lg, vertical: ZSpacing.md),
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: ZColors.brandAccent,
        foregroundColor: Colors.white,
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
        elevation: 0,
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        foregroundColor: ZColors.brandSecondary,
        side: const BorderSide(color: ZColors.brandSecondary),
        minimumSize: const Size(double.infinity, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
      ),
    ),
    textButtonTheme: TextButtonThemeData(
      style: TextButton.styleFrom(
        foregroundColor: ZColors.brandSecondary,
        textStyle: const TextStyle(fontWeight: FontWeight.w600),
      ),
    ),
    chipTheme: ChipThemeData(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.sm)),
      side: const BorderSide(color: ZColors.darkBorder),
      backgroundColor: ZColors.darkSurface,
      labelStyle: const TextStyle(fontSize: 13, color: ZColors.neutral300),
    ),
    dividerTheme: const DividerThemeData(
      color: ZColors.darkBorder,
      thickness: 1,
      space: 1,
    ),
    snackBarTheme: SnackBarThemeData(
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
    ),
    floatingActionButtonTheme: FloatingActionButtonThemeData(
      backgroundColor: ZColors.brandAccent,
      foregroundColor: Colors.white,
      elevation: 4,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.lg)),
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