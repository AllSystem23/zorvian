import 'package:flutter/material.dart';
import 'package:zorvian/core/widgets/app_scaffold.dart';
import 'package:zorvian/features/warranties/providers/warranty_dashboard_provider.dart';
import 'package:provider/provider.dart';

class WarrantyDashboardPage extends StatelessWidget {
  const WarrantyDashboardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: 'Dashboard de Garantías',
      body: ChangeNotifierProvider(
        create: (_) => WarrantyDashboardProvider()..fetchMetrics(),
        child: Consumer<WarrantyDashboardProvider>(
          builder: (context, provider, _) {
            if (provider.isLoading) {
              return const Center(child: CircularProgressIndicator());
            }
            final metrics = provider.metrics;
            return GridView.count(
              crossAxisCount: 2,
              padding: const EdgeInsets.all(16),
              children: [
                _buildMetricCard(context, 'Total Activas', '${metrics?.totalActive ?? 0}', Icons.assignment),
                _buildMetricCard(context, 'Brechas SLA', '${metrics?.totalBreachedSla ?? 0}', Icons.warning, color: Colors.red),
                _buildMetricCard(context, 'Registradas', '${metrics?.registeredCount ?? 0}', Icons.fiber_new),
                _buildMetricCard(context, 'En Diagnóstico', '${metrics?.inDiagnosisCount ?? 0}', Icons.biotech),
              ],
            );
          },
        ),
      ),
    );
  }

  Widget _buildMetricCard(BuildContext context, String title, String value, IconData icon, {Color color = Colors.blue}) {
    return Card(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 48, color: color),
          Text(title, style: Theme.of(context).textTheme.titleMedium),
          Text(value, style: Theme.of(context).textTheme.headlineLarge),
        ],
      ),
    );
  }
}
