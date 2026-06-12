import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../core/widgets/responsive_layout.dart';

class TreasuryDashboardPage extends ConsumerWidget {
  const TreasuryDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: const Text('Gestión de Tesorería'),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () => ZCommandPalette.show(context)),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Executive Summary ──
            const Text('Resumen Financiero', style: ZTypography.titleLarge),
            const SizedBox(height: 16),
            ResponsiveGrid(
              mobileColumns: 1,
              tabletColumns: 2,
              desktopColumns: 4,
              children: [
                const ZStatCard(title: 'Saldo Bancario Total', value: 'C\$ 1,250,400.00', icon: Icons.account_balance_outlined, variant: ZStatVariant.primary),
                const ZStatCard(title: 'Depósitos Pendientes', value: '12', icon: Icons.move_to_inbox_outlined, variant: ZStatVariant.info),
                const ZStatCard(title: 'Cheques por Cobrar', value: '5', icon: Icons.payments_outlined, variant: ZStatVariant.warning),
                const ZStatCard(title: 'Conciliaciones Requeridas', value: '3', icon: Icons.compare_arrows_outlined, variant: ZStatVariant.danger),
              ],
            ),
            
            const SizedBox(height: 40),
            
            // ── Quick Operations ──
            _buildQuickOperations(context),
            
            const SizedBox(height: 40),

            // ── Recent Movements ──
            const Text('Movimientos Recientes', style: ZTypography.titleLarge),
            const SizedBox(height: 16),
            ZCard(
              padding: EdgeInsets.zero,
              child: SizedBox(
                height: 400,
                child: Center(child: Text('Cargando movimientos...', style: TextStyle(color: ZColors.neutral500))),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildQuickOperations(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Operaciones Rápidas', style: ZTypography.titleLarge),
        const SizedBox(height: 16),
        ResponsiveGrid(
          mobileColumns: 2,
          tabletColumns: 3,
          desktopColumns: 5,
          children: [
            _OperationButton(label: 'Transferencia', icon: Icons.sync_alt, color: ZColors.brandPrimary, route: '/treasury/transfers'),
            _OperationButton(label: 'Depósito', icon: Icons.move_to_inbox, color: ZColors.success, route: '/treasury/deposits'),
            _OperationButton(label: 'Cheques', icon: Icons.payments, color: ZColors.warning, route: '/treasury/checks'),
            _OperationButton(label: 'Comisión', icon: Icons.percent, color: ZColors.brandAccent, route: '/treasury/commissions'),
            _OperationButton(label: 'Cobranza', icon: Icons.receipt_long, color: ZColors.moduleCrm, route: '/treasury/collections'),
          ],
        ),
      ],
    );
  }
}

class _OperationButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final Color color;
  final String route;

  const _OperationButton({required this.label, required this.icon, required this.color, required this.route});

  @override
  Widget build(BuildContext context) {
    return ZCard(
      child: InkWell(
        onTap: () => context.push(route),
        borderRadius: BorderRadius.circular(ZRadii.lg),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: color.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(ZRadii.md)),
                child: Icon(icon, color: color, size: 24),
              ),
              const SizedBox(height: 12),
              Text(label, style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold)),
            ],
          ),
        ),
      ),
    );
  }
}
