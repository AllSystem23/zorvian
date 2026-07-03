import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../utils/formatters.dart';

// Import auth_provider for the provider dependency (must be after the
// function declarations so that company_currency_provider.dart can be
// consumed by auth_provider.dart without circular imports).
import '../../auth/auth_provider.dart';

/// In-memory cache for the company currency code, preloaded from SecureStorage
/// on app startup so it's available immediately without waiting for `auth/me`.
String? _cachedCurrencyCode;

/// Called from app initialization to preload the cached currency code.
void setCachedCurrencyCode(String code) {
  _cachedCurrencyCode = code;
}

/// Called on logout to clear the cached currency so the previous company's
/// currency isn't briefly shown before `authProvider` fully resets.
void clearCachedCurrencyCode() {
  _cachedCurrencyCode = null;
}

/// Provider that exposes the current company's default currency code.
///
/// Prioritizes:
/// 1. Auth state (once resolved from API)
/// 2. In-memory cache (preloaded from SecureStorage on startup)
/// 3. Fallback `'NIO'`
///
/// Falls back to `'NIO'` when nothing else is available.
final companyCurrencyProvider = Provider<String>((ref) {
  final authCode = ref.watch(authProvider).currencyCode;

  // If auth already resolved to a real (non-default) value, it's authoritative
  if (authCode != 'NIO') return authCode;

  // Auth not ready yet (unknown status) — use cached value from startup
  if (_cachedCurrencyCode != null) return _cachedCurrencyCode!;

  return 'NIO';
});

/// Convenient formatting service pre-bound to the company's currency code.
///
/// Usage in any widget:
/// ```dart
/// final fmt = ref.watch(currencyFormatServiceProvider);
/// fmt.currency(amount);        // → "C$ 1,250,400.00"
/// fmt.currencyCompact(amount); // → "C$ 1.3M"
/// ```
class CurrencyFormatService {
  final String _currencyCode;

  const CurrencyFormatService(this._currencyCode);

  /// Full currency format with thousands separators.
  String currency(double amount) =>
      ZFormatters.currency(amount, currencyCode: _currencyCode);

  /// Compact currency format for KPI cards (K/M notation).
  String currencyCompact(num amount) =>
      ZFormatters.currencyCompact(amount, currencyCode: _currencyCode);
}

/// Provider that exposes a [CurrencyFormatService] bound to the company's
/// default currency. Updates reactively when the user switches tenant.
final currencyFormatServiceProvider = Provider<CurrencyFormatService>((ref) {
  return CurrencyFormatService(ref.watch(companyCurrencyProvider));
});
