import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_currency_provider.dart';
import '../../../shared/ds/ds.dart';
import '../providers/provider_state.dart';
import '../data/provider_repository.dart';
import '../../../core/entities/provider_invoice.dart';

final class ProviderInvoicesPage extends ConsumerStatefulWidget {
  const ProviderInvoicesPage({super.key});
  @override
  ConsumerState<ProviderInvoicesPage> createState() =>
      _ProviderInvoicesPageState();
}

final class _ProviderInvoicesPageState
    extends ConsumerState<ProviderInvoicesPage> {
  @override
  Widget build(BuildContext context) {
    final invoicesAsync = ref.watch(allInvoicesProvider);
    final fmt = ref.watch(currencyFormatServiceProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Pagos y Facturas de Prestadores')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            invoicesAsync.when(
              data: (invoices) {
                final pendingCount = invoices
                    .where((i) => i.status == 'received')
                    .length;
                final totalPending = invoices
                    .where((i) => i.status == 'received')
                    .fold(0.0, (sum, item) => sum + item.netAmount);

                return Row(
                  children: [
                    Expanded(
                      child: ZStatCard(
                        title: 'Facturas Pendientes',
                        value: pendingCount.toString(),
                      ),
                    ),
                    const SizedBox(width: ZSpacing.md),
                    Expanded(
                      child: ZStatCard(
                        title: 'Total por Pagar',
                        value: fmt.currency(totalPending),
                      ),
                    ),
                  ],
                );
              },
              loading: () => const ZSkeleton(height: 100),
              error: (err, _) => const SizedBox.shrink(),
            ),
            const SizedBox(height: ZSpacing.xl),

            Text(
              'Registro de Facturas Recibidas',
              style: ZTypography.titleLarge,
            ),
            const SizedBox(height: ZSpacing.md),

            invoicesAsync.when(
              data: (invoices) => invoices.isEmpty
                  ? ZEmptyState(
                      icon: Icons.receipt_long,
                      title: 'Sin facturas',
                      subtitle: 'No se han registrado facturas de prestadores.',
                    )
                  : ZDataTable<ProviderInvoice>(
                      columns: const [
                        ZColumn(id: 'invoice', label: 'Factura #'),
                        ZColumn(id: 'amount', label: 'Monto', numeric: true),
                        ZColumn(id: 'status', label: 'Estado'),
                        ZColumn(id: 'due', label: 'Vence'),
                        ZColumn(id: 'actions', label: ''),
                      ],
                      rows: invoices,
                      rowMapper: (i) => DataRow(
                        cells: [
                          DataCell(
                            Text(
                              i.invoiceNumber,
                              style: ZTypography.labelMedium.copyWith(
                                fontWeight: FontWeight.bold,
                              ),
                            ),
                          ),
                          DataCell(Text('${i.invoiceAmount} ${i.currency}')),
                          DataCell(
                            ZBadge(
                              text: i.status,
                              type: i.status == 'paid'
                                  ? ZBadgeType.success
                                  : ZBadgeType.neutral,
                            ),
                          ),
                          DataCell(
                            Text(i.invoiceDate.toString().split(' ')[0]),
                          ),
                          DataCell(
                            IconButton(
                              icon: const Icon(Icons.payment),
                              onPressed: () async {
                                await ref
                                    .read(providerRepositoryProvider)
                                    .programPayment(
                                      i.id,
                                      DateTime.now(),
                                      'AUTO-PAY',
                                    );
                                ref.invalidate(allInvoicesProvider);
                              },
                            ),
                          ),
                        ],
                      ),
                    ),
              loading: () => const ZSkeleton(height: 300),
              error: (err, _) => ZAlertCard(
                message: 'Error al cargar facturas: $err',
                severity: 'high',
              ),
            ),

            const SizedBox(height: ZSpacing.xl),
            const ZAlertCard(
              message:
                  'Asegúrese de aplicar el 10% de IR a los prestadores de servicios profesionales antes de programar el pago.',
              severity: 'low',
            ),
          ],
        ),
      ),
    );
  }
}

final class RegisterInvoiceDialog extends ConsumerStatefulWidget {
  final VoidCallback onSaved;
  const RegisterInvoiceDialog({super.key, required this.onSaved});
  @override
  ConsumerState<RegisterInvoiceDialog> createState() =>
      _RegisterInvoiceDialogState();
}

