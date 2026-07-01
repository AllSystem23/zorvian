import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:zorvian/features/warranties/providers/public_warranty_provider.dart';

void main() {
  test('PublicWarrantyNotifier initializes correctly', () {
    final container = ProviderContainer();
    final state = container.read(publicWarrantyProvider);
    expect(state.isLoading, false);
    expect(state.warranty, isNull);
    container.dispose();
  });
}
