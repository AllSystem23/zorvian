import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class WarrantyItem {
  final String id;
  final String folio;
  final String clientName;
  final String productName;
  final String type;
  final String status;
  final String startDate;
  final String? endDate;
  final String? description;

  const WarrantyItem({
    required this.id, required this.folio, required this.clientName,
    required this.productName, required this.type, required this.status,
    required this.startDate, this.endDate, this.description,
  });

  factory WarrantyItem.fromJson(Map<String, dynamic> j) => WarrantyItem(
    id: j['id'] as String,
    folio: j['folio'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    productName: j['productName'] as String? ?? '',
    type: j['type'] as String? ?? '',
    status: j['status'] as String? ?? '',
    startDate: j['startDate'] as String? ?? '',
    endDate: j['endDate'] as String?,
    description: j['description'] as String?,
  );
}

final class WarrantyState {
  final List<WarrantyItem> items;
  final bool loading;
  final String? error;
  const WarrantyState({this.items = const [], this.loading = false, this.error});
  WarrantyState copyWith({List<WarrantyItem>? items, bool? loading, String? error}) =>
    WarrantyState(items: items ?? this.items, loading: loading ?? this.loading, error: error ?? this.error);
}

final class WarrantyNotifier extends Notifier<WarrantyState> {
  @override
  WarrantyState build() => const WarrantyState();
  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('warranties');
      final data = r.data as List;
      state = WarrantyState(items: data.map((e) => WarrantyItem.fromJson(e)).toList());
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar garantías', loading: false);
    }
  }
}

final warrantyProvider = NotifierProvider<WarrantyNotifier, WarrantyState>(WarrantyNotifier.new);
