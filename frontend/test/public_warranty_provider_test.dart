import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/warranties/providers/public_warranty_provider.dart';

void main() {
  test('PublicWarrantyProvider initializes correctly', () {
    final provider = PublicWarrantyProvider();
    expect(provider.isLoading, false);
    expect(provider.warranty, isNull);
  });
}