final class _RegisterInvoiceDialogState
    extends ConsumerState<RegisterInvoiceDialog> {
  final _formKey = GlobalKey<FormState>();
  final _invoiceNumberCtrl = TextEditingController();
  final _invoiceAmountCtrl = TextEditingController();
  final _withholdingCtrl = TextEditingController(text: '0');
  final _currencyCtrl = TextEditingController(text: 'NIO');
  String? _selectedContractId;
  String? _selectedMilestoneId;
  DateTime _invoiceDate = DateTime.now();
  bool _saving = false;
  List<dynamic>? _milestones;

  @override
  void dispose() {
    _invoiceNumberCtrl.dispose();
    _invoiceAmountCtrl.dispose();
    _withholdingCtrl.dispose();
    _currencyCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadMilestones() async {
    if (_selectedContractId == null) return;
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get(
        'providers/contracts/$_selectedContractId/milestones',
      );
      setState(() => _milestones = r.data as List);
    } catch (_) {
      setState(() => _milestones = null);
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedMilestoneId == null) return;
    setState(() => _saving = true);
    try {
      final amount = double.parse(_invoiceAmountCtrl.text);
      final withholding = double.parse(_withholdingCtrl.text);
      final invoice = ProviderInvoice(
        id: '',
        paymentMilestoneId: _selectedMilestoneId!,
        invoiceNumber: _invoiceNumberCtrl.text.trim(),
        invoiceDate: _invoiceDate,
        invoiceAmount: amount,
        withholdingAmount: withholding,
        netAmount: amount - withholding,
        currency: _currencyCtrl.text.trim(),
      );
      await ref
          .read(providerRepositoryProvider)
          .registerInvoice(_selectedMilestoneId!, invoice);
      widget.onSaved();
      if (mounted) Navigator.of(context).pop();
    } catch (_) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Error al registrar factura')),
        );
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Registrar Factura'),
      content: SingleChildScrollView(
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              _ContractDropdown(
                value: _selectedContractId,
                onChanged: (v) {
                  setState(() {
                    _selectedContractId = v;
                    _selectedMilestoneId = null;
                    _milestones = null;
                  });
                  _loadMilestones();
                },
              ),
              const SizedBox(height: 12),
              if (_milestones != null)
                DropdownButtonFormField<String>(
                  initialValue: _selectedMilestoneId,
                  decoration: const InputDecoration(labelText: 'Hito'),
                  items: _milestones!
                      .map<DropdownMenuItem<String>>(
                        (m) => DropdownMenuItem<String>(
                          value: m['id'] as String?,
                          child: Text(
                            m['description'] as String? ?? m['id'] as String,
                          ),
                        ),
                      )
                      .toList(),
                  onChanged: (v) => setState(() => _selectedMilestoneId = v),
                  validator: (v) => v == null ? 'Seleccione un hito' : null,
                ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _invoiceNumberCtrl,
                decoration: const InputDecoration(
                  labelText: 'Número de Factura',
                ),
                validator: (v) => v == null || v.isEmpty ? 'Requerido' : null,
              ),
              const SizedBox(height: 12),
              Row(
                children: [
                  Expanded(
                    child: TextFormField(
                      controller: _invoiceAmountCtrl,
                      decoration: const InputDecoration(labelText: 'Monto'),
                      keyboardType: TextInputType.number,
                      validator: (v) =>
                          v == null || v.isEmpty ? 'Requerido' : null,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: TextFormField(
                      controller: _withholdingCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Retención IR',
                      ),
                      keyboardType: TextInputType.number,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: _currencyCtrl,
                decoration: const InputDecoration(labelText: 'Moneda'),
              ),
              const SizedBox(height: 12),
              InkWell(
                onTap: () async {
                  final date = await showDatePicker(
                    context: context,
                    initialDate: _invoiceDate,
                    firstDate: DateTime.now().subtract(
                      const Duration(days: 365),
                    ),
                    lastDate: DateTime.now(),
                  );
                  if (date != null) {
                    setState(() => _invoiceDate = date);
                  }
                },
                child: InputDecorator(
                  decoration: const InputDecoration(
                    labelText: 'Fecha de Factura',
                  ),
                  child: Text(
                    '${_invoiceDate.day}/${_invoiceDate.month}/${_invoiceDate.year}',
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancelar'),
        ),
        ZButton(text: 'Guardar', onPressed: _save, isLoading: _saving),
      ],
    );
  }
}

final class _ContractDropdown extends ConsumerWidget {
  final String? value;
  final ValueChanged<String?> onChanged;
  const _ContractDropdown({required this.value, required this.onChanged});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final contractsAsync = ref.watch(allContractsProvider);
    return contractsAsync.when(
      data: (contracts) => DropdownButtonFormField<String>(
        initialValue: value,
        decoration: const InputDecoration(labelText: 'Contrato'),
        items: contracts
            .map(
              (c) => DropdownMenuItem(value: c.id, child: Text(c.contractName)),
            )
            .toList(),
        onChanged: onChanged,
        validator: (v) => v == null ? 'Seleccione un contrato' : null,
      ),
      loading: () => const SizedBox(
        height: 56,
        child: Center(child: LinearProgressIndicator()),
      ),
      error: (_, _) => const Text('Error al cargar contratos'),
    );
  }
}
