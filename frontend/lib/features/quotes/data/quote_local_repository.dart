import 'package:drift/drift.dart';
import '../../../core/offline/app_database.dart';
import '../../../core/offline/base_local_repository.dart';

class QuoteLocalRepository implements LocalRepository<QuotesLocalData, QuotesLocalCompanion> {
  final AppDatabase _db;

  QuoteLocalRepository(this._db);

  @override
  Table get table => _db.quotesLocal;

  @override
  Future<List<QuotesLocalData>> getAll() => _db.getAllQuotes();

  @override
  Future<QuotesLocalData?> getById(String id) async {
    return await (_db.select(_db.quotesLocal)..where((t) => t.id.equals(id))).getSingleOrNull();
  }

  @override
  Future<void> upsert(Map<String, dynamic> json) => _db.upsertQuote(json);

  @override
  Future<void> delete(String id) => _db.deleteQuote(id);
}
