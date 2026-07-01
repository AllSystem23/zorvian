import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import '../../../shared/ds/ds.dart';
import '../providers/public_warranty_provider.dart';
import '../models/warranty_model.dart';
import '../utils/warranty_utils.dart';

class WarrantyTrackingPage extends ConsumerStatefulWidget {
  const WarrantyTrackingPage({super.key});

  @override
  ConsumerState<WarrantyTrackingPage> createState() => _WarrantyTrackingPageState();
}

class _WarrantyTrackingPageState extends ConsumerState<WarrantyTrackingPage> {
  final _formKey = GlobalKey<FormState>();
  final _warrantyCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();

  @override
  void dispose() {
    _warrantyCtrl.dispose();
    _phoneCtrl.dispose();
    super.dispose();
  }

  void _track() {
    if (_formKey.currentState?.validate() ?? false) {
      ref.read(publicWarrantyProvider.notifier).track(
        _warrantyCtrl.text.trim(),
        _phoneCtrl.text.trim(),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final tracking = ref.watch(publicWarrantyProvider);
    final warranty = tracking.warranty;
    final isWide = MediaQuery.of(context).size.width > 600;

    return Scaffold(
      backgroundColor: ZColors.neutral50,
      appBar: AppBar(
        backgroundColor: ZColors.brandPrimary,
        foregroundColor: Colors.white,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.go('/'),
        ),
        title: const Text('Consulta de Garantía'),
      ),
      body: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 700),
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const SizedBox(height: 16),
                const Icon(Icons.verified_user_outlined, size: 64, color: ZColors.brandPrimary),
                const SizedBox(height: 12),
                Text(
                  'Verifique el estado de su garantía',
                  style: ZTypography.headlineSmall,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 8),
                Text(
                  'Ingrese el número de garantía y su número de teléfono registrado.',
                  style: ZTypography.bodyMedium.copyWith(color: ZColors.neutral600),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 32),
                Form(
                  key: _formKey,
                  child: Column(
                    children: [
                      ZTextField(
                        controller: _warrantyCtrl,
                        label: 'Número de garantía',
                        prefix: const Icon(Icons.confirmation_number_outlined),
                        validator: (v) => (v == null || v.trim().isEmpty) ? 'Ingrese el número de garantía' : null,
                      ),
                      const SizedBox(height: 16),
                      ZTextField(
                        controller: _phoneCtrl,
                        label: 'Teléfono registrado',
                        prefix: const Icon(Icons.phone_outlined),
                        keyboardType: TextInputType.phone,
                        validator: (v) => (v == null || v.trim().isEmpty) ? 'Ingrese su número de teléfono' : null,
                      ),
                      const SizedBox(height: 24),
                      SizedBox(
                        width: double.infinity,
                        child: ZButton(
                          text: 'Consultar',
                          icon: Icons.search,
                          type: ZButtonType.primary,
                          onPressed: _track,
                        ),
                      ),
                    ],
                  ),
                ),
                if (tracking.isLoading) ...[
                  const SizedBox(height: 48),
                  const Center(child: CircularProgressIndicator(color: ZColors.brandPrimary)),
                ],
                if (tracking.error != null && !tracking.isLoading) ...[
                  const SizedBox(height: 32),
                  ZAlertCard(
                    message: tracking.error!,
                    severity: 'high',
                  ),
                ],
                if (warranty != null) ...[
                  const SizedBox(height: 32),
                  _buildResult(context, warranty, isWide),
                ],
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildResult(BuildContext ctx, WarrantyTrackingModel w, bool isWide) {
    final dateFmt = DateFormat('dd/MM/yyyy');
    final color = warrantyStatusColor(w.status);
    final label = warrantyStatusLabel(w.status);

    return ZCard(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.verified, color: color, size: 28),
              const SizedBox(width: 12),
              Expanded(
                child: Text(
                  'Garantía ${w.warrantyNumber}',
                  style: ZTypography.titleLarge,
                ),
              ),
              ZBadge(text: label, type: warrantyBadgeType(w.status)),
            ],
          ),
          const SizedBox(height: 20),
          const Divider(),
          const SizedBox(height: 12),
          if (isWide)
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(child: _buildInfoSection(w, dateFmt)),
                const SizedBox(width: 24),
                Expanded(child: _buildDatesSection(w, dateFmt)),
              ],
            )
          else ...[
            _buildInfoSection(w, dateFmt),
            const SizedBox(height: 16),
            _buildDatesSection(w, dateFmt),
          ],
          if (w.termsAndConditions != null && w.termsAndConditions!.isNotEmpty) ...[
            const SizedBox(height: 20),
            const Divider(),
            const SizedBox(height: 12),
            Text('Términos y condiciones', style: ZTypography.titleSmall),
            const SizedBox(height: 8),
            Text(w.termsAndConditions!, style: ZTypography.bodyMedium),
          ],
          if (w.claims.isNotEmpty) ...[
            const SizedBox(height: 20),
            const Divider(),
            const SizedBox(height: 12),
            Text('Reclamos (${w.claims.length})', style: ZTypography.titleSmall),
            const SizedBox(height: 8),
            ...w.claims.map((c) => Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: ZCard(
                padding: const EdgeInsets.all(12),
                child: Row(
                  children: [
                    ZBadge(
                      text: warrantyStatusLabel(c.status),
                      type: warrantyBadgeType(c.status),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(c.claimNumber, style: ZTypography.bodySmall),
                          if (c.description != null)
                            Text(c.description!, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral600)),
                        ],
                      ),
                    ),
                    if (c.createdAt != null)
                      Text(dateFmt.format(c.createdAt!), style: ZTypography.bodySmall),
                  ],
                ),
              ),
            )),
          ],
          if (w.timeline.isNotEmpty) ...[
            const SizedBox(height: 20),
            const Divider(),
            const SizedBox(height: 12),
            Text('Historial', style: ZTypography.titleSmall),
            const SizedBox(height: 8),
            ...w.timeline.map((t) => Padding(
              padding: const EdgeInsets.only(bottom: ZSpacing.sm),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    width: 8,
                    height: 8,
                    margin: const EdgeInsets.only(top: 6),
                    decoration: const BoxDecoration(color: ZColors.brandPrimary, shape: BoxShape.circle),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(t.eventType, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                        if (t.description != null)
                          Text(t.description!, style: ZTypography.bodySmall.copyWith(color: ZColors.neutral600)),
                        if (t.eventDate != null)
                          Text(dateFmt.format(t.eventDate!), style: ZTypography.bodySmall),
                      ],
                    ),
                  ),
                ],
              ),
            )),
          ],
        ],
      ),
    );
  }

  Widget _buildInfoSection(WarrantyTrackingModel w, DateFormat dateFmt) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _infoRow(Icons.person_outline, 'Cliente', w.clientName),
        _infoRow(Icons.inventory_2_outlined, 'Producto', w.productName),
        if (w.serialNumber?.isNotEmpty == true)
          _infoRow(Icons.qr_code, 'Serie', w.serialNumber!),
        if (w.imei?.isNotEmpty == true)
          _infoRow(Icons.phone_android, 'IMEI', w.imei!),
        if (w.lot?.isNotEmpty == true)
          _infoRow(Icons.category_outlined, 'Lote', w.lot!),
      ],
    );
  }

  Widget _buildDatesSection(WarrantyTrackingModel w, DateFormat dateFmt) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _infoRow(Icons.calendar_today, 'Inicio', w.startDate != null ? dateFmt.format(w.startDate!) : '—'),
        _infoRow(Icons.event_busy, 'Vencimiento', w.endDate != null ? dateFmt.format(w.endDate!) : '—'),
        if (w.durationDays != null)
          _infoRow(Icons.timelapse, 'Duración', '${w.durationDays} días'),
      ],
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(icon, size: 18, color: ZColors.neutral500),
          const SizedBox(width: 8),
          Text('$label: ', style: ZTypography.bodySmall.copyWith(fontWeight: FontWeight.w600)),
          Expanded(child: Text(value, style: ZTypography.bodySmall)),
        ],
      ),
    );
  }
}
