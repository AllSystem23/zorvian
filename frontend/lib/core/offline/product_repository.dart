import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/auth_provider.dart';
import '../../features/products/providers/product_provider.dart';
import '../network/dio_client.dart';
import 'app_database.dart';
import 'connectivity_monitor.dart';
import 'sync_engine.dart';
import 'sync_orchestrator.dart';

class ProductRepository {
  final AppDatabase _db;
  final DioClient _dio;
  final SyncEngine _syncEngine;
  final Ref _ref;

  ProductRepository(this._db, this._dio, this._syncEngine, this._ref);

  bool get _isOnline =>
      _ref.read(connectivityProvider) == ConnectivityStatus.online;

  Future<List<ProductItem>> getAll() async {
    try {
      if (_isOnline) {
        try {
          await _syncEngine.push().timeout(const Duration(seconds: 5));
        } catch (_) {
          // push failed, continue to local fallback
        }
        try {
          final since = await _syncEngine.lastSyncedAt('Product');
          await _syncEngine
              .pull('Product', since: since)
              .timeout(const Duration(seconds: 10));
        } catch (_) {
          // pull failed, fallback to local
        }
      }
      final local = await _db.getAllProducts();
      return local
          .map(
            (p) => ProductItem(
              id: p.id,
              code: p.code,
              name: p.name,
              description: p.description,
              categoryName: p.categoryName,
              brandName: p.brandName,
              price: p.price,
              cost: p.cost,
              stock: p.stock,
              minStock: p.minStock,
              maxStock: p.maxStock,
              unit: p.unit,
              isActive: p.isActive,
            ),
          )
          .toList();
    } catch (_) {
      return [];
    }
  }

  Future<Map<String, dynamic>?> getById(String id) async {
    if (_isOnline) {
      try {
        final r = await _dio.get('products/$id');
        return r.data as Map<String, dynamic>;
      } catch (_) {}
    }
    final local = await _db.getProductById(id);
    if (local == null) return null;
    return {
      'id': local.id,
      'code': local.code,
      'name': local.name,
      'description': local.description,
      'categoryName': local.categoryName,
      'brandName': local.brandName,
      'sellingPrice': local.price,
      'cost': local.cost,
      'stock': local.stock,
      'minStock': local.minStock,
      'maxStock': local.maxStock,
      'unit': local.unit,
      'isActive': local.isActive,
    };
  }

  Future<void> create(Map<String, dynamic> data) async {
    if (_isOnline) {
      try {
        final r = await _dio.post('products', data: data);
        await _db.upsertProduct(r.data as Map<String, dynamic>);
        return;
      } catch (_) {}
    }
    final tempId = 'local_${DateTime.now().millisecondsSinceEpoch}';
    data['id'] = tempId;
    await _db.upsertProduct(data);
    await _syncEngine.enqueueMutation('Product', tempId, 'created', data);
  }

  Future<void> update(String id, Map<String, dynamic> data) async {
    if (_isOnline) {
      try {
        final r = await _dio.put('products/$id', data: data);
        await _db.upsertProduct(r.data as Map<String, dynamic>);
        await _syncEngine.push();
        return;
      } catch (_) {}
    }
    await _db.upsertProduct(data);
    await _syncEngine.enqueueMutation('Product', id, 'updated', data);
  }

  Future<void> delete(String id) async {
    if (_isOnline) {
      try {
        await _dio.delete('products/$id');
        await _db.deleteProduct(id);
        await _syncEngine.push();
        return;
      } catch (_) {}
    }
    await _db.deleteProduct(id);
    await _syncEngine.enqueueMutation('Product', id, 'deleted', null);
  }
}

final productRepositoryProvider = Provider<ProductRepository>((ref) {
  final db = ref.read(appDatabaseProvider);
  final dio = ref.read(dioClientProvider);
  final syncEngine = ref.read(syncEngineProvider);
  return ProductRepository(db, dio, syncEngine, ref);
});

final productRepositoryAutoSyncProvider = Provider<void>((ref) {
  ref.watch(autoSyncProvider);
  ref.watch(productRepositoryProvider);
});
