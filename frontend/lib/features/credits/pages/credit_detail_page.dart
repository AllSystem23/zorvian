import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';
import '../providers/credit_provider.dart';

final class CreditDetailPage extends ConsumerStatefulWidget {
  final String creditId;
  const CreditDetailPage({super.key, required this.creditId});
  @override
  ConsumerState<CreditDetailPage> createState() => _CreditDetailPageState();
}

final class _CreditDetailPageState extends ConsumerState<CreditDetailPage> with SingleTickerProviderStateMixin {
  CreditDetail? _data;
  bool _loading = true;
  String? _error;
  List<LateFee> _lateFees = [];
  List<CollectionAction> _collectionActions = [];
  List<CreditRefinancing> _refinancings = [];
  bool _lateFeesLoading = false;
  bool _actionsLoading = false;
  bool _refinancingsLoading = false;
  late TabController _tabCtrl;

  @override
  void initState() {
    super.initState();
    _tabCtrl = TabController(length: 5, vsync: this);
    _loadDetail();
    _loadLateFees();
    _loadActions();
    _loadRefinancings();
  }

  @override
  void dispose() {
    _tabCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadDetail() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits/${widget.creditId}');
      setState(() { _data = CreditDetail.fromJson(r.data as Map<String, dynamic>); _loading = false; });
    } catch (_) {
      setState(() { _error = 'Error al cargar crédito'; _loading = false; });
    }
  }

  Future<void> _loadLateFees() async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits/${widget.creditId}/late-fees');
      final data = r.data;
      final list = data is List ? data : [];
      setState(() { _lateFees = list.map((e) => LateFee.fromJson(e as Map<String, dynamic>)).toList(); });
    } catch (_) {}
  }

  Future<void> _loadActions() async {
    try {
      setState(() => _actionsLoading = true);
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits/${widget.creditId}/collection-actions');
      final data = r.data;
      final list = data is Map ? (data['items'] ?? []) : (data is List ? data : []);
      setState(() { _collectionActions = (list as List).map((e) => CollectionAction.fromJson(e as Map<String, dynamic>)).toList(); _actionsLoading = false; });
    } catch (_) {
      setState(() => _actionsLoading = false);
    }
  }

  Future<void> _loadRefinancings() async {
    try {
      setState(() => _refinancingsLoading = true);
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('credits/${widget.creditId}/refinancings');
      final data = r.data;
      final list = data is List ? data : [];
      setState(() { _refinancings = list.map((e) => CreditRefinancing.fromJson(e as Map<String, dynamic>)).toList(); _refinancingsLoading = false; });
    } catch (_) {
      setState(() => _refinancingsLoading = false);
    }
  }

  Future<void> _refinance() async {
    final result = await context.push<bool>('/credits/${widget.creditId}/refinancing');
    if (result == true) { await _loadDetail(); await _loadRefinancings(); }
  }

  Future<void> _calculateLateFees() async {
    setState(() => _lateFeesLoading = true);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('credits/${widget.creditId}/late-fees/calculate', data: {});
      await _loadLateFees();
      await _loadDetail();
    } catch (_) {}
    setState(() => _lateFeesLoading = false);
  }

  Future<void> _registerPayment() async {
    final amountCtrl = TextEditingController();
    final method = ValueNotifier('cash');
    final result = await ZModal.show<bool>(context,
      title: 'Registrar Pago',
      confirmText: 'Pagar',
      cancelText: 'Cancelar',
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          ZTextField(controller: amountCtrl, label: 'Monto', keyboardType: TextInputType.number, prefix: const Text('\$ ')),
          const SizedBox(height: 12),
          DropdownButtonFormField<String>(
            initialValue: method.value,
            items: const [
              DropdownMenuItem(value: 'cash', child: Text('Efectivo')),
              DropdownMenuItem(value: 'card', child: Text('Tarjeta')),
              DropdownMenuItem(value: 'transfer', child: Text('Transferencia')),
              DropdownMenuItem(value: 'check', child: Text('Cheque')),
            ],
            onChanged: (v) => method.value = v ?? 'cash',
            decoration: const InputDecoration(labelText: 'Método de pago'),
          ),
        ],
      ),
    );
    if (result != true || amountCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('credits/${widget.creditId}/payments', data: {
        'amount': double.parse(amountCtrl.text),
        'paymentMethod': method.value,
        'creditId': widget.creditId,
      });
      await _loadDetail();
      if (mounted) ZToast.success(context, 'Pago registrado');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  Future<void> _addCollectionAction() async {
    final typeCtrl = TextEditingController();
    final descCtrl = TextEditingController();
    final contactCtrl = TextEditingController();
    final phoneCtrl = TextEditingController();
    final promiseAmtCtrl = TextEditingController();
    final result = await ZModal.show<bool>(context,
      title: 'Nueva Acción de Cobranza',
      confirmText: 'Guardar',
      cancelText: 'Cancelar',
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            DropdownButtonFormField<String>(
              items: const [
                DropdownMenuItem(value: 'call', child: Text('Llamada')),
                DropdownMenuItem(value: 'visit', child: Text('Visita')),
                DropdownMenuItem(value: 'message', child: Text('Mensaje')),
                DropdownMenuItem(value: 'promise', child: Text('Acuerdo de pago')),
              ],
              onChanged: (v) => typeCtrl.text = v ?? '',
              decoration: const InputDecoration(labelText: 'Tipo'),
            ),
            const SizedBox(height: 8),
            ZTextField(controller: descCtrl, label: 'Descripción', maxLines: 2),
            ZTextField(controller: contactCtrl, label: 'Contacto'),
            ZTextField(controller: phoneCtrl, label: 'Teléfono', keyboardType: TextInputType.phone),
            ZTextField(controller: promiseAmtCtrl, label: 'Monto prometido', keyboardType: TextInputType.number),
          ],
        ),
      ),
    );
    if (result != true || typeCtrl.text.isEmpty) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('credits/${widget.creditId}/collection-actions', data: {
        'actionType': typeCtrl.text,
        'description': descCtrl.text,
        'contactPerson': contactCtrl.text,
        'contactPhone': phoneCtrl.text,
        'promiseAmount': promiseAmtCtrl.text.isNotEmpty ? promiseAmtCtrl.text : null,
      });
      await _loadActions();
      if (mounted) ZToast.success(context, 'Acción registrada');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error: $e');
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (_loading) return Scaffold(body: const Center(child: CircularProgressIndicator()));
    if (_error != null) return Scaffold(body: Center(child: Text(_error!)));

    final d = _data!;
    final stColor = switch (d.status) {
      'active' => Colors.green,
      'completed' => Colors.blue,
      'defaulted' => Colors.red,
      _ => Colors.orange,
    };

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                IconButton(icon: const Icon(Icons.payments, size: 20), onPressed: _registerPayment, tooltip: 'Registrar Pago'),
                IconButton(icon: const Icon(Icons.gavel, size: 20), onPressed: _addCollectionAction, tooltip: 'Acción de Cobranza'),
                IconButton(icon: const Icon(Icons.warning_amber, size: 20), onPressed: _calculateLateFees, tooltip: 'Calcular Mora'),
                IconButton(icon: const Icon(Icons.swap_horiz, size: 20), onPressed: _refinance, tooltip: 'Refinanciar'),
              ],
            ),
          ),
          ZCard(
            margin: const EdgeInsets.all(12),
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(children: [
                  Expanded(child: Text(d.clientName, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold))),
                  Chip(label: Text(d.status, style: const TextStyle(fontSize: 11, color: Colors.white)), backgroundColor: stColor, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap),
                ]),
                const SizedBox(height: 8),
                Row(children: [
                  Expanded(child: _row('Financiado', '\$${d.financedAmount.toStringAsFixed(0)}')),
                  Expanded(child: _row('Saldo', '\$${d.balance.toStringAsFixed(0)}', bold: true, color: d.balance > 0 ? Colors.red : Colors.green)),
                ]),
                Row(children: [
                  Expanded(child: _row('Pagado', '\$${d.paidAmount.toStringAsFixed(0)}')),
                  Expanded(child: _row('Interés', '\$${d.interestAmount.toStringAsFixed(0)}')),
                ]),
                Row(children: [
                  Expanded(child: _row('Cuotas', '${d.installmentCount} x \$${d.installmentAmount.toStringAsFixed(0)}')),
                  Expanded(child: _row('Tasa', '${d.interestRate.toStringAsFixed(1)}%')),
                ]),
                if (d.nextDueDate != null) _row('Próximo vencimiento', d.nextDueDate!),
              ],
            ),
          ),
          TabBar(
            controller: _tabCtrl,
            tabs: const [
              Tab(text: 'Cuotas'),
              Tab(text: 'Mora'),
              Tab(text: 'Cobranza'),
              Tab(text: 'Pagos'),
              Tab(text: 'Refinanciación'),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tabCtrl,
              children: [
                _installmentsTab(d.installments, theme),
                _lateFeesTab(theme),
                _collectionActionsTab(theme),
                _paymentsTab(theme),
                _refinancingsTab(theme),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _installmentsTab(List<CreditInstallment> installments, ThemeData theme) {
    return installments.isEmpty
        ? const Center(child: Text('No hay cuotas'))
        : RefreshIndicator(
            onRefresh: _loadDetail,
            child: ListView.builder(
              itemCount: installments.length,
              itemBuilder: (_, i) {
                final inst = installments[i];
                final stColor = inst.isOverdue ? Colors.red : (inst.isPaid ? Colors.green : Colors.orange);
                return ListTile(
                  leading: CircleAvatar(
                    backgroundColor: stColor.withAlpha(30),
                    child: Text('${inst.installmentNumber}', style: TextStyle(color: stColor, fontWeight: FontWeight.bold)),
                  ),
                  title: Text('Cuota #${inst.installmentNumber}', style: TextStyle(fontWeight: FontWeight.w600)),
                  subtitle: Text('Vence: ${inst.dueDate.length >= 10 ? inst.dueDate.substring(0, 10) : inst.dueDate} · Capital: \$${inst.principalAmount.toStringAsFixed(0)} · Int: \$${inst.interestAmount.toStringAsFixed(0)}'),
                  trailing: inst.isOverdue
                      ? Chip(label: Text('VENCIDA', style: const TextStyle(fontSize: 10, color: Colors.white)), backgroundColor: Colors.red, materialTapTargetSize: MaterialTapTargetSize.shrinkWrap, padding: EdgeInsets.zero)
                      : Chip(label: Text(inst.status, style: const TextStyle(fontSize: 10)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap, padding: EdgeInsets.zero),
                );
              },
            ),
          );
  }

  Widget _lateFeesTab(ThemeData theme) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              icon: _lateFeesLoading ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.refresh),
              label: const Text('Calcular Mora'),
              onPressed: _lateFeesLoading ? null : _calculateLateFees,
            ),
          ),
        ),
        Expanded(
          child: _lateFees.isEmpty
              ? const Center(child: Text('Sin cargos por mora'))
              : RefreshIndicator(
                  onRefresh: _loadLateFees,
                  child: ListView.builder(
                    itemCount: _lateFees.length,
                    itemBuilder: (_, i) {
                      final lf = _lateFees[i];
                      return ZCard(
                        margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                        child: ListTile(
                          title: Text('${lf.daysOverdue} días de atraso', style: TextStyle(fontWeight: FontWeight.w600)),
                          subtitle: Text('Multa: \$${lf.feeAmount.toStringAsFixed(0)} · Interés: \$${lf.interestAmount.toStringAsFixed(0)}'),
                          trailing: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text('\$${lf.totalAmount.toStringAsFixed(0)}', style: TextStyle(fontWeight: FontWeight.bold, color: lf.status == 'paid' ? Colors.green : Colors.red)),
                              Text(lf.status, style: const TextStyle(fontSize: 11)),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
        ),
      ],
    );
  }

  Widget _collectionActionsTab(ThemeData theme) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.all(12),
          child: SizedBox(
            width: double.infinity,
            child: ZButton(
              text: 'Nueva Acción',
              onPressed: _addCollectionAction,
              icon: Icons.add,
            ),
          ),
        ),
        Expanded(
          child: _collectionActions.isEmpty
              ? const Center(child: Text('Sin acciones registradas'))
              : _actionsLoading
                  ? const Center(child: CircularProgressIndicator())
                  : RefreshIndicator(
                      onRefresh: _loadActions,
                      child: ListView.builder(
                        itemCount: _collectionActions.length,
                        itemBuilder: (_, i) {
                          final a = _collectionActions[i];
                          final icon = switch (a.actionType) { 'call' => Icons.phone, 'visit' => Icons.person_pin, 'message' => Icons.message, 'promise' => Icons.handshake, _ => Icons.notifications };
                          return ZCard(
                            margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                            child: ListTile(
                              leading: CircleAvatar(child: Icon(icon, size: 18)),
                              title: Text('${a.actionType} · ${a.employeeName}', style: TextStyle(fontWeight: FontWeight.w600)),
                              subtitle: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  if (a.description != null && a.description!.isNotEmpty) Text(a.description!, maxLines: 2),
                                  if (a.followUpDate != null) Text('Seguimiento: ${a.followUpDate!.length >= 10 ? a.followUpDate!.substring(0, 10) : a.followUpDate!}', style: const TextStyle(fontSize: 11)),
                                ],
                              ),
                              trailing: a.result != null && a.result!.isNotEmpty ? Chip(label: Text(a.result!, style: const TextStyle(fontSize: 10)), materialTapTargetSize: MaterialTapTargetSize.shrinkWrap, padding: EdgeInsets.zero) : null,
                            ),
                          );
                        },
                      ),
                    ),
        ),
      ],
    );
  }

  Widget _paymentsTab(ThemeData theme) {
    return Padding(
      padding: const EdgeInsets.all(12),
      child: SizedBox(
        width: double.infinity,
        child: OutlinedButton.icon(
          icon: const Icon(Icons.payments),
          label: const Text('Registrar Pago'),
          onPressed: _registerPayment,
        ),
      ),
    );
  }

  Widget _refinancingsTab(ThemeData theme) {
    return _refinancingsLoading
        ? const Center(child: CircularProgressIndicator())
        : _refinancings.isEmpty
            ? const Center(child: Text('Sin refinanciamientos previos'))
            : RefreshIndicator(
                onRefresh: _loadRefinancings,
                child: ListView.builder(
                  itemCount: _refinancings.length,
                  itemBuilder: (_, i) {
                    final r = _refinancings[i];
                    return ZCard(
                      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                      padding: const EdgeInsets.all(12),
                      child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Refinanciamiento ${i + 1}', style: const TextStyle(fontWeight: FontWeight.bold)),
                            const Divider(),
                            _row('Saldo anterior', '\$${r.previousBalance.toStringAsFixed(0)}'),
                            _row('Tasa anterior', '${r.previousInterestRate.toStringAsFixed(1)}%'),
                            _row('Nuevo monto', '\$${r.newFinancedAmount.toStringAsFixed(0)}'),
                            _row('Nueva tasa', '${r.newInterestRate.toStringAsFixed(1)}%'),
                            _row('Nuevas cuotas', '${r.newInstallmentCount} x \$${r.newInstallmentAmount.toStringAsFixed(0)}'),
                            _row('Nuevo total', '\$${r.newTotalAmount.toStringAsFixed(0)}'),
                            if (r.reason.isNotEmpty) _row('Motivo', r.reason),
                          ],
                        ),
                      );
                  },
                ),
              );
  }

  Widget _row(String label, String value, {bool bold = false, Color? color}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.grey, fontSize: 13)),
          Text(value, style: TextStyle(fontWeight: bold ? FontWeight.bold : FontWeight.normal, color: color, fontSize: 13)),
        ],
      ),
    );
  }
}
