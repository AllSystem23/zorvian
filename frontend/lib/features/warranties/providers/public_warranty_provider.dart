import 'package:flutter/material.dart';
import 'package:zorvian/core/network/public_dio_client.dart';
import 'package:zorvian/features/warranties/models/warranty_model.dart';

class PublicWarrantyProvider extends ChangeNotifier {
  WarrantyModel? _warranty;
  bool _isLoading = false;
  String? _error;

  WarrantyModel? get warranty => _warranty;
  bool get isLoading => _isLoading;
  String? get error => _error;

  Future<void> trackWarranty(String warrantyNumber, String phoneNumber) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    try {
      final response = await PublicDioClient().get('/api/v1/public/warranties/track/$warrantyNumber?phoneNumber=$phoneNumber');
      _warranty = WarrantyModel.fromJson(response.data);
    } catch (e) {
      _error = 'Error al consultar la garantía. Verifique los datos.';
      debugPrint('Error tracking warranty: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
