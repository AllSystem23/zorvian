import 'package:drift/native.dart';
import 'package:zorvian/core/offline/app_database.dart';

/// `AppDatabase` en memoria para las pantallas offline-first.
///
/// El `AppDatabase` real abre un archivo a través de sqflite, que no existe en
/// widget tests: la consulta nunca completa y la página se queda en el spinner
/// (y `pumpAndSettle` expira). `AppDatabase.forTesting` acepta cualquier
/// `QueryExecutor`, así que basta un `NativeDatabase.memory()`.
///
/// Recordar cerrarla: `addTearDown(db.close)`.
AppDatabase memoryDb() => AppDatabase.forTesting(NativeDatabase.memory());
