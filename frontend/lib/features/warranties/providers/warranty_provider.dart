import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class WarrantyItem {
  final String id;
  final String warrantyNumber;
  final String clientName;
  final String productName;
  final String? endDate;
  final String status;

  const WarrantyItem({
    required this.id, required this.warrantyNumber, required this.clientName,
    required this.productName, this.endDate, required this.status,
  });

  factory WarrantyItem.fromJson(Map<String, dynamic> j) => WarrantyItem(
    id: j['id'] as String,
    warrantyNumber: j['warrantyNumber'] as String? ?? '',
    clientName: j['clientName'] as String? ?? '',
    productName: j['productName'] as String? ?? '',
    endDate: j['endDate'] as String?,
    status: j['status'] as String? ?? '',
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

class WarrantyNotifier extends Notifier<WarrantyState> {
  @override
  WarrantyState build() => const WarrantyState();

  Future<void> load({int page = 1, int pageSize = 20, String? search, String? status}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
      };
      if (search != null && search.isNotEmpty) params['search'] = search;
      if (status != null && status != 'all') params['status'] = status;
      final r = await dio.get('warranties', params: params);
      final data = r.data;
      final items = (data['items'] as List).map((e) => WarrantyItem.fromJson(e)).toList();
      state = WarrantyState(items: items);
    } catch (_) {
      state = state.copyWith(error: 'Error al cargar garantías', loading: false);
    }
  }
}

final warrantyProvider = NotifierProvider<WarrantyNotifier, WarrantyState>(WarrantyNotifier.new);
