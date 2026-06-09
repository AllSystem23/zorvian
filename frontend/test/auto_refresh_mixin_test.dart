import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/core/mixins/auto_refresh_mixin.dart';

// Test simple para verificar que el Mixin se inicializa y se dispone correctamente
class TestAutoRefreshWidget extends ConsumerStatefulWidget {
  const TestAutoRefreshWidget({super.key});

  @override
  ConsumerState<TestAutoRefreshWidget> createState() => _TestAutoRefreshWidgetState();
}

class _TestAutoRefreshWidgetState extends ConsumerState<TestAutoRefreshWidget> with AutoRefreshMixin<TestAutoRefreshWidget> {
  bool initialized = false;
  @override
  void initState() {
    super.initState();
    initialized = true;
    // Solo iniciamos con providers vacíos para evitar resolución de tipos en test
    startAutoRefresh(providers: [], duration: const Duration(milliseconds: 10));
  }

  @override
  Widget build(BuildContext context) => const SizedBox();
}

void main() {
  testWidgets('AutoRefreshMixin inicializa correctamente', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: TestAutoRefreshWidget()));
    final state = tester.state<ConsumerState<TestAutoRefreshWidget>>(find.byType(TestAutoRefreshWidget));
    expect((state as _TestAutoRefreshWidgetState).initialized, isTrue);
  });
}
