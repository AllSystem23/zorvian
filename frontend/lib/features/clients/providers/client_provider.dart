import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class ClientItem {
  final String id;
  final String code;
  final String fullName;
  final String? email;
  final String? phone;
  final String? address;
  final String? city;
  final String? state;
  final String? references;
  final String? documentNumber;
  final String type;
  final bool isActive;
  final double creditLimit;
  final double totalSales;

  const ClientItem({
    required this.id, required this.code, required this.fullName,
    this.email, this.phone, this.address, this.city, this.state, this.references,
    this.documentNumber,
    required this.type, required this.isActive,
    required this.creditLimit, required this.totalSales,
  });

  factory ClientItem.fromJson(Map<String, dynamic> j) => ClientItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    fullName: j['fullName'] as String? ?? '',
    email: j['email'] as String?,
    phone: j['phone'] as String?,
    address: j['address'] as String?,
    city: j['city'] as String?,
    state: j['state'] as String?,
    references: j['references'] as String?,
    documentNumber: j['identificationNumber'] as String? ?? j['documentNumber'] as String?,
    type: j['type'] as String? ?? 'Person',
    isActive: j['isActive'] as bool? ?? true,
    creditLimit: (j['creditLimit'] as num?)?.toDouble() ?? 0,
    totalSales: (j['totalSales'] as num?)?.toDouble() ?? 0,
  );
}

final class ClientState {
  final List<ClientItem> items;
  final bool loading;
  final String? error;
  const ClientState({this.items = const [], this.loading = false, this.error});
  ClientState copyWith({List<ClientItem>? items, bool? loading, String? error}) =>
    ClientState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class ClientNotifier extends Notifier<ClientState> {
  @override
  ClientState build() => const ClientState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('clients');
      final data = r.data as List;
      state = ClientState(items: data.map((e) => ClientItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar clientes', loading: false);
    }
  }
}

final clientProvider = NotifierProvider<ClientNotifier, ClientState>(ClientNotifier.new);
