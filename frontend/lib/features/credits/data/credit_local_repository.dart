import 'package:drift/drift.dart';
import '../../../core/offline/app_database.dart';
import '../../../core/offline/base_local_repository.dart';

class CreditLocalRepository implements LocalRepository<CreditsLocalData, CreditsLocalCompanion> {
  final AppDatabase _db;

  CreditLocalRepository(this._db);

  @override
  Table get table => _db.creditsLocal;

  @override
  Future<List<CreditsLocalData>> getAll() => _db.getAllCredits();

  @override
  Future<CreditsLocalData?> getById(String id) => _db.getCreditById(id);

  @override
  Future<void> upsert(Map<String, dynamic> json) => _db.upsertCredit(json);

  @override
  Future<void> delete(String id) => _db.deleteCredit(id);
}
