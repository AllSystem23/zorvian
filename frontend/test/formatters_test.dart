import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/utils/formatters.dart';

void main() {
  group('ZFormatters.currency', () {
    test('formats NIO with es_NI locale', () {
      // es_NI locale: '.' for thousands, ',' for decimals, symbol after with \u00A0
      expect(
        ZFormatters.currency(1234.50, currencyCode: 'NIO'),
        '1.234,50\u00A0C\$',
      );
    });

    test('formats USD with en_US locale', () {
      // en_US locale: no space between symbol and amount
      expect(
        ZFormatters.currency(999.99, currencyCode: 'USD'),
        '\$999.99',
      );
    });

    test('formats zero in NIO', () {
      expect(ZFormatters.currency(0, currencyCode: 'NIO'), '0,00\u00A0C\$');
    });
  });

  group('ZFormatters.currencyCompact', () {
    group('amounts under 1000', () {
      test('returns full format for small amounts', () {
        // Falls back to currency() with es_NI locale
        expect(
          ZFormatters.currencyCompact(500, currencyCode: 'NIO'),
          '500,00\u00A0C\$',
        );
      });

      test('returns full format for zero', () {
        expect(
          ZFormatters.currencyCompact(0, currencyCode: 'NIO'),
          '0,00\u00A0C\$',
        );
      });

      test('handles cents correctly', () {
        expect(
          ZFormatters.currencyCompact(999.99, currencyCode: 'NIO'),
          '999,99\u00A0C\$',
        );
      });
    });

    group('amounts in thousands (K)', () {
      test('formats 1500 as C\$1.5K', () {
        // currencyCompact does symbol + value + suffix (no spaces)
        expect(
          ZFormatters.currencyCompact(1500, currencyCode: 'NIO'),
          'C\$1.5K',
        );
      });

      test('formats 10000 as C\$10.0K', () {
        expect(
          ZFormatters.currencyCompact(10000, currencyCode: 'NIO'),
          'C\$10.0K',
        );
      });

      test('formats 999500 as C\$999.5K', () {
        // Use value that doesn't round up
        expect(
          ZFormatters.currencyCompact(999500, currencyCode: 'NIO'),
          'C\$999.5K',
        );
      });
    });

    group('amounts in millions (M)', () {
      test('formats 1M as C\$1.0M', () {
        expect(
          ZFormatters.currencyCompact(1000000, currencyCode: 'NIO'),
          'C\$1.0M',
        );
      });

      test('formats 2.5M correctly', () {
        expect(
          ZFormatters.currencyCompact(2500000, currencyCode: 'NIO'),
          'C\$2.5M',
        );
      });

      test('formats 15M correctly', () {
        expect(
          ZFormatters.currencyCompact(15000000, currencyCode: 'NIO'),
          'C\$15.0M',
        );
      });
    });

    group('different currency codes', () {
      test('USD compact', () {
        // _currencySymbol returns '$', then concat: '$' + '2.5' + 'K'
        expect(
          ZFormatters.currencyCompact(2500, currencyCode: 'USD'),
          '\$2.5K',
        );
      });

      test('USD millions', () {
        expect(
          ZFormatters.currencyCompact(5000000, currencyCode: 'USD'),
          '\$5.0M',
        );
      });

      test('GTQ compact', () {
        expect(
          ZFormatters.currencyCompact(3400, currencyCode: 'GTQ'),
          'Q3.4K',
        );
      });

      test('HNL compact', () {
        expect(
          ZFormatters.currencyCompact(7200, currencyCode: 'HNL'),
          'L7.2K',
        );
      });
    });

    group('edge cases', () {
      test('accepts int type', () {
        expect(
          ZFormatters.currencyCompact(1000, currencyCode: 'NIO'),
          'C\$1.0K',
        );
      });

      test('unknown currency code uses dollar symbol', () {
        // _currencySymbol default case returns '$'
        final result = ZFormatters.currencyCompact(2000, currencyCode: 'EUR');
        expect(result, '\$2.0K');
      });
    });
  });

  group('ZFormatters.number', () {
    test('formats with thousands separator', () {
      expect(ZFormatters.number(1234567.89), '1,234,567.89');
    });

    test('formats zero', () {
      expect(ZFormatters.number(0), '0.00');
    });
  });

  group('ZFormatters.percentage', () {
    test('converts 0.25', () {
      expect(ZFormatters.percentage(0.25), '25.0%');
    });

    test('converts 1.0', () {
      expect(ZFormatters.percentage(1.0), '100.0%');
    });
  });

  group('ZFormatters.date', () {
    test('formats date as dd/MM/yyyy', () {
      final dt = DateTime(2026, 7, 3);
      expect(ZFormatters.date(dt), '03/07/2026');
    });
  });

  group('ZFormatters.phone', () {
    test('formats 8-digit Nicaraguan number', () {
      expect(ZFormatters.phone('12345678'), '1234-5678');
    });

    test('formats 11-digit with 505 prefix', () {
      expect(ZFormatters.phone('50512345678'), '+505 1234-5678');
    });

    test('returns original if no pattern matches', () {
      expect(ZFormatters.phone('abc'), 'abc');
    });
  });
}
