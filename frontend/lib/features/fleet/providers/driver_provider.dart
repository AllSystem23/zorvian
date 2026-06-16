import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart' show dioClientProvider;

final class FleetDriverItem {
  final String id;
  final String firstName;
  final String lastName;
  final String fullName;
  final String idDocument;
  final String phone;
  final String email;
  final String licenseNumber;
  final String? licenseExpiryDate;
  final String status;
  final String? branchName;
  final String? licenseCategoryName;

  const FleetDriverItem({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.fullName,
    required this.idDocument,
    required this.phone,
    required this.email,
    required this.licenseNumber,
    this.licenseExpiryDate,
    required this.status,
    this.branchName,
    this.licenseCategoryName,
  });

  factory FleetDriverItem.fromJson(Map<String, dynamic> j) => FleetDriverItem(
    id: j['id'] as String,
    firstName: j['firstName'] as String? ?? '',
    lastName: j['lastName'] as String? ?? '',
    fullName: j['fullName'] as String? ?? '',
    idDocument: j['idDocument'] as String? ?? '',
    phone: j['phone'] as String? ?? '',
    email: j['email'] as String? ?? '',
    licenseNumber: j['licenseNumber'] as String? ?? '',
    licenseExpiryDate: j['licenseExpiryDate'] as String?,
    status: j['status'] as String? ?? 'Active',
    branchName: j['branchName'] as String?,
    licenseCategoryName: j['licenseCategoryName'] as String?,
  );
}

final class DriverState {
  final List<FleetDriverItem> items;
  final bool loading;
  final String? error;

  const DriverState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  DriverState copyWith({
    List<FleetDriverItem>? items,
    bool? loading,
    String? error,
  }) => DriverState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class DriverNotifier extends Notifier<DriverState> {
  @override
  DriverState build() => const DriverState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('fleet/drivers');
      final data = r.data;
      final items = (data['items'] as List)
          .map((e) => FleetDriverItem.fromJson(e as Map<String, dynamic>))
          .toList();
      state = DriverState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar conductores', loading: false);
    }
  }
}

final driverProvider = NotifierProvider<DriverNotifier, DriverState>(DriverNotifier.new);
