import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class SupplierItem {
  final String id;
  final String code;
  final String name;
  final String? contactName;
  final String? phone;
  final String? email;
  final String? address;
  final bool isActive;
  const SupplierItem({
    required this.id, required this.code, required this.name,
    this.contactName, this.phone, this.email, this.address, required this.isActive,
  });
  factory SupplierItem.fromJson(Map<String, dynamic> j) => SupplierItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    name: j['name'] as String? ?? '',
    contactName: j['contactName'] as String?,
    phone: j['phone'] as String?,
    email: j['email'] as String?,
    address: j['address'] as String?,
    isActive: j['isActive'] as bool? ?? true,
  );
}

final class SupplierState {
  final List<SupplierItem> items;
  final bool loading;
  final String? error;
  const SupplierState({this.items = const [], this.loading = false, this.error});
  SupplierState copyWith({List<SupplierItem>? items, bool? loading, String? error}) =>
    SupplierState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class SupplierNotifier extends Notifier<SupplierState> {
  @override
  SupplierState build() => const SupplierState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('suppliers');
      final data = r.data as List;
      state = SupplierState(items: data.map((e) => SupplierItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar proveedores', loading: false);
    }
  }
}

final supplierProvider = NotifierProvider<SupplierNotifier, SupplierState>(SupplierNotifier.new);
