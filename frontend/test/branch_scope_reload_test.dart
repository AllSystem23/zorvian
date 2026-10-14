import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

/// Regresión del patrón usado por los Notifiers que deben recargar al cambiar
/// de empresa/sucursal. El header cambia `branchScopeProvider`; como la página
/// sigue montada, `initState` NO re-ejecuta `load()`, así que el notifier
/// compara el scope actual con el último visto y reprograma la carga.
///
/// Nota: `ref.listen` dentro de `build()` NO sirve para esto porque el propio
/// provider se reconstruye y descarta el listener antes de dispararlo.

final scopeProbeProvider =
    NotifierProvider<ScopeProbeNotifier, String>(ScopeProbeNotifier.new);

class ScopeProbeNotifier extends Notifier<String> {
  @override
  String build() => 'company_a_branch_b';
  void set(String v) => state = v;
}

int loadCalls = 0;

final dataProbeProvider =
    NotifierProvider<DataProbeNotifier, int>(DataProbeNotifier.new);

class DataProbeNotifier extends Notifier<int> {
  static String? lastScope;

  @override
  int build() {
    final scope = ref.watch(scopeProbeProvider);
    if (lastScope != null && lastScope != scope) {
      Future.microtask(() => load());
    }
    lastScope = scope;
    return 0;
  }

  void load() {
    loadCalls++;
    state = loadCalls;
  }
}

void main() {
  setUp(() {
    loadCalls = 0;
    DataProbeNotifier.lastScope = null;
  });

  test('el primer build no dispara carga (lo hace initState de la pagina)', () {
    final container = ProviderContainer();
    container.listen(dataProbeProvider, (_, _) {});
    container.listen(scopeProbeProvider, (_, _) {});
    expect(loadCalls, 0);
    container.dispose();
  });

  test('cambiar de sucursal recarga los datos del notifier', () async {
    final container = ProviderContainer();
    container.listen(dataProbeProvider, (_, _) {});
    container.listen(scopeProbeProvider, (_, _) {});

    container.read(scopeProbeProvider.notifier).set('company_a_branch_z');

    await Future.delayed(const Duration(milliseconds: 10));

    expect(loadCalls, 1, reason: 'el cambio de scope debe recargar');
    expect(container.read(dataProbeProvider), 1);

    container.dispose();
  });

  test('cambiar de empresa tambien recarga', () async {
    final container = ProviderContainer();
    container.listen(dataProbeProvider, (_, _) {});
    container.listen(scopeProbeProvider, (_, _) {});

    container.read(scopeProbeProvider.notifier).set('company_b_branch_b');

    await Future.delayed(const Duration(milliseconds: 10));

    expect(loadCalls, 1);
    container.dispose();
  });
}
