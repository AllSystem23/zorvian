import 'package:drift/drift.dart';
import 'connection_native.dart' if (dart.library.js) 'connection_web.dart';

part 'app_database.g.dart';

class ProductsLocal extends Table {
  TextColumn get id => text()();
  TextColumn get code => text()();
  TextColumn get name => text()();
  TextColumn? get description => text().nullable()();
  TextColumn? get categoryName => text().nullable()();
  TextColumn? get brandName => text().nullable()();
  RealColumn get price => real()();
  RealColumn? get cost => real().nullable()();
  RealColumn get stock => real()();
  RealColumn get minStock => real()();
  RealColumn get maxStock => real()();
  TextColumn get unit => text()();
  BoolColumn get isActive => boolean()();
  IntColumn get syncedAt => integer()();

  @override
  Set<Column> get primaryKey => {id};
}

class PendingMutations extends Table {
  TextColumn get id => text()();
  TextColumn get entity => text()();
  TextColumn get entityId => text()();
  TextColumn get operation => text()();
  TextColumn? get payloadJson => text().nullable()();
  TextColumn get clientMutationId => text()();
  DateTimeColumn get createdAt => dateTime()();
  IntColumn get attempts => integer()();

  @override
  Set<Column> get primaryKey => {id};
}

class SyncState extends Table {
  TextColumn get entity => text()();
  DateTimeColumn get lastSyncedAt => dateTime()();

  @override
  Set<Column> get primaryKey => {entity};
}

class QuotesLocal extends Table {
  TextColumn get id => text()();
  TextColumn get quoteNumber => text()();
  TextColumn get clientName => text()();
  RealColumn get total => real()();
  TextColumn get status => text()();
  IntColumn get updatedAt => integer()(); // Unix timestamp

  @override
  Set<Column> get primaryKey => {id};
}

@DriftDatabase(tables: [ProductsLocal, QuotesLocal, PendingMutations, SyncState])
class AppDatabase extends _$AppDatabase {
  AppDatabase() : super(openConnection());
  AppDatabase.forTesting(super.connection);

  @override
  int get schemaVersion => 1;

  Future<void> upsertQuote(Map<String, dynamic> json) async {
    final now = DateTime.now().millisecondsSinceEpoch;
    await into(quotesLocal).insertOnConflictUpdate(QuotesLocalCompanion(
      id: Value(json['id'] as String),
      quoteNumber: Value(json['quoteNumber'] as String),
      clientName: Value(json['clientName'] as String),
      total: Value((json['total'] as num).toDouble()),
      status: Value(json['status'] as String),
      updatedAt: Value(now),
    ));
  }

  Future<void> deleteQuote(String id) async {
    await (delete(quotesLocal)..where((t) => t.id.equals(id))).go();
  }

  Future<List<QuotesLocalData>> getAllQuotes() async {
    return await select(quotesLocal).get();
  }

  Future<void> upsertProduct(Map<String, dynamic> json) async {
    final existing = await (select(productsLocal)
          ..where((t) => t.id.equals(json['id'] as String)))
        .getSingleOrNull();

    final now = DateTime.now().millisecondsSinceEpoch;

    if (existing != null) {
      await (update(productsLocal)
            ..where((t) => t.id.equals(json['id'] as String)))
          .write(ProductsLocalCompanion(
        code: Value(json['code'] as String? ?? existing.code),
        name: Value(json['name'] as String? ?? existing.name),
        description: Value(json['description'] as String?),
        categoryName: Value(json['categoryName'] as String?),
        brandName: Value(json['brandName'] as String?),
        price: Value((json['sellingPrice'] as num? ?? json['price'] as num? ?? existing.price).toDouble()),
        cost: Value((json['costPrice'] as num? ?? json['cost'] as num? ?? existing.cost)?.toDouble()),
        stock: Value((json['stock'] as num? ?? existing.stock).toDouble()),
        minStock: Value((json['minStock'] as num? ?? existing.minStock).toDouble()),
        maxStock: Value((json['maxStock'] as num? ?? existing.maxStock).toDouble()),
        unit: Value(json['unitOfMeasure'] as String? ?? json['unit'] as String? ?? existing.unit),
        isActive: Value(json['isActive'] as bool? ?? existing.isActive),
        syncedAt: Value(now),
      ));
    } else {
      await into(productsLocal).insert(ProductsLocalCompanion(
        id: Value(json['id'] as String),
        code: Value(json['code'] as String? ?? ''),
        name: Value(json['name'] as String? ?? ''),
        description: Value(json['description'] as String?),
        categoryName: Value(json['categoryName'] as String?),
        brandName: Value(json['brandName'] as String?),
        price: Value((json['sellingPrice'] as num? ?? json['price'] as num? ?? 0).toDouble()),
        cost: Value((json['costPrice'] as num? ?? json['cost'] as num?)?.toDouble()),
        stock: Value((json['stock'] as num? ?? 0).toDouble()),
        minStock: Value((json['minStock'] as num? ?? 0).toDouble()),
        maxStock: Value((json['maxStock'] as num? ?? 0).toDouble()),
        unit: Value(json['unitOfMeasure'] as String? ?? json['unit'] as String? ?? 'pz'),
        isActive: Value(json['isActive'] as bool? ?? true),
        syncedAt: Value(now),
      ));
    }
  }

  Future<void> deleteProduct(String id) async {
    await (delete(productsLocal)..where((t) => t.id.equals(id))).go();
  }

  Future<List<ProductsLocalData>> getAllProducts() async {
    return await select(productsLocal).get();
  }

  Future<ProductsLocalData?> getProductById(String id) async {
    return await (select(productsLocal)..where((t) => t.id.equals(id))).getSingleOrNull();
  }

  Future<List<String>> getAllTrackedEntities() async {
    final rows = await select(syncState).get();
    return rows.map((r) => r.entity).toList();
  }
}
