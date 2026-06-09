import 'package:drift/drift.dart';

/// Interface for entities that support offline synchronization.
abstract class ISyncable {
  String get id;
  DateTime get updatedAt;
  bool get isDeleted;
}

/// Generic interface for local entity repositories.
abstract class LocalRepository<T extends DataClass, C extends UpdateCompanion<T>> {
  Future<List<T>> getAll();
  Future<T?> getById(String id);
  Future<void> upsert(Map<String, dynamic> json);
  Future<void> delete(String id);
  
  // Necessary for generic sync engine access
  Table get table;
}
