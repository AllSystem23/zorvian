import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/bi/models/bi_models.dart';

void main() {
  group('BranchComparison', () {
    test('fromJson parsea el payload completo del comparativo', () {
      final c = BranchComparison.fromJson({
        'branches': [
          {
            'branchId': 'aaa',
            'branchName': 'Central',
            'monthSales': 100.5,
            'salesCount': 2,
            'monthRecovery': 40,
            'portfolio': 500.25,
            'activeCredits': 5,
            'overdueCredits': 1,
            'inventoryValue': 1000,
          },
          {
            'branchId': 'bbb',
            'branchName': 'Norte',
            'monthSales': 50,
            'salesCount': 1,
            'monthRecovery': 10,
            'portfolio': 200,
            'activeCredits': 3,
            'overdueCredits': 2,
            'inventoryValue': 300,
          },
        ],
        'totalMonthSales': 150.5,
        'totalSalesCount': 3,
        'totalMonthRecovery': 50,
        'totalPortfolio': 700.25,
        'totalActiveCredits': 8,
        'totalOverdueCredits': 3,
        'totalInventoryValue': 1300,
      });

      expect(c.branches, hasLength(2));
      expect(c.branches[0].branchName, 'Central');
      expect(c.branches[0].monthSales, 100.5);
      expect(c.branches[1].overdueCredits, 2);
      expect(c.totalMonthSales, 150.5);
      expect(c.totalSalesCount, 3);
      expect(c.totalPortfolio, 700.25);
      expect(c.totalActiveCredits, 8);
      expect(c.totalOverdueCredits, 3);
      expect(c.totalInventoryValue, 1300);
    });

    test('fromJson con payload vacío no lanza y da totales en cero', () {
      final c = BranchComparison.fromJson(const {});

      expect(c.branches, isEmpty);
      expect(c.totalMonthSales, 0);
      expect(c.totalSalesCount, 0);
      expect(c.totalMonthRecovery, 0);
      expect(c.totalPortfolio, 0);
      expect(c.totalActiveCredits, 0);
      expect(c.totalOverdueCredits, 0);
      expect(c.totalInventoryValue, 0);
    });

    test('BranchComparisonItem.fromJson parsea campos del backend', () {
      final item = BranchComparisonItem.fromJson(const {
        'branchId': 'x',
        'branchName': 'Sur',
        'monthSales': 10,
        'salesCount': 1,
        'monthRecovery': 0,
        'portfolio': 0,
        'activeCredits': 0,
        'overdueCredits': 0,
        'inventoryValue': 0,
      });

      expect(item.branchName, 'Sur');
      expect(item.monthSales, 10.0);
      expect(item.salesCount, 1);
    });
  });
}
