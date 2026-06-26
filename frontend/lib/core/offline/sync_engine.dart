import 'dart:convert';
import 'package:drift/drift.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:uuid/uuid.dart';
import '../../auth/auth_provider.dart';
import '../network/dio_client.dart';
import 'app_database.dart';
import 'base_local_repository.dart';
import '../../features/products/data/product_local_repository.dart';
import '../../features/quotes/data/quote_local_repository.dart';

class SyncEngine {
  final AppDatabase _db;
  final DioClient _dio;
  final Uuid _uuid = const Uuid();
  
  // Registry of repositories
  final Map<String, LocalRepository> _repositories = {};

  SyncEngine(this._db, this._dio);

  void registerRepository(String entityName, LocalRepository repository) {
    _repositories[entityName] = repository;
  }

  Future<DateTime> pull(String entityName, {DateTime? since}) async {
    final repository = _repositories[entityName];
    if (repository == null) throw Exception('No repository registered for $entityName');

    final params = <String, dynamic>{'entity': entityName, 'take': 500};
    if (since != null) params['since'] = since.toIso8601String();

    final r = await _dio.get('sync/pull', params: params);
    final data = r.data as Map<String, dynamic>;
    final changes = data['changes'] as List;

    for (final change in changes) {
      final op = change['operation'] as String;
      final eid = change['entityId'] as String;

      if (op == 'deleted') {
        await repository.delete(eid);
      } else if (op == 'created' || op == 'updated') {
        final payload = change['payloadJson'];
        if (payload != null) {
          final json = jsonDecode(payload as String) as Map<String, dynamic>;
          await repository.upsert(json);
        }
      }
    }

    final serverTs = DateTime.parse(data['serverTimestamp'] as String);
    await _db.into(_db.syncState).insertOnConflictUpdate(
          SyncStateCompanion.insert(
            entity: entityName,
            lastSyncedAt: serverTs,
          ),
        );

    return serverTs;
  }

  Future<void> push() async {
    final pending = await _db.select(_db.pendingMutations).get();
    if (pending.isEmpty) return;

    final mutations = pending.map((m) => {
          'entityName': m.entity,
          'entityId': m.entityId,
          'operation': m.operation,
          'payloadJson': m.payloadJson,
          'clientMutationId': m.clientMutationId,
        }).toList();

    try {
      await _dio.post('sync/push', data: mutations);
      await _db.delete(_db.pendingMutations).go();
    } catch (_) {
      for (final m in pending) {
        await (_db.update(_db.pendingMutations)
              ..where((t) => t.id.equals(m.id)))
            .write(PendingMutationsCompanion(
              attempts: Value(m.attempts + 1),
            ));
      }
      rethrow;
    }
  }

  Future<void> enqueueMutation(
      String entityName, String entityId, String operation,
      [Map<String, dynamic>? payload]) async {
    await _db.into(_db.pendingMutations).insert(
          PendingMutationsCompanion.insert(
            id: _uuid.v4(),
            entity: entityName,
            entityId: entityId,
            operation: operation,
            payloadJson: payload != null ? Value(jsonEncode(payload)) : const Value.absent(),
            clientMutationId: _uuid.v4(),
            createdAt: DateTime.now().toUtc(),
            attempts: 0,
          ),
        );
  }

  Future<DateTime?> lastSyncedAt(String entityName) async {
    final row = await (_db.select(_db.syncState)
          ..where((t) => t.entity.equals(entityName)))
        .getSingleOrNull();
    return row?.lastSyncedAt;
  }
}

final syncEngineProvider = Provider<SyncEngine>((ref) {
  final db = ref.read(appDatabaseProvider);
  final dio = ref.read(dioClientProvider);
  final engine = SyncEngine(db, dio);
  engine.registerRepository('Product', ProductLocalRepository(db));
  engine.registerRepository('Quote', QuoteLocalRepository(db));
  return engine;
});

final appDatabaseProvider = Provider<AppDatabase>((ref) {
  final db = AppDatabase();
  ref.onDispose(() => db.close());
  return db;
});

final lastSyncProvider = FutureProvider.family<DateTime?, String>(
  (ref, entityName) async {
    final engine = ref.read(syncEngineProvider);
    return engine.lastSyncedAt(entityName);
  },
);
