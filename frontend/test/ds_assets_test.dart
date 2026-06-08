import 'package:flutter_test/flutter_test.dart';
import 'package:nexora/shared/ds/ds.dart';

void main() {
  test('ZAssets paths are correctly defined', () {
    expect(ZAssets.logo, 'assets/Zorvian.png');
    expect(ZAssets.logoErp, 'assets/logo_erp.png');
  });
}
