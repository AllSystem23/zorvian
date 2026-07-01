import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class WorkshopItem {
  final String id;
  final String code;
  final String name;
  final String? contactName;
  final String? phone;
  final String? email;
  final String? city;
  final int avgResponseHours;
  final int avgRepairHours;
  final double rating;
  final bool isActive;
  final int technicianCount;

  const WorkshopItem({
    required this.id, required this.code, required this.name,
    this.contactName, this.phone, this.email, this.city,
    this.avgResponseHours = 48, this.avgRepairHours = 72,
    this.rating = 0, this.isActive = true, this.technicianCount = 0,
  });

  factory WorkshopItem.fromJson(Map<String, dynamic> j) => WorkshopItem(
    id: j['id'] as String,
    code: j['code'] as String? ?? '',
    name: j['name'] as String? ?? '',
    contactName: j['contactName'] as String?,
    phone: j['phone'] as String?,
    email: j['email'] as String?,
    city: j['city'] as String?,
    avgResponseHours: j['avgResponseHours'] as int? ?? 48,
    avgRepairHours: j['avgRepairHours'] as int? ?? 72,
    rating: (j['rating'] as num?)?.toDouble() ?? 0,
    isActive: j['isActive'] as bool? ?? true,
    technicianCount: j['technicianCount'] as int? ?? 0,
  );
}

final class WorkshopState {
  final List<WorkshopItem> items;
  final bool loading;
  final String? error;
  const WorkshopState({this.items = const [], this.loading = false, this.error});

  const WorkshopState.loading() : items = const [], loading = true, error = null;
  const WorkshopState.error(String e) : items = const [], loading = false, error = e;
}

class WorkshopNotifier extends Notifier<WorkshopState> {
  @override
  WorkshopState build() => const WorkshopState();

  Future<void> load() async {
    state = const WorkshopState.loading();
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('service-workshops');
      final list = (r.data as List).map((e) => WorkshopItem.fromJson(e)).toList();
      state = WorkshopState(items: list);
    } catch (_) {
      state = const WorkshopState.error('Error al cargar talleres');
    }
  }

  Future<void> delete(String id) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('service-workshops/$id');
      state = WorkshopState(items: state.items.where((w) => w.id != id).toList());
    } catch (_) {}
  }
}

final workshopProvider = NotifierProvider<WorkshopNotifier, WorkshopState>(WorkshopNotifier.new);
