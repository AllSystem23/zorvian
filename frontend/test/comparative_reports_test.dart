import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/accounting/models/enhanced_report_models.dart';
import 'package:zorvian/features/accounting/pages/comparative_reports_page.dart'; // Importa la extensión


void main() {
  group('ComparativeLineX Extension', () {
    test('toBarChartItem maps data correctly for single period', () {
      final line = ComparativeLine(
        concept: 'Ventas',
        accountType: 'Ingreso',
        periods: [
          ComparativePeriod(periodName: 'P1', amount: 100.0, percentageOfTotal: 1.0),
        ],
        variance: 0.0,
        variancePercent: 0.0,
      );
      
      final theme = ThemeData.light();
      final item = line.toBarChartItem(theme);
      
      expect(item.label, 'Ventas');
      expect(item.value, 100.0);
    });

    test('toBarChartItem maps data correctly for two periods (uses second)', () {
      final line = ComparativeLine(
        concept: 'Ventas',
        accountType: 'Ingreso',
        periods: [
          ComparativePeriod(periodName: 'P1', amount: 100.0, percentageOfTotal: 1.0),
          ComparativePeriod(periodName: 'P2', amount: 200.0, percentageOfTotal: 1.0),
        ],
        variance: 100.0,
        variancePercent: 1.0,
      );
      
      final theme = ThemeData.light();
      final item = line.toBarChartItem(theme);
      
      expect(item.label, 'Ventas');
      expect(item.value, 200.0); // Should pick the second
    });
  });
}
