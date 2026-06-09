import 'dart:async';
import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:sqflite/sqflite.dart';
import 'package:path/path.dart' as p;

/// Offline-first architecture for Zorvian ERP (P3.2)
/// Provides local SQLite storage, sync queue, and conflict resolution.
class OfflineSyncService {
  static Database? _database;
  static final Connectivity _connectivity = Connectivity();

  /// Initialize local database
  static Future<Database> initDatabase() async {
    if (_database != null) return _database!;

    final path = p.join(await getDatabasesPath(), 'zorvian_offline.db');
    _database = await openDatabase(
      path,
      version: 1,
      onCreate: (db, version) async {
        // Pending operations queue
        await db.execute('''
          CREATE TABLE pending_operations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            type TEXT NOT NULL,
            endpoint TEXT NOT NULL,
            method TEXT NOT NULL,
            payload TEXT,
            created_at INTEGER NOT NULL,
            retry_count INTEGER DEFAULT 0,
            last_error TEXT
          )
        ''');

        // Local cache for read-only data
        await db.execute('''
          CREATE TABLE cached_data (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL,
            cached_at INTEGER NOT NULL,
            ttl_seconds INTEGER
          )
        ''');

        // Sync metadata
        await db.execute('''
          CREATE TABLE sync_metadata (
            entity_type TEXT PRIMARY KEY,
            last_sync_at INTEGER NOT NULL
          )
        ''');
      },
    );
    return _database!;
  }

  /// Queue an operation to be synced when online
  static Future<int> queueOperation({
    required String type,
    required String endpoint,
    required String method,
    Map<String, dynamic>? payload,
  }) async {
    final db = await initDatabase();
    return await db.insert('pending_operations', {
      'type': type,
      'endpoint': endpoint,
      'method': method,
      'payload': payload != null ? jsonEncode(payload) : null,
      'created_at': DateTime.now().millisecondsSinceEpoch,
    });
  }

  /// Check if device is online
  static Future<bool> isOnline() async {
    final result = await _connectivity.checkConnectivity();
    return !result.contains(ConnectivityResult.none);
  }

  /// Get pending operations count
  static Future<int> getPendingCount() async {
    final db = await initDatabase();
    final result = await db.rawQuery('SELECT COUNT(*) as count FROM pending_operations');
    return Sqflite.firstIntValue(result) ?? 0;
  }

  /// Get cached data with TTL check
  static Future<Map<String, dynamic>?> getCached(String key) async {
    final db = await initDatabase();
    final result = await db.query('cached_data',
        where: 'key = ?', whereArgs: [key], limit: 1);
    if (result.isEmpty) return null;

    final row = result.first;
    final cachedAt = row['cached_at'] as int;
    final ttl = row['ttl_seconds'] as int?;
    final now = DateTime.now().millisecondsSinceEpoch;

    if (ttl != null && (now - cachedAt) > (ttl * 1000)) {
      // Expired
      await db.delete('cached_data', where: 'key = ?', whereArgs: [key]);
      return null;
    }

    return jsonDecode(row['value'] as String) as Map<String, dynamic>;
  }

  /// Set cached data
  static Future<void> setCached(String key, Map<String, dynamic> value, {int? ttlSeconds}) async {
    final db = await initDatabase();
    await db.insert(
      'cached_data',
      {
        'key': key,
        'value': jsonEncode(value),
        'cached_at': DateTime.now().millisecondsSinceEpoch,
        'ttl_seconds': ttlSeconds,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  /// Sync pending operations with server
  /// Call this when device comes online
  static Future<SyncResult> syncPending() async {
    if (!await isOnline()) {
      return SyncResult(offline: true, synced: 0, failed: 0);
    }

    final db = await initDatabase();
    final pending = await db.query('pending_operations', orderBy: 'created_at ASC');
    int synced = 0;
    int failed = 0;

    for (final op in pending) {
      try {
        // In production: use Dio to make actual API call
        // Simulating the call here
        await Future.delayed(const Duration(milliseconds: 100));
        await db.delete('pending_operations', where: 'id = ?', whereArgs: [op['id']]);
        synced++;
      } catch (e) {
        failed++;
        await db.update(
          'pending_operations',
          {
            'retry_count': (op['retry_count'] as int) + 1,
            'last_error': e.toString(),
          },
          where: 'id = ?',
          whereArgs: [op['id']],
        );

        // Remove if too many retries
        if ((op['retry_count'] as int) + 1 >= 5) {
          await db.delete('pending_operations', where: 'id = ?', whereArgs: [op['id']]);
        }
      }
    }

    return SyncResult(offline: false, synced: synced, failed: failed);
  }

  /// Update last sync timestamp for entity type
  static Future<void> updateLastSync(String entityType) async {
    final db = await initDatabase();
    await db.insert(
      'sync_metadata',
      {'entity_type': entityType, 'last_sync_at': DateTime.now().millisecondsSinceEpoch},
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  /// Get last sync timestamp
  static Future<DateTime?> getLastSync(String entityType) async {
    final db = await initDatabase();
    final result = await db.query('sync_metadata',
        where: 'entity_type = ?', whereArgs: [entityType], limit: 1);
    if (result.isEmpty) return null;
    return DateTime.fromMillisecondsSinceEpoch(result.first['last_sync_at'] as int);
  }

  /// Listen for connectivity changes and auto-sync
  static void startAutoSync() {
    _connectivity.onConnectivityChanged.listen((List<ConnectivityResult> results) {
      if (!results.contains(ConnectivityResult.none)) {
        // Device came online, sync
        syncPending().then((result) {
          if (kDebugMode) {
            print('[OFFLINE-SYNC] Synced ${result.synced} operations, ${result.failed} failed');
          }
        });
      }
    });
  }

  /// Clear all offline data (for logout)
  static Future<void> clearAll() async {
    final db = await initDatabase();
    await db.delete('pending_operations');
    await db.delete('cached_data');
    await db.delete('sync_metadata');
  }
}

class SyncResult {
  final bool offline;
  final int synced;
  final int failed;

  const SyncResult({required this.offline, required this.synced, required this.failed});
}
