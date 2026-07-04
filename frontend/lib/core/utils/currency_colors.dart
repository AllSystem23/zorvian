import 'package:flutter/material.dart';

/// Currency-specific color mapping for visual indicators (badges, avatars, chips).
///
/// Each major currency gets a distinct brand-appropriate color so users can
/// quickly identify currencies at a glance in lists, headers, and dialogs.
class CurrencyColors {
  CurrencyColors._();

  /// Primary background color for the given currency code.
  static Color background(String code, {required bool isDark}) {
    final color = _baseColors[code.toUpperCase()] ?? const Color(0xFF1A0A3E);
    return isDark ? color.withValues(alpha: 0.25) : color.withValues(alpha: 0.12);
  }

  /// Foreground/text color for the given currency code.
  static Color foreground(String code, {required bool isDark}) {
    return isDark
        ? (_baseColors[code.toUpperCase()] ?? const Color(0xFF7C4DFF))
        : (_darkerColors[code.toUpperCase()] ?? const Color(0xFF1A0A3E));
  }

  /// Gradient colors for premium visual effects (e.g., avatar backgrounds).
  static List<Color> gradient(String code, {required bool isDark}) {
    final base = _baseColors[code.toUpperCase()] ?? const Color(0xFF1A0A3E);
    final accent = _accentColors[code.toUpperCase()] ?? const Color(0xFF7C4DFF);
    return isDark
        ? [base.withValues(alpha: 0.3), accent.withValues(alpha: 0.15)]
        : [base.withValues(alpha: 0.15), accent.withValues(alpha: 0.08)];
  }

  /// Bright gradient for dark-mode avatars.
  static List<Color> gradientBright(String code) {
    final base = _baseColors[code.toUpperCase()] ?? const Color(0xFF1A0A3E);
    final accent = _accentColors[code.toUpperCase()] ?? const Color(0xFF7C4DFF);
    return [base, accent];
  }

  // ── Color Map ──────────────────────────────────────────────────────────
  // Base (vibrant) and accent (lighter complementary) per currency.
  static const Map<String, Color> _baseColors = {
    'USD': Color(0xFF2E7D32), // Green — stable, dollar
    'NIO': Color(0xFF7C4DFF), // Violet — Zorvian brand
    'EUR': Color(0xFF1565C0), // Blue — euro
    'GTQ': Color(0xFFE65100), // Orange — quetzal
    'HNL': Color(0xFF00838F), // Teal — lempira
    'CRC': Color(0xFFC62828), // Red — colón
    'MXN': Color(0xFF6A1B9A), // Purple — peso
    'COP': Color(0xFFF9A825), // Gold — peso colombiano
    'PEN': Color(0xFFAD1457), // Rose — sol
    'BRL': Color(0xFF1B5E20), // Dark green — real
    'ARS': Color(0xFF0277BD), // Light blue — peso argentino
    'CLP': Color(0xFFD84315), // Deep orange — peso chileno
    'GBP': Color(0xFF283593), // Indigo — pound
    'JPY': Color(0xFFE53935), // Red — yen
    'CAD': Color(0xFFC62828), // Red — Canadian dollar
    'CHF': Color(0xFF00897B), // Teal — franc
  };

  static const Map<String, Color> _accentColors = {
    'USD': Color(0xFF66BB6A),
    'NIO': Color(0xFFB388FF),
    'EUR': Color(0xFF42A5F5),
    'GTQ': Color(0xFFFF8A65),
    'HNL': Color(0xFF26C6DA),
    'CRC': Color(0xFFEF5350),
    'MXN': Color(0xFFAB47BC),
    'COP': Color(0xFFFFD54F),
    'PEN': Color(0xFFEC407A),
    'BRL': Color(0xFF43A047),
    'ARS': Color(0xFF29B6F6),
    'CLP': Color(0xFFFF7043),
    'GBP': Color(0xFF5C6BC0),
    'JPY': Color(0xFFEF5350),
    'CAD': Color(0xFFEF5350),
    'CHF': Color(0xFF26A69A),
  };

  /// Darker variant for light-mode text (WCAG AA ≥4.5:1 on white).
  static const Map<String, Color> _darkerColors = {
    'USD': Color(0xFF1B5E20),
    'NIO': Color(0xFF4A148C),
    'EUR': Color(0xFF0D47A1),
    'GTQ': Color(0xFFBF360C),
    'HNL': Color(0xFF006064),
    'CRC': Color(0xFFB71C1C),
    'MXN': Color(0xFF4A148C),
    'COP': Color(0xFFF57F17),
    'PEN': Color(0xFF880E4F),
    'BRL': Color(0xFF1B5E20),
    'ARS': Color(0xFF01579B),
    'CLP': Color(0xFFBF360C),
    'GBP': Color(0xFF1A237E),
    'JPY': Color(0xFFB71C1C),
    'CAD': Color(0xFFB71C1C),
    'CHF': Color(0xFF004D40),
  };

  /// Currency-specific icon for the given currency code.
  static IconData icon(String code) {
    switch (code.toUpperCase()) {
      case 'USD':
      case 'CAD':
      case 'AUD':
      case 'NZD':
        return Icons.attach_money;
      case 'EUR':
        return Icons.euro;
      case 'GBP':
        return Icons.monetization_on;
      case 'JPY':
      case 'CNY':
        return Icons.money;
      case 'BRL':
      case 'MXN':
      case 'COP':
      case 'ARS':
      case 'CLP':
      case 'PEN':
        return Icons.monetization_on;
      default:
        return Icons.currency_exchange;
    }
  }
}
