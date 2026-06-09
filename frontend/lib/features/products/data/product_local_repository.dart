import 'package:drift/drift.dart';
import '../../../core/offline/app_database.dart';
import '../../../core/offline/base_local_repository.dart';

class ProductLocalRepository implements LocalRepository<ProductsLocalData, ProductsLocalCompanion> {
  final AppDatabase _db;

  ProductLocalRepository(this._db);

  @override
  Table get table => _db.productsLocal;

  @override
  Future<List<ProductsLocalData>> getAll() => _db.getAllProducts();

  @override
  Future<ProductsLocalData?> getById(String id) => _db.getProductById(id);

  @override
  Future<void> upsert(Map<String, dynamic> json) => _db.upsertProduct(json);

  @override
  Future<void> delete(String id) => _db.deleteProduct(id);
}
