import 'package:intl/intl.dart';

/// Centralized formatting utilities for Zorvian ERP
class ZFormatters {
  ZFormatters._();

  // ── Currency ──
  static final Map<String, NumberFormat> _currencyFormats = {
    'NIO': NumberFormat.currency(symbol: 'C\$', decimalDigits: 2, locale: 'es_NI'),
    'USD': NumberFormat.currency(symbol: '\$', decimalDigits: 2, locale: 'en_US'),
    'GTQ': NumberFormat.currency(symbol: 'Q', decimalDigits: 2, locale: 'es_GT'),
    'HNL': NumberFormat.currency(symbol: 'L', decimalDigits: 2, locale: 'es_HN'),
    'CRC': NumberFormat.currency(symbol: '₡', decimalDigits: 2, locale: 'es_CR'),
    'SVC': NumberFormat.currency(symbol: '\$', decimalDigits: 2, locale: 'es_SV'),
    'PAB': NumberFormat.currency(symbol: 'B/.', decimalDigits: 2, locale: 'es_PA'),
  };

  static String currency(double amount, {String currencyCode = 'NIO'}) {
    final format = _currencyFormats[currencyCode];
    if (format != null) return format.format(amount);
    return NumberFormat.currency(symbol: currencyCode, decimalDigits: 2).format(amount);
  }

  /// Compact currency format for KPI cards and dashboards.
  ///
  /// Shows values in compact notation: `C$ 1.5M`, `$ 2.3K`, `L 500.00`.
  /// Falls back to [currency] for amounts under 1 000.
  static String currencyCompact(num amount, {String currencyCode = 'NIO'}) {
    final symbol = _currencySymbol(currencyCode);
    final d = amount.toDouble();
    final abs = d.abs();
    if (abs >= 1000000) {
      return '$symbol${(d / 1000000).toStringAsFixed(1)}M';
    }
    if (abs >= 1000) {
      return '$symbol${(d / 1000).toStringAsFixed(1)}K';
    }
    return currency(d, currencyCode: currencyCode);
  }

  /// Returns the currency symbol for a given currency code.
  static String _currencySymbol(String code) {
    switch (code) {
      case 'NIO': return 'C\$';
      case 'USD': return '\$';
      case 'GTQ': return 'Q';
      case 'HNL': return 'L';
      case 'CRC': return '₡';
      case 'SVC': return '\$';
      case 'PAB': return 'B/.';
      default: return '\$';
    }
  }

  // ── Numbers ──
  static String number(double value, {int decimals = 2}) {
    return NumberFormat('#,##0.${'0' * decimals}').format(value);
  }

  static String integer(int value) {
    return NumberFormat('#,##0').format(value);
  }

  static String percentage(double value, {int decimals = 1}) {
    return '${(value * 100).toStringAsFixed(decimals)}%';
  }

  // ── Dates ──
  static String date(DateTime dt) {
    return DateFormat('dd/MM/yyyy').format(dt);
  }

  /// Formats a nullable DateTime. Returns [fallback] (default '—') when null.
  static String dateOrNull(DateTime? dt, {String fallback = '—'}) {
    if (dt == null) return fallback;
    return date(dt);
  }

  static String dateTime(DateTime dt) {
    return DateFormat('dd/MM/yyyy HH:mm').format(dt);
  }

  static String dateTimeFull(DateTime dt) {
    return DateFormat("d 'de' MMMM 'de' yyyy, HH:mm", 'es').format(dt);
  }

  static String time(DateTime dt) {
    return DateFormat('HH:mm').format(dt);
  }

  static String relative(DateTime dt) {
    final now = DateTime.now();
    final diff = now.difference(dt);

    if (diff.inSeconds < 60) return 'Hace un momento';
    if (diff.inMinutes < 60) return 'Hace ${diff.inMinutes} min';
    if (diff.inHours < 24) return 'Hace ${diff.inHours}h';
    if (diff.inDays < 7) return 'Hace ${diff.inDays} días';
    if (diff.inDays < 30) return 'Hace ${(diff.inDays / 7).floor()} semanas';
    return date(dt);
  }

  // ── Phone ──
  static String phone(String value) {
    final digits = value.replaceAll(RegExp(r'\D'), '');
    if (digits.length == 8) {
      return '${digits.substring(0, 4)}-${digits.substring(4)}';
    }
    if (digits.length == 11 && digits.startsWith('505')) {
      return '+505 ${digits.substring(3, 7)}-${digits.substring(7)}';
    }
    return value;
  }

  // ── Identification (RUC/Cédula/Panamá) ──
  static String identification(String value, {String? country}) {
    final digits = value.replaceAll(RegExp(r'\D'), '');

    if (country != null) {
      switch (country.toUpperCase()) {
        case 'PA': // Panamá - Cédula: 8 dígitos
          if (digits.length == 8) {
            return digits; // Panamá usa solo números, sin guiones
          }
          break;
        case 'NI': // Nicaragua - Cédula: 14 dígitos
          if (digits.length == 14) {
            return '${digits.substring(0, 4)}-${digits.substring(4, 10)}-${digits.substring(10)}';
          }
          break;
        case 'CR': // Costa Rica - Cédula: 9-12 dígitos
          return digits;
        case 'GT': // Guatemala - DPI: 13 dígitos
          if (digits.length == 13) {
            return '${digits.substring(0, 4)} ${digits.substring(4, 8)} ${digits.substring(8)}';
          }
          break;
        case 'HN': // Honduras - ID: 13 dígitos
          return digits;
        case 'SV': // El Salvador - DUI: 9 dígitos
          if (digits.length == 9) {
            return '${digits.substring(0, 8)}-${digits.substring(8)}';
          }
          break;
      }
    }

    // Default: Nicaragua format
    if (digits.length == 14) {
      return '${digits.substring(0, 4)}-${digits.substring(4, 10)}-${digits.substring(10)}';
    }
    if (digits.length == 13) {
      return '${digits.substring(0, 3)}-${digits.substring(3, 9)}-${digits.substring(9)}';
    }
    return value;
  }

  // ── Truncate ──
  static String truncate(String text, {int maxLength = 50, String suffix = '...'}) {
    if (text.length <= maxLength) return text;
    return '${text.substring(0, maxLength - suffix.length)}$suffix';
  }

  // ── Status Colors ──
  static String statusLabel(String status) {
    switch (status.toLowerCase()) {
      case 'active': return 'Activo';
      case 'inactive': return 'Inactivo';
      case 'pending': return 'Pendiente';
      case 'approved': return 'Aprobado';
      case 'rejected': return 'Rechazado';
      case 'cancelled': return 'Cancelado';
      case 'paid': return 'Pagado';
      case 'overdue': return 'Vencido';
      case 'completed': return 'Completado';
      case 'in_progress': return 'En Progreso';
      default: return status;
    }
  }
}