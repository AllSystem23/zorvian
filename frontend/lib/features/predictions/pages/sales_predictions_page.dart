import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

class SalesPredictionsPage extends ConsumerStatefulWidget {
  const SalesPredictionsPage({super.key});

  @override
  ConsumerState<SalesPredictionsPage> createState() => _SalesPredictionsPageState();
}

class _SalesPredictionsPageState extends ConsumerState<SalesPredictionsPage> {
  dynamic _predictions;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final res = await dio.get('sales/predictions/next-month');
      _predictions = res.data;
    } catch (_) {}
    setState(() => _loading = false);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : Column(
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                  child: Row(
                    children: [
                      const Text('Predicción de Ventas', style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                      const Spacer(),
                      IconButton(icon: const Icon(Icons.refresh), onPressed: _load),
                    ],
                  ),
                ),
                Expanded(
                  child: SingleChildScrollView(
                    padding: const EdgeInsets.all(24),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        ZCard(
                          child: Column(
                            children: [
                              const Icon(Icons.trending_up, size: 48, color: ZColors.brandAccent),
                              const SizedBox(height: 16),
                              Text('Ventas Proyectadas - Próximo Mes',
                                  style: ZTypography.headlineSmall),
                              const SizedBox(height: 24),
                              ZStatCard(
                                title: 'PREDICCIÓN',
                                value: _predictions != null
                                    ? 'C\$${(_predictions['predictedTotal'] as num?)?.toStringAsFixed(2) ?? '0.00'}'
                                    : 'C\$0.00',
                                label: 'Confianza: ${_predictions != null ? (_predictions['confidence'] as num?)?.toStringAsFixed(1) ?? '0' : '0'}%',
                                icon: Icons.analytics,
                                variant: ZStatVariant.primary,
                              ),
                              if (_predictions != null && _predictions['byCategory'] != null) ...[
                                const SizedBox(height: 24),
                                const Text('Desglose por Categoría',
                                    style: TextStyle(fontWeight: FontWeight.bold)),
                                const SizedBox(height: 16),
                                ...(_predictions['byCategory'] as List).map((c) => ListTile(
                                  leading: const Icon(Icons.category),
                                  title: Text(c['category'] ?? ''),
                                  trailing: Text('C\$${(c['amount'] as num?)?.toStringAsFixed(2) ?? '0.00'}'),
                                )),
                              ],
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}
