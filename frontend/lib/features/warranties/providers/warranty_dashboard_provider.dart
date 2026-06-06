import 'package:flutter/material.dart';
import 'package:zorvian/core/api/api_client.dart'; // Assuming this exists
import 'package:zorvian/features/warranties/models/warranty_dashboard_model.dart'; // Need to create this

class WarrantyDashboardProvider extends ChangeNotifier {
  WarrantyDashboardModel? _metrics;
  bool _isLoading = false;

  WarrantyDashboardModel? get metrics => _metrics;
  bool get isLoading => _isLoading;

  Future<void> fetchMetrics() async {
    _isLoading = true;
    notifyListeners();
    try {
      final response = await ApiClient().get('/api/v1/warranty-dashboard/metrics');
      _metrics = WarrantyDashboardModel.fromJson(response.data);
    } catch (e) {
      debugPrint('Error fetching warranty dashboard metrics: $e');
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
