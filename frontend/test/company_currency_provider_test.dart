import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/core/providers/company_currency_provider.dart';

/// A test notifier that returns a fixed [AuthState] from build(),
/// skipping the normal auth initialization.
class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

/// Helper to create a ProviderContainer with authProvider overridden to return
/// a fixed [AuthState].
ProviderContainer createContainerWithAuth(AuthState authState) {
  return ProviderContainer(overrides: [
    authProvider.overrideWith(() => _FixedAuthNotifier(authState)),
  ]);
}

void main() {
  group('CurrencyFormatService', () {
    test('currency formats with bound NIO', () {
      final service = CurrencyFormatService('NIO');
      // es_NI locale uses non-breaking space before symbol
      expect(service.currency(1500.00), '1.500,00\u00A0C\$');
    });

    test('currency formats with USD', () {
      final service = CurrencyFormatService('USD');
      expect(service.currency(99.99), '\$99.99');
    });

    test('currencyCompact formats millions with NIO', () {
      final service = CurrencyFormatService('NIO');
      expect(service.currencyCompact(1250000), 'C\$1.3M');
    });

    test('currencyCompact formats small amounts', () {
      final service = CurrencyFormatService('HNL');
      final result = service.currencyCompact(750.00);
      expect(result, contains('750'));
    });

    test('currencyCompact formats GTQ thousands', () {
      final service = CurrencyFormatService('GTQ');
      expect(service.currencyCompact(3500), 'Q3.5K');
    });
  });

  group('currencyFormatServiceProvider', () {
    test('returns service bound to auth currencyCode', () {
      final container = createContainerWithAuth(
        AuthState(
          status: AuthStatus.authenticated,
          currencyCode: 'USD',
        ),
      );
      final service = container.read(currencyFormatServiceProvider);
      expect(service.currency(100.00), '\$100.00');
      container.dispose();
    });
  });

  group('companyCurrencyProvider', () {
    tearDown(() => clearCachedCurrencyCode());

    test('returns auth currencyCode when authenticated and non-default', () {
      final container = createContainerWithAuth(
        AuthState(
          status: AuthStatus.authenticated,
          currencyCode: 'USD',
        ),
      );
      expect(container.read(companyCurrencyProvider), 'USD');
      container.dispose();
    });

    test('returns NIO fallback when auth has default and cache is empty', () {
      clearCachedCurrencyCode();
      final container = createContainerWithAuth(
        const AuthState(
          status: AuthStatus.unknown,
          currencyCode: 'NIO',
        ),
      );
      expect(container.read(companyCurrencyProvider), 'NIO');
      container.dispose();
    });

    test('returns cached value when auth has default and cache is set', () {
      setCachedCurrencyCode('HNL');
      final container = createContainerWithAuth(
        const AuthState(
          status: AuthStatus.unknown,
          currencyCode: 'NIO',
        ),
      );
      expect(container.read(companyCurrencyProvider), 'HNL');
      container.dispose();
    });

    test('auth takes priority over cache when non-default', () {
      setCachedCurrencyCode('HNL');
      final container = createContainerWithAuth(
        AuthState(
          status: AuthStatus.authenticated,
          currencyCode: 'GTQ',
        ),
      );
      expect(container.read(companyCurrencyProvider), 'GTQ');
      container.dispose();
    });
  });

  group('clearCachedCurrencyCode', () {
    tearDown(() => clearCachedCurrencyCode());

    test('clears cached currency code', () {
      setCachedCurrencyCode('USD');
      clearCachedCurrencyCode();

      final container = createContainerWithAuth(
        const AuthState(
          status: AuthStatus.unknown,
          currencyCode: 'NIO',
        ),
      );
      expect(container.read(companyCurrencyProvider), 'NIO');
      container.dispose();
    });
  });
}
