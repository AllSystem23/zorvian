import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetCatalogItem {
  final String id;
  final String name;
  final String countryCode;
  final bool isActive;

  const FleetCatalogItem({required this.id, required this.name, this.countryCode = '', this.isActive = true});

  factory FleetCatalogItem.fromJson(Map<String, dynamic> j) => FleetCatalogItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    countryCode: j['countryCode'] as String? ?? '',
    isActive: j['isActive'] as bool? ?? true,
  );
}

final _silentOptions = Options(extra: {'suppressGlobalError': true});

final vehicleBrandListProvider =
    FutureProvider.autoDispose<List<FleetCatalogItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('fleet/brands', options: _silentOptions);
    return _parseCatalogList(r.data);
  } catch (_) {
    return [];
  }
});

final vehicleTypeListProvider =
    FutureProvider.autoDispose<List<FleetCatalogItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('fleet/vehicle-types', options: _silentOptions);
    return _parseCatalogList(r.data);
  } catch (_) {
    return [];
  }
});

final fuelTypeListProvider =
    FutureProvider.autoDispose<List<FleetCatalogItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('fleet/fuel-types', options: _silentOptions);
    return _parseCatalogList(r.data);
  } catch (_) {
    return [];
  }
});

/// License categories filtered by country code.
final driverLicenseCategoryListProvider =
    FutureProvider.autoDispose<List<FleetCatalogItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('fleet/driver-license-categories', options: _silentOptions);
    return _parseCatalogList(r.data);
  } catch (_) {
    return [];
  }
});

/// License categories filtered by country code.
final driverLicenseCategoryByCountryProvider =
    FutureProvider.autoDispose.family<List<FleetCatalogItem>, String>(
  (ref, countryCode) async {
    final dio = ref.read(dioClientProvider);
    final r = await dio.get('fleet/driver-license-categories',
        params: {'countryCode': countryCode});
    return _parseCatalogList(r.data);
  },
);

final documentTypeListProvider =
    FutureProvider.autoDispose<List<DocumentTypeItem>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final r = await dio.get('fleet/document-types', options: _silentOptions);
    final data = r.data;
    final list = data is List ? data : (data['items'] as List? ?? []);
    return (list).map((e) => DocumentTypeItem.fromJson(e as Map<String, dynamic>)).toList();
  } catch (_) {
    return [];
  }
});

List<FleetCatalogItem> _parseCatalogList(dynamic data) {
  final list = data is List ? data : (data['items'] as List? ?? []);
  return list.map((e) => FleetCatalogItem.fromJson(e as Map<String, dynamic>)).toList();
}

final class DocumentTypeItem {
  final String id;
  final String name;
  final String entityType;
  final bool hasExpiry;
  final int alertDaysBefore;
  final bool isRequired;

  const DocumentTypeItem({
    required this.id,
    required this.name,
    required this.entityType,
    this.hasExpiry = false,
    this.alertDaysBefore = 0,
    this.isRequired = false,
  });

  factory DocumentTypeItem.fromJson(Map<String, dynamic> j) => DocumentTypeItem(
    id: j['id'] as String,
    name: j['name'] as String? ?? '',
    entityType: j['entityType'] as String? ?? '',
    hasExpiry: j['hasExpiry'] as bool? ?? false,
    alertDaysBefore: j['alertDaysBefore'] as int? ?? 0,
    isRequired: j['isRequired'] as bool? ?? false,
  );
}
