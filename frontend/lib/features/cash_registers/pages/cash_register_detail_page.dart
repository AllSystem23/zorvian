import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import 'package:zorvian/core/widgets/responsive_layout.dart';
import '../../../auth/auth_provider.dart';
import '../providers/cash_register_provider.dart';

final class CashRegisterDetailPage extends ConsumerStatefulWidget {
  final String registerId;
  const CashRegisterDetailPage({super.key, required this.registerId});
  @override
  ConsumerState<CashRegisterDetailPage> createState() => _CashRegisterDetailPageState();
}

final class _CashRegisterDetailPageState extends ConsumerState<CashRegisterDetailPage> with SingleTickerProviderStateMixin {
  Map<String, dynamic>? _data;
  bool _loading = true;
  String? _error;
  List<CashMovement> _movements = [];
  bool _movementsLoading = false;
  Map<String, dynamic>? _arqueo;
  bool _arqueoLoading = false;
  late TabController _tabCtrl;

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 3, vsync: this);
    _load();
    _loadMovements();
    _loadArqueo();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}');
      setState(() { _data = r.data as Map<String, dynamic>; _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar registro'; _loading = false; });
    }
  }

  Future<void> _loadMovements() async {
    try {
      setState(() => _movementsLoading = true);
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}/movements');
      final data = r.data;
      final list = data is List ? data : [];
      setState(() { _movements = list.map((e) => CashMovement.fromJson(e as Map<String, dynamic>)).toList(); _movementsLoading = false; });
    } catch (_) {
      setState(() => _movementsLoading = false);
    }
  }

  Future<void> _closeRegister() async {
    final balanceCtrl = TextEditingController();
    final result = await ZModal.show<bool>(context,
      title: 'Cerrar Caja',
      confirmText: 'Cerrar',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text('Saldo esperado: \$${((_data?['expectedBalance'] as num?)?.toDouble() ?? 0).toStringAsFixed(0)}'),
          const SizedBox(height: 12),
          ZTextField(controller: balanceCtrl, label: 'Saldo Real', keyboardType: TextInputType.number, prefix: const Text('\$ ')),
        ],
      ),
    );
    if (result != true || balanceCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/${widget.registerId}/close', data: {
        'closingBalance': double.tryParse(balanceCtrl.text) ?? 0,
      });
      await _load();
      if (mounted) ZToast.success(context, 'Caja cerrada');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  Future<void> _loadArqueo() async {
    try {
      setState(() => _arqueoLoading = true);
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('cash-registers/${widget.registerId}/arqueo');
      setState(() { _arqueo = r.data as Map<String, dynamic>?; _arqueoLoading = false; });
    } catch (_) {
      setState(() { _arqueo = null; _arqueoLoading = false; });
    }
  }

  Future<void> _addMovement() async {
    final typeCtrl = TextEditingController();
    final amountCtrl = TextEditingController();
    final conceptCtrl = TextEditingController();
    final result = await ZModal.show<bool>(context,
      title: 'Agregar Movimiento',
      confirmText: 'Agregar',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          DropdownButtonFormField<String>(
            initialValue: 'income',
            items: const [
              DropdownMenuItem(value: 'income', child: Text('Ingreso')),
              DropdownMenuItem(value: 'expense', child: Text('Egreso')),
            ],
            onChanged: (v) => typeCtrl.text = v ?? 'income',
            decoration: const InputDecoration(labelText: 'Tipo', border: OutlineInputBorder()),
          ),
          const SizedBox(height: 12),
          ZTextField(controller: amountCtrl, label: 'Monto', keyboardType: TextInputType.number, prefix: const Text('\$ ')),
          const SizedBox(height: 12),
          ZTextField(controller: conceptCtrl, label: 'Concepto'),
        ],
      ),
    );
    if (result != true || amountCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('cash-registers/${widget.registerId}/movements', data: {
        'movementType': typeCtrl.text.isEmpty ? 'income' : typeCtrl.text,
        'amount': double.tryParse(amountCtrl.text) ?? 0,
        'concept': conceptCtrl.text,
      });
      await _loadMovements();
      await _load();
      if (mounted) ZToast.success(context, 'Movimiento registrado');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(appBar: AppBar(title: const Text('Caja')), body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(appBar: AppBar(title: const Text('Caja')), body: Center(child: Text(_error!)));

    final d = _data!;
    final isOpen = d['status'] == 'open';

    return Scaffold(
      backgroundColor: Theme.of(context).brightness == Brightness.dark ? ZColors.darkBackground : ZColors.neutral50,
      appBar: AppBar(
        title: Text('Caja: ${d['code'] ?? ''}'),
        actions: [
          if (isOpen) ...[
            IconButton(icon: const Icon(Icons.payments_outlined), onPressed: _addMovement, tooltip: 'Agregar Movimiento'),
            IconButton(icon: const Icon(Icons.calculate_outlined), onPressed: () async {
              final r = await context.push('/cash-registers/${widget.registerId}/arqueo');
              if (r == true) { await _load(); await _loadArqueo(); }
            }, tooltip: 'Arqueo de Caja'),
            IconButton(icon: const Icon(Icons.lock_outline), onPressed: _closeRegister, tooltip: 'Cerrar Caja'),
          ],
        ],
      ),
      body: Column(
        children: [
          // ── Header Card ──
          Container(
            color: Theme.of(context).brightness == Brightness.dark ? ZColors.darkSurface : Colors.white,
            padding: const EdgeInsets.all(24),
            child: Column(
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Text(d['code'] ?? '', style: ZTypography.headlineMedium.copyWith(fontWeight: FontWeight.bold)),
                              const SizedBox(width: 12),
                              ZBadge(
                                text: d['status']?.toString().toUpperCase() ?? '',
                                type: isOpen ? ZBadgeType.success : ZBadgeType.neutral,
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Text(
                            'Abierta el ${(d['openedAt'] as String?)?.substring(0, 16).replaceFirst('T', ' ') ?? ''}',
                            style: ZTypography.bodySmall.copyWith(color: ZColors.neutral500),
                          ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text('SALDO ESPERADO', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                        Text(
                          'C\$ ${(d['expectedBalance'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                          style: ZTypography.headlineSmall.copyWith(fontWeight: FontWeight.bold, color: ZColors.brandPrimary),
                        ),
                      ],
                    ),
                  ],
                ),
                const SizedBox(height: 24),
                ResponsiveGrid(
                  mobileColumns: 2,
                  tabletColumns: 4,
                  desktopColumns: 4,
                  children: [
                    _MiniStat(label: 'SALDO INICIAL', value: 'C\$ ${(d['openingBalance'] as num?)?.toStringAsFixed(2) ?? '0'}'),
                    _MiniStat(label: 'INGRESOS', value: 'C\$ ${(d['totalIncome'] as num?)?.toStringAsFixed(2) ?? '0'}', color: ZColors.success),
                    _MiniStat(label: 'EGRESOS', value: 'C\$ ${(d['totalExpense'] as num?)?.toStringAsFixed(2) ?? '0'}', color: ZColors.danger),
                    _MiniStat(
                      label: 'DIFERENCIA',
                      value: 'C\$ ${(d['difference'] as num?)?.toStringAsFixed(2) ?? '0'}',
                      color: ((d['difference'] as num?)?.abs() ?? 0) > 0.01 ? ZColors.danger : ZColors.success,
                    ),
                  ],
                ),
              ],
            ),
          ),
          
          TabBar(
            controller: _tabCtrl,
            labelStyle: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.bold),
            unselectedLabelStyle: ZTypography.labelMedium,
            indicatorColor: ZColors.brandAccent,
            tabs: const [
              Tab(text: 'MOVIMIENTOS'),
              Tab(text: 'RESUMEN DETALLADO'),
              Tab(text: 'ARQUEO'),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tabCtrl,
              children: [
                _movementsTab(theme),
                _summaryTab(d, theme),
                _arqueoTab(theme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _movementsTab(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Libro de Movimientos', style: ZTypography.titleLarge),
              if (_data?['status'] == 'open')
                ZButton(
                  text: 'Nuevo Movimiento',
                  onPressed: _addMovement,
                  icon: Icons.add,
                  fullWidth: false,
                ),
            ],
          ),
          const SizedBox(height: 16),
          Expanded(
            child: _movementsLoading
                ? const Center(child: CircularProgressIndicator())
                : ZDataTable<CashMovement>(
                    columns: const [
                      ZColumn(id: 'type', label: 'Tipo'),
                      ZColumn(id: 'concept', label: 'Concepto'),
                      ZColumn(id: 'date', label: 'Hora'),
                      ZColumn(id: 'amount', label: 'Monto', numeric: true),
                    ],
                    rows: _movements,
                    emptyMessage: 'No se han registrado movimientos en esta caja.',
                    rowMapper: (m) {
                      final isIncome = m.movementType == 'income';
                      return DataRow(cells: [
                        DataCell(Icon(
                          isIncome ? Icons.arrow_upward : Icons.arrow_downward,
                          color: isIncome ? ZColors.success : ZColors.danger,
                          size: 18,
                        )),
                        DataCell(Text(m.concept ?? 'Sin concepto', style: const TextStyle(fontWeight: FontWeight.w600))),
                        DataCell(Text(m.createdAt.length >= 16 ? m.createdAt.substring(11, 16) : m.createdAt)),
                        DataCell(Text(
                          'C\$ ${m.amount.toStringAsFixed(2)}',
                          style: TextStyle(
                            fontWeight: FontWeight.bold,
                            color: isIncome ? ZColors.success : ZColors.danger,
                          ),
                        )),
                      ]);
                    },
                  ),
          ),
        ],
      ),
    );
  }

  Widget _summaryTab(Map<String, dynamic> d, ThemeData theme) {
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        ZCard(
          padding: const EdgeInsets.all(24),
          child: Column(
            children: [
              _row('Código de Terminal', d['code'] ?? '', bold: true),
              _row('Estado Actual', d['status']?.toString().toUpperCase() ?? ''),
              _row('Cajero Responsable', d['employeeName'] ?? 'No asignado'),
              const Divider(height: 32),
              _row('Saldo de Apertura', 'C\$ ${(d['openingBalance'] as num?)?.toStringAsFixed(2) ?? '0'}'),
              _row('Ventas e Ingresos (+)', 'C\$ ${(d['totalIncome'] as num?)?.toStringAsFixed(2) ?? '0'}', color: ZColors.success),
              _row('Gastos y Retiros (-)', 'C\$ ${(d['totalExpense'] as num?)?.toStringAsFixed(2) ?? '0'}', color: ZColors.danger),
              const Divider(height: 32),
              _row('Saldo Teórico (Esperado)', 'C\$ ${(d['expectedBalance'] as num?)?.toStringAsFixed(2) ?? '0'}', bold: true),
              _row('Saldo Declarado (Cierre)', 'C\$ ${(d['closingBalance'] as num?)?.toStringAsFixed(2) ?? '0'}'),
              _row('Diferencia de Caja', 'C\$ ${(d['difference'] as num?)?.toStringAsFixed(2) ?? '0'}',
                bold: true, color: ((d['difference'] as num?)?.abs() ?? 0) > 0.01 ? ZColors.danger : ZColors.success),
              const Divider(height: 32),
              _row('Fecha Apertura', (d['openedAt'] as String?)?.replaceFirst('T', ' ').substring(0, 16) ?? ''),
              if (d['closedAt'] != null) _row('Fecha Cierre', (d['closedAt'] as String).replaceFirst('T', ' ').substring(0, 16)),
              if (d['notes'] != null) ...[
                const SizedBox(height: 12),
                Text('Notas:', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                Text(d['notes'] as String, style: ZTypography.bodyMedium),
              ],
            ],
          ),
        ),
      ],
    );
  }

  Widget _arqueoTab(ThemeData theme) {
    if (_arqueoLoading) return const Center(child: CircularProgressIndicator());
    if (_arqueo == null) {
      return Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.calculate_outlined, size: 64, color: ZColors.neutral200),
            const SizedBox(height: 16),
            Text('No se ha realizado el arqueo físico', style: ZTypography.titleMedium.copyWith(color: ZColors.neutral400)),
            const SizedBox(height: 24),
            if (_data?['status'] == 'open')
              ZButton(
                text: 'Iniciar Arqueo de Caja',
                icon: Icons.play_arrow_outlined,
                fullWidth: false,
                onPressed: () async {
                  final r = await context.push('/cash-registers/${widget.registerId}/arqueo');
                  if (r == true) { await _load(); await _loadArqueo(); }
                },
              ),
          ],
        ),
      );
    }
    final a = _arqueo!;
    final denoms = (a['denominations'] as List?) ?? [];
    return ListView(
      padding: const EdgeInsets.all(24),
      children: [
        ZCard(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Resultado del Arqueo', style: ZTypography.titleLarge),
                    ZBadge(text: 'COMPLETADO', type: ZBadgeType.success),
                  ],
                ),
                const SizedBox(height: 24),
                _row('Saldo del Sistema', 'C\$ ${(a['expectedBalance'] as num?)?.toStringAsFixed(2) ?? '0.00'}'),
                _row('Total Contado (Físico)', 'C\$ ${(a['countedTotal'] as num?)?.toStringAsFixed(2) ?? '0.00'}', bold: true),
                _row('Diferencia Detectada', 'C\$ ${(a['difference'] as num?)?.toStringAsFixed(2) ?? '0.00'}',
                  color: ((a['difference'] as num?)?.abs() ?? 0) > 0.01 ? ZColors.danger : ZColors.success, bold: true),
                const Divider(height: 32),
                if (a['employeeName'] != null) _row('Realizado por', a['employeeName'] as String),
                if (denoms.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text('DESGLOSE DE DENOMINACIONES', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                  const SizedBox(height: 12),
                  ...denoms.map((d) => Padding(
                    padding: const EdgeInsets.only(bottom: 8.0),
                    child: Row(
                      children: [
                        Icon(d['denominationType'] == 'bill' ? Icons.money : Icons.copyright, size: 16, color: ZColors.neutral400),
                        const SizedBox(width: 12),
                        Text(
                          d['denominationType'] == 'bill' ? 'Billete C\$ ${(d['denominationValue'] as num).toStringAsFixed(2)}' : 'Moneda C\$ ${(d['denominationValue'] as num).toStringAsFixed(2)}',
                          style: ZTypography.bodyMedium,
                        ),
                        const Spacer(),
                        Text('x${d['quantity']}', style: ZTypography.monoMedium),
                        const SizedBox(width: 16),
                        Text('C\$ ${(d['total'] as num).toStringAsFixed(2)}', style: ZTypography.titleSmall),
                      ],
                    ),
                  )),
                ],
              ],
            ),
          ),
        ],
    );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral500)),
          Text(value, style: ZTypography.bodyLarge.copyWith(
            fontWeight: bold ? FontWeight.bold : FontWeight.normal,
            color: color,
          )),
        ],
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  final String label;
  final String value;
  final Color? color;

  const _MiniStat({required this.label, required this.value, this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, fontSize: 9)),
        const SizedBox(height: 4),
        Text(value, style: ZTypography.titleMedium.copyWith(
          fontWeight: FontWeight.bold,
          color: color,
        )),
      ],
    );
  }
}
