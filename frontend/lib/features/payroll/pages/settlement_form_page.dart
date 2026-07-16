import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/printing/platform/download_helper.dart';
import '../../../shared/ds/ds.dart';
import '../providers/settlement_provider.dart';
import '../services/settlement_service.dart';

class SettlementFormPage extends ConsumerStatefulWidget {
  final String employeeId;
  final String companyId;

  const SettlementFormPage({super.key, required this.employeeId, required this.companyId});

  @override
  ConsumerState<SettlementFormPage> createState() => _SettlementFormPageState();
}

class _SettlementFormPageState extends ConsumerState<SettlementFormPage> {
  String _terminationReason = 'VoluntaryResignation';
  DateTime _terminationDate = DateTime.now();
  DateTime? _paidThroughDate;
  final _overtimeHoursController = TextEditingController(text: '0');
  final _overtimePayController = TextEditingController(text: '0');
  bool _isGeneratingPdf = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(employeeInfoProvider(widget.employeeId));
    });
  }

  @override
  void dispose() {
    _overtimeHoursController.dispose();
    _overtimePayController.dispose();
    super.dispose();
  }

  Future<void> _calculate() async {
    await ref.read(settlementProvider.notifier).calculate(
          employeeId: widget.employeeId,
          reason: _terminationReason,
          terminationDate: _terminationDate,
          paidThroughDate: _paidThroughDate,
          overtimeHours: double.tryParse(_overtimeHoursController.text) ?? 0,
          overtimePay: double.tryParse(_overtimePayController.text) ?? 0,
        );
  }

  Future<void> _downloadPdf() async {
    setState(() => _isGeneratingPdf = true);
    try {
      final result = ref.read(settlementProvider).value;
      if (result == null) return;

      final pdfBytes = await ref.read(settlementServiceProvider).generateSettlementPdf(
            employeeId: widget.employeeId,
            companyId: widget.companyId,
            terminationType: _terminationReason,
            lastDay: _terminationDate,
            baseSalary: result.monthlySalary,
            accruedVacations: result.vacationPay,
            accruedAguinaldo: result.aguinaldoPay,
            indemnization: result.severancePay,
          );

      downloadBytes(pdfBytes, 'Liquidacion_${widget.employeeId}.pdf');

      if (mounted) ZToast.success(context, 'PDF descargado exitosamente');
    } catch (e) {
      if (mounted) ZToast.error(context, 'Error al generar PDF: $e');
    } finally {
      if (mounted) setState(() => _isGeneratingPdf = false);
    }
  }

  // ── Currency helper ──
  String _fmt(SettlementResult r, double amount) =>
      '${r.currencySymbol} ${NumberFormat('#,##0.00').format(amount)}';

  String _rate(double value) => '${(value * 100).toStringAsFixed(1)}%';

  String _legalRef(SettlementResult r, String concept) {
    // Country-specific legal references
    switch (r.countryCode) {
      case 'NIC':
        return _nicLegalRefs[concept] ?? '';
      case 'HND':
        return _hndLegalRefs[concept] ?? '';
      default:
        return '';
    }
  }

  static const _nicLegalRefs = {
    'aguinaldo': 'Art. 93 CT',
    'vacaciones': 'Art. 78 CT',
    'indemnizacion': 'Art. 45 CT',
    'confianza': 'Art. 46 CT',
    'domestic15x': 'Art. 145 CT',
    'ir_salary': 'Art. 23 Ley 822',
    'ir_occasional': 'Art. 19.2 Reglamento Ley 822',
    'ir_trust': 'Art. 19.3 L822',
  };

  static const _hndLegalRefs = {
    'aguinaldo': 'Art. 135 Código del Trabajo',
    'vacaciones': 'Art. 133 Código del Trabajo',
    'indemnizacion': 'Art. 113 Código del Trabajo',
    'confianza': 'Art. 115 Código del Trabajo',
    'ir_salary': 'Art. 12 Reforma Tributaria',
  };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final employeeAsync = ref.watch(employeeInfoProvider(widget.employeeId));
    final settlementAsync = ref.watch(settlementProvider);
    final result = settlementAsync.value;

    return Scaffold(
      backgroundColor: isDark ? ZColors.darkBackground : ZColors.background,
      body: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(
            child: employeeAsync.when(
              data: (emp) => emp != null ? _buildEmployeeHeader(emp, isDark) : const SizedBox.shrink(),
              loading: () => const SizedBox(height: 80, child: Center(child: CircularProgressIndicator())),
              error: (_, _) => const SizedBox.shrink(),
            ),
          ),

          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(ZSpacing.xl, ZSpacing.lg, ZSpacing.xl, ZSpacing.huge),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  _buildConfigurationCard(isDark, result),
                  const SizedBox(height: ZSpacing.xl),
                  ZButton(
                    text: 'Calcular Liquidación',
                    icon: Icons.calculate,
                    gradient: ZColors.accentGradient,
                    isLoading: settlementAsync is AsyncLoading,
                    onPressed: _calculate,
                  ),
                  if (result != null) ...[
                    const SizedBox(height: ZSpacing.md),
                    ZButton(
                      text: 'Descargar PDF',
                      icon: Icons.picture_as_pdf,
                      fullWidth: false,
                      isLoading: _isGeneratingPdf,
                      onPressed: _downloadPdf,
                    ),
                  ],
                  const SizedBox(height: ZSpacing.xl),
                  if (result != null) ...[
                    _buildSummaryStats(result, isDark),
                    const SizedBox(height: ZSpacing.xl),
                    _buildIngresosSection(result, isDark),
                    const SizedBox(height: ZSpacing.xl),
                    _buildDeduccionesSection(result, isDark),
                    const SizedBox(height: ZSpacing.xl),
                    _buildCostosPatronalesSection(result, isDark),
                    const SizedBox(height: ZSpacing.xl),
                    _buildNetoSection(result, isDark),
                  ],
                  if (settlementAsync is AsyncError)
                    _buildErrorCard(settlementAsync.error, isDark),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  EMPLOYEE HEADER
  // ══════════════════════════════════════════════════════════════
  Widget _buildEmployeeHeader(EmployeeInfo emp, bool isDark) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(ZSpacing.xl, ZSpacing.lg, ZSpacing.xl, ZSpacing.xl),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: isDark
              ? [ZColors.darkSurface, ZColors.darkBackground]
              : [ZColors.brandPrimary.withAlpha(10), ZColors.background],
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
        ),
      ),
      child: Row(
        children: [
          CircleAvatar(
            radius: 28,
            backgroundColor: ZColors.moduleHr.withAlpha(30),
            child: Text(
              emp.name.isNotEmpty ? emp.name[0].toUpperCase() : '?',
              style: ZTypography.headlineMedium.copyWith(color: ZColors.moduleHr),
            ),
          ),
          const SizedBox(width: ZSpacing.lg),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(emp.name, style: ZTypography.headlineSmall),
                const SizedBox(height: ZSpacing.xxs),
                Text(
                  emp.position ?? 'Trabajador',
                  style: ZTypography.bodyMedium.copyWith(
                    color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                  ),
                ),
                if (emp.hireDate != null) ...[
                  const SizedBox(height: ZSpacing.xxs),
                  Text(
                    'Ingreso: ${ZFormatters.date(emp.hireDate!)}',
                    style: ZTypography.bodySmall.copyWith(
                      color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                    ),
                  ),
                ],
                if (emp.countryCode != null) ...[
                  const SizedBox(height: ZSpacing.xxs),
                  ZBadge(
                    text: emp.countryCode!,
                    type: ZBadgeType.accent,
                  ),
                ],
              ],
            ),
          ),
          if (emp.salary != null)
            Container(
              padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
              decoration: BoxDecoration(
                color: ZColors.brandTeal.withAlpha(20),
                borderRadius: BorderRadius.circular(ZRadii.md),
                border: Border.all(color: ZColors.brandTeal.withAlpha(40)),
              ),
              child: Column(
                children: [
                  Text('SALARIO MENSUAL', style: ZTypography.labelSmall.copyWith(color: ZColors.brandTeal)),
                  Text(
                    '${emp.countryCode == 'HND' ? 'L' : 'C\$'} ${NumberFormat('#,##0.00').format(emp.salary!)}',
                    style: ZTypography.titleLarge.copyWith(color: ZColors.brandTeal),
                  ),
                ],
              ),
            ),
        ],
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  CONFIGURATION CARD
  // ══════════════════════════════════════════════════════════════
  Widget _buildConfigurationCard(bool isDark, SettlementResult? r) {
    final curr = r?.currencySymbol ?? 'C\$';
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                width: 36, height: 36,
                decoration: BoxDecoration(
                  color: ZColors.brandAccent.withAlpha(20),
                  borderRadius: BorderRadius.circular(ZRadii.sm),
                ),
                child: const Icon(Icons.tune, size: 20, color: ZColors.brandAccent),
              ),
              const SizedBox(width: ZSpacing.md),
              Text('Configuración de Liquidación', style: ZTypography.titleLarge),
            ],
          ),
          const SizedBox(height: ZSpacing.xl),
          LayoutBuilder(
            builder: (context, constraints) {
              final isWide = constraints.maxWidth > 600;
              return isWide
                  ? Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(child: _buildReasonDropdown(isDark)),
                        const SizedBox(width: ZSpacing.lg),
                        Expanded(child: _buildTerminationDatePicker(isDark)),
                        const SizedBox(width: ZSpacing.lg),
                        Expanded(child: _buildPaidThroughDatePicker(isDark)),
                      ],
                    )
                  : Column(
                      children: [
                        _buildReasonDropdown(isDark),
                        const SizedBox(height: ZSpacing.lg),
                        _buildTerminationDatePicker(isDark),
                        const SizedBox(height: ZSpacing.lg),
                        _buildPaidThroughDatePicker(isDark),
                      ],
                    );
            },
          ),
          const SizedBox(height: ZSpacing.xl),
          Container(
            padding: const EdgeInsets.all(ZSpacing.lg),
            decoration: BoxDecoration(
              color: isDark ? ZColors.neutral800.withAlpha(50) : ZColors.neutral50,
              borderRadius: BorderRadius.circular(ZRadii.md),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    const Icon(Icons.access_time, size: 16, color: ZColors.warning),
                    const SizedBox(width: ZSpacing.sm),
                    Text('Horas Extras (Opcional)', style: ZTypography.titleSmall),
                  ],
                ),
                const SizedBox(height: ZSpacing.md),
                Row(
                  children: [
                    Expanded(
                      child: ZTextField(
                        controller: _overtimeHoursController,
                        label: 'Horas',
                        keyboardType: TextInputType.number,
                      ),
                    ),
                    const SizedBox(width: ZSpacing.lg),
                    Expanded(
                      child: ZTextField(
                        controller: _overtimePayController,
                        label: 'Monto ($curr)',
                        keyboardType: TextInputType.number,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildReasonDropdown(bool isDark) {
    return ZDropdownFormField<String>(
      value: _terminationReason,
      label: 'Motivo de Terminación',
      prefixIcon: Icons.gavel,
      items: const [
        DropdownMenuItem(value: 'VoluntaryResignation', child: Text('Renuncia Voluntaria')),
        DropdownMenuItem(value: 'JustifiedDismissal', child: Text('Despido con Causa Justificada')),
        DropdownMenuItem(value: 'UnjustifiedDismissal', child: Text('Despido Injustificado')),
        DropdownMenuItem(value: 'MutualAgreement', child: Text('Mutuo Acuerdo')),
      ],
      onChanged: (val) => setState(() => _terminationReason = val!),
    );
  }

  Widget _buildTerminationDatePicker(bool isDark) {
    return _buildDatePickerField(
      label: 'FECHA DE SALIDA',
      value: ZFormatters.date(_terminationDate),
      icon: Icons.event,
      isDark: isDark,
      onTap: () async {
        final date = await showDatePicker(
          context: context,
          initialDate: _terminationDate,
          firstDate: DateTime(2020),
          lastDate: DateTime.now().add(const Duration(days: 365)),
        );
        if (date != null) setState(() => _terminationDate = date);
      },
    );
  }

  Widget _buildPaidThroughDatePicker(bool isDark) {
    return _buildDatePickerField(
      label: 'PAGADO HASTA (OPCIONAL)',
      value: _paidThroughDate != null ? ZFormatters.date(_paidThroughDate!) : 'Al día',
      icon: Icons.event_available,
      isDark: isDark,
      onTap: () async {
        final date = await showDatePicker(
          context: context,
          initialDate: _paidThroughDate ?? _terminationDate,
          firstDate: DateTime(2020),
          lastDate: DateTime.now().add(const Duration(days: 365)),
        );
        setState(() => _paidThroughDate = date);
      },
      onClear: _paidThroughDate != null ? () => setState(() => _paidThroughDate = null) : null,
    );
  }

  Widget _buildDatePickerField({
    required String label,
    required String value,
    required IconData icon,
    required bool isDark,
    required VoidCallback onTap,
    VoidCallback? onClear,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: ZSpacing.xs, left: 4),
          child: Text(label, style: ZTypography.labelSmall.copyWith(
            color: isDark ? ZColors.neutral400 : ZColors.neutral500,
            letterSpacing: 1.2,
          )),
        ),
        InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(ZRadii.md),
          child: InputDecorator(
            decoration: InputDecoration(
              prefixIcon: Icon(icon, size: 18, color: ZColors.brandAccent),
              suffixIcon: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (onClear != null)
                    IconButton(
                      icon: const Icon(Icons.close, size: 16),
                      padding: EdgeInsets.zero,
                      constraints: const BoxConstraints(minWidth: 24, minHeight: 24),
                      onPressed: onClear,
                    ),
                  const Icon(Icons.calendar_today, size: 16, color: ZColors.neutral400),
                  const SizedBox(width: ZSpacing.sm),
                ],
              ),
              filled: true,
              fillColor: isDark ? ZColors.darkSurface : ZColors.surface,
            ),
            child: Text(value, style: ZTypography.bodyMedium.copyWith(
              color: isDark ? Colors.white : ZColors.neutral900,
            )),
          ),
        ),
      ],
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  SUMMARY STATS (KPIs)
  // ══════════════════════════════════════════════════════════════
  Widget _buildSummaryStats(SettlementResult r, bool isDark) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            const Icon(Icons.analytics_outlined, size: 20, color: ZColors.brandAccent),
            const SizedBox(width: ZSpacing.sm),
            Text('Resumen de Liquidación', style: ZTypography.titleLarge),
            const SizedBox(width: ZSpacing.sm),
            ZBadge(text: r.countryName, type: ZBadgeType.accent),
          ],
        ),
        const SizedBox(height: ZSpacing.lg),
        LayoutBuilder(
          builder: (context, constraints) {
            final isWide = constraints.maxWidth > 600;
            return isWide
                ? Row(
                    children: [
                      Expanded(child: _buildNetStat(r, isDark)),
                      const SizedBox(width: ZSpacing.md),
                      Expanded(child: _buildGrossStat(r, isDark)),
                      const SizedBox(width: ZSpacing.md),
                      Expanded(child: _buildDeductionsStat(r, isDark)),
                      const SizedBox(width: ZSpacing.md),
                      Expanded(child: _buildDaysStat(r, isDark)),
                    ],
                  )
                : Column(
                    children: [
                      Row(children: [
                        Expanded(child: _buildNetStat(r, isDark)),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: _buildGrossStat(r, isDark)),
                      ]),
                      const SizedBox(height: ZSpacing.md),
                      Row(children: [
                        Expanded(child: _buildDeductionsStat(r, isDark)),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: _buildDaysStat(r, isDark)),
                      ]),
                    ],
                  );
          },
        ),
      ],
    );
  }

  Widget _buildNetStat(SettlementResult r, bool isDark) => ZStatCard(
    title: 'NETO A RECIBIR',
    value: _fmt(r, r.netSettlement),
    label: 'Monto final',
    icon: Icons.account_balance_wallet,
    variant: ZStatVariant.success,
  );

  Widget _buildGrossStat(SettlementResult r, bool isDark) => ZStatCard(
    title: 'TOTAL INGRESOS',
    value: _fmt(r, r.grossSettlement),
    label: 'Bruto',
    icon: Icons.trending_up,
    variant: ZStatVariant.primary,
  );

  Widget _buildDeductionsStat(SettlementResult r, bool isDark) => ZStatCard(
    title: 'DEDUCCIONES',
    value: _fmt(r, r.totalDeductions),
    label: '${_rate(r.inssEmployeeRate)} + IR',
    icon: Icons.remove_circle_outline,
    variant: ZStatVariant.danger,
  );

  Widget _buildDaysStat(SettlementResult r, bool isDark) => ZStatCard(
    title: 'DIAS TRABAJADOS',
    value: '${r.daysWorked}',
    label: '${NumberFormat('#,##0.00').format(r.monthsWorked)} meses',
    icon: Icons.calendar_today,
    variant: ZStatVariant.info,
  );

  // ══════════════════════════════════════════════════════════════
  //  INGRESOS SECTION
  // ══════════════════════════════════════════════════════════════
  Widget _buildIngresosSection(SettlementResult r, bool isDark) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildSectionHeader('INGRESOS Y PRESTACIONES SOCIALES', Icons.trending_up, ZColors.success),
          const SizedBox(height: ZSpacing.lg),

          if (r.pendingSalaryPay > 0) ...[
            _buildSectionSubtitle('Salario Pendiente de Pago'),
            _buildDataRow(
              label: 'Sueldo pendiente (${r.pendingSalaryDays} días × ${_fmt(r, r.dailySalary)})',
              value: r.pendingSalaryPay, r: r, isDark: isDark,
            ),
            if (r.overtimePay > 0)
              _buildDataRow(
                label: 'Horas extras (${r.overtimeHours} hrs)',
                value: r.overtimePay, r: r, isDark: isDark,
              ),
            _buildTotalRow(
              label: 'SUMAN SALARIOS',
              value: r.pendingSalaryPay + r.overtimePay, r: r, isDark: isDark,
              color: ZColors.brandAccent,
            ),
            const SizedBox(height: ZSpacing.lg),
            const Divider(),
            const SizedBox(height: ZSpacing.lg),
          ],

          _buildSectionSubtitle('Aguinaldo Proporcional (${_legalRef(r, 'aguinaldo')})'),
          _buildDataRow(
            label: 'Salario base mensual × ${NumberFormat('#,##0.00').format(r.monthsWorked / 12)}',
            value: r.aguinaldoPay, r: r, isDark: isDark,
          ),
          if (r.aguinaldoPay > 0)
            ZBadge(
              text: r.aguinaldoPay > r.monthlySalary
                  ? 'Con 1.5× (${_legalRef(r, 'domestic15x')})'
                  : 'Proporcional',
              type: ZBadgeType.info,
            ),
          const SizedBox(height: ZSpacing.lg),

          _buildSectionSubtitle('Vacaciones Pendientes (${_legalRef(r, 'vacaciones')})'),
          _buildDataRow(
            label: 'Días acumulados: ${NumberFormat('#,##0.00').format(r.vacationDaysAccrued)}',
            value: null, r: r, isDark: isDark,
          ),
          _buildDataRow(
            label: 'Días tomados: ${r.vacationDaysTaken}',
            value: null, r: r, isDark: isDark,
          ),
          _buildDataRow(
            label: 'Días a pagar: ${NumberFormat('#,##0.00').format(r.vacationDaysToPay)} × ${_fmt(r, r.vacationDaysToPay > 0 ? r.vacationPay / r.vacationDaysToPay : 0)}',
            value: r.vacationPay, r: r, isDark: isDark, highlight: true,
          ),
          const SizedBox(height: ZSpacing.lg),

          _buildSectionSubtitle('Indemnización por Antigüedad (${_legalRef(r, 'indemnizacion')})'),
          _buildDataRow(
            label: '${NumberFormat('#,##0.00').format(r.severanceDays)} días × ${_fmt(r, r.dailySalary)}',
            value: r.severancePay, r: r, isDark: isDark,
          ),
          if (r.severancePay > 0)
            ZBadge(
              text: r.severancePay > r.monthlySalary
                  ? 'Con 1.5× (${_legalRef(r, 'domestic15x')})'
                  : 'Proporcional',
              type: ZBadgeType.info,
            ),

          if (r.isTrustPosition && r.trustPositionPay > 0) ...[
            const SizedBox(height: ZSpacing.lg),
            _buildSectionSubtitle('Indemnización Cargo de Confianza (${_legalRef(r, 'confianza')})'),
            _buildDataRow(
              label: 'Tope máximo',
              value: r.trustPositionPay, r: r, isDark: isDark,
            ),
          ],

          const SizedBox(height: ZSpacing.lg),
          const Divider(height: 1),
          const SizedBox(height: ZSpacing.lg),
          _buildTotalRow(
            label: 'TOTAL INGRESOS',
            value: r.grossSettlement, r: r, isDark: isDark,
            color: ZColors.success, bold: true,
          ),
        ],
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  DEDUCCIONES SECTION
  // ══════════════════════════════════════════════════════════════
  Widget _buildDeduccionesSection(SettlementResult r, bool isDark) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildSectionHeader('DEDUCCIONES', Icons.remove_circle_outline, ZColors.danger),
          const SizedBox(height: ZSpacing.lg),

          _buildSectionSubtitle('Seguro Social Trabajador (${_rate(r.inssEmployeeRate)})'),
          _buildDataRow(
            label: 'Base: ${_fmt(r, r.pendingSalaryPay + r.vacationPay + r.overtimePay)} (salarios + vacaciones + otros)',
            value: r.inssLaboralAmount, r: r, isDark: isDark, isDeduction: true,
          ),
          const SizedBox(height: ZSpacing.lg),

          _buildSectionSubtitle('Impuesto sobre la Renta (IR)'),
          if (r.irSalaryAmount > 0)
            _buildDataRow(
              label: 'IR - Salario Ordinario (${_legalRef(r, 'ir_salary')})',
              value: r.irSalaryAmount, r: r, isDark: isDark, isDeduction: true,
            ),
          if (r.irOnVacation > 0)
            _buildDataRow(
              label: 'IR - Pagos Ocasionales (${_legalRef(r, 'ir_occasional')})',
              value: r.irOnVacation, r: r, isDark: isDark, isDeduction: true,
            ),
          if (r.irSalaryAmount == 0 && r.irTotalAmount == 0)
            _buildDataRow(
              label: 'No aplica (debajo del umbral)',
              value: 0, r: r, isDark: isDark, isDeduction: true,
            ),

          const SizedBox(height: ZSpacing.lg),
          const Divider(height: 1),
          const SizedBox(height: ZSpacing.lg),
          _buildTotalRow(
            label: 'TOTAL DEDUCCIONES',
            value: r.totalDeductions, r: r, isDark: isDark,
            color: ZColors.danger, bold: true,
          ),
        ],
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  COSTOS PATRONALES SECTION
  // ══════════════════════════════════════════════════════════════
  Widget _buildCostosPatronalesSection(SettlementResult r, bool isDark) {
    return ZCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _buildSectionHeader('COSTOS PATRONALES (Informativo)', Icons.business, ZColors.warning),
          const SizedBox(height: ZSpacing.lg),

          _buildDataRow(
            label: 'Seguro Social Empleador (${_rate(r.inssEmployerRate)})',
            value: r.inssPatronalAmount, r: r, isDark: isDark,
          ),
          const SizedBox(height: ZSpacing.md),
          _buildDataRow(
            label: '${r.otherEmployerName} (${_rate(r.otherEmployerRate)})',
            value: r.inatecAmount, r: r, isDark: isDark,
          ),
          const SizedBox(height: ZSpacing.lg),
          const Divider(height: 1),
          const SizedBox(height: ZSpacing.lg),
          _buildTotalRow(
            label: 'TOTAL COSTO PATRONAL',
            value: r.inssPatronalAmount + r.inatecAmount, r: r, isDark: isDark,
            color: ZColors.warning, bold: true,
          ),
        ],
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  NETO A RECIBIR SECTION
  // ══════════════════════════════════════════════════════════════
  Widget _buildNetoSection(SettlementResult r, bool isDark) {
    return ZCard(
      padding: const EdgeInsets.all(ZSpacing.xl),
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(ZSpacing.xl),
        decoration: BoxDecoration(
          gradient: LinearGradient(
            colors: [ZColors.success.withAlpha(15), ZColors.success.withAlpha(5)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          borderRadius: BorderRadius.circular(ZRadii.lg),
          border: Border.all(color: ZColors.success.withAlpha(40)),
        ),
        child: Column(
          children: [
            Text('NETO A RECIBIR', style: ZTypography.labelLarge.copyWith(
              color: ZColors.success, letterSpacing: 2,
            )),
            const SizedBox(height: ZSpacing.sm),
            Text(
              _fmt(r, r.netSettlement),
              style: ZTypography.displayLarge.copyWith(
                color: ZColors.success, fontWeight: FontWeight.w800,
              ),
            ),
            const SizedBox(height: ZSpacing.sm),
            Text(
              'Ingresos ${_fmt(r, r.grossSettlement)} - Deducciones ${_fmt(r, r.totalDeductions)}',
              style: ZTypography.bodySmall.copyWith(
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  ERROR CARD
  // ══════════════════════════════════════════════════════════════
  Widget _buildErrorCard(Object? error, bool isDark) {
    return ZCard(
      child: Container(
        padding: const EdgeInsets.all(ZSpacing.lg),
        decoration: BoxDecoration(
          color: ZColors.danger.withAlpha(10),
          borderRadius: BorderRadius.circular(ZRadii.md),
          border: Border.all(color: ZColors.danger.withAlpha(30)),
        ),
        child: Row(
          children: [
            const Icon(Icons.error_outline, color: ZColors.danger, size: 24),
            const SizedBox(width: ZSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Error al calcular la liquidación', style: ZTypography.titleMedium.copyWith(color: ZColors.danger)),
                  const SizedBox(height: ZSpacing.xs),
                  Text('$error', style: ZTypography.bodySmall.copyWith(
                    color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                  )),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  // ══════════════════════════════════════════════════════════════
  //  SHARED HELPERS
  // ══════════════════════════════════════════════════════════════
  Widget _buildSectionHeader(String title, IconData icon, Color color) {
    return Row(
      children: [
        Container(
          width: 32, height: 32,
          decoration: BoxDecoration(
            color: color.withAlpha(20),
            borderRadius: BorderRadius.circular(ZRadii.sm),
          ),
          child: Icon(icon, size: 18, color: color),
        ),
        const SizedBox(width: ZSpacing.md),
        Text(title, style: ZTypography.titleMedium),
      ],
    );
  }

  Widget _buildSectionSubtitle(String text) {
    return Padding(
      padding: const EdgeInsets.only(bottom: ZSpacing.sm),
      child: Text(text, style: ZTypography.labelMedium.copyWith(color: ZColors.neutral500)),
    );
  }

  Widget _buildDataRow({
    required String label,
    required double? value,
    required SettlementResult r,
    required bool isDark,
    bool isDeduction = false,
    bool highlight = false,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: ZSpacing.xs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Text(label, style: ZTypography.bodyMedium.copyWith(
              color: highlight ? ZColors.brandAccent : (isDark ? ZColors.neutral300 : ZColors.neutral600),
              fontWeight: highlight ? FontWeight.w600 : FontWeight.w400,
            )),
          ),
          if (value != null)
            Text(
              isDeduction ? '- ${_fmt(r, value)}' : _fmt(r, value),
              style: ZTypography.bodyMedium.copyWith(
                fontWeight: FontWeight.w600,
                color: isDeduction ? ZColors.danger : (highlight ? ZColors.brandAccent : null),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildTotalRow({
    required String label,
    required double value,
    required SettlementResult r,
    required bool isDark,
    required Color color,
    bool bold = false,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(vertical: ZSpacing.sm, horizontal: ZSpacing.md),
      decoration: BoxDecoration(
        color: color.withAlpha(10),
        borderRadius: BorderRadius.circular(ZRadii.sm),
      ),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: ZTypography.titleSmall.copyWith(
            color: color,
            fontWeight: bold ? FontWeight.w700 : FontWeight.w600,
          )),
          Text(
            _fmt(r, value),
            style: ZTypography.titleMedium.copyWith(
              color: color,
              fontWeight: bold ? FontWeight.w800 : FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}
