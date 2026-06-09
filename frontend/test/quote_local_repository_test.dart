import 'package:flutter_test/flutter_test.dart';
import 'package:drift/native.dart';
import 'package:nexora/core/offline/app_database.dart';
import 'package:nexora/features/quotes/data/quote_local_repository.dart';

void main() {
  late AppDatabase database;
  late QuoteLocalRepository repository;

  setUp(() {
    database = AppDatabase.forTesting(NativeDatabase.memory());
    repository = QuoteLocalRepository(database);
  });

  tearDown(() async {
    await database.close();
  });

  test('QuoteLocalRepository upserts and retrieves quote', () async {
    final quoteJson = {
      'id': 'test-id',
      'quoteNumber': 'COT-001',
      'clientName': 'Test Client',
      'total': 100.0,
      'status': 'pending'
    };

    await repository.upsert(quoteJson);
    final quote = await repository.getById('test-id');

    expect(quote, isNotNull);
    expect(quote!.id, 'test-id');
    expect(quote.clientName, 'Test Client');
  });
}
