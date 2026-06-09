import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'app_database.dart';
import 'connectivity_monitor.dart';
import 'sync_engine.dart';

class SyncOrchestrator {
  final SyncEngine _engine;
  final AppDatabase _db;

  SyncOrchestrator(this._engine, this._db);

  Future<int> syncAll() async {
    await _engine.push();

    final entities = await _db.getAllTrackedEntities();
    var pulled = 0;
    for (final entity in entities) {
      final since = await _engine.lastSyncedAt(entity);
      await _engine.pull(entity, since: since);
      pulled++;
    }
    return pulled;
  }
}

final syncOrchestratorProvider = Provider<SyncOrchestrator>((ref) {
  final engine = ref.read(syncEngineProvider);
  final db = ref.read(appDatabaseProvider);
  return SyncOrchestrator(engine, db);
});

final autoSyncProvider = Provider<void>((ref) {
  ref.watch(connectivityProvider);
  final orchestrator = ref.read(syncOrchestratorProvider);

  ref.listen(connectivityProvider, (prev, next) {
    if (prev == ConnectivityStatus.offline && next == ConnectivityStatus.online) {
      orchestrator.syncAll();
    }
  });
});
