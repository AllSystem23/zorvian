import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/network/public_dio_client.dart';
import '../models/warranty_model.dart';

class PublicWarrantyTracking {
  final WarrantyTrackingModel? warranty;
  final bool isLoading;
  final String? error;

  const PublicWarrantyTracking({this.warranty, this.isLoading = false, this.error});
}

class PublicWarrantyNotifier extends Notifier<PublicWarrantyTracking> {
  @override
  PublicWarrantyTracking build() => const PublicWarrantyTracking();

  Future<void> track(String warrantyNumber, String phoneNumber) async {
    state = const PublicWarrantyTracking(isLoading: true);
    try {
      final dio = PublicDioClient();
      final response = await dio.get('public/warranties/track/$warrantyNumber?phoneNumber=$phoneNumber');
      final warranty = WarrantyTrackingModel.fromJson(response.data as Map<String, dynamic>);
      state = PublicWarrantyTracking(warranty: warranty);
    } catch (e) {
      state = const PublicWarrantyTracking(error: 'No se encontró la garantía. Verifique el número y su teléfono.');
    }
  }

  void clear() {
    state = const PublicWarrantyTracking();
  }
}

final publicWarrantyProvider = NotifierProvider<PublicWarrantyNotifier, PublicWarrantyTracking>(
  PublicWarrantyNotifier.new,
);
