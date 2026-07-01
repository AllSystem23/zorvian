import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../shared/ds/ds.dart';
import '../../../auth/auth_provider.dart';

class WorkshopFormPage extends ConsumerStatefulWidget {
  final String? workshopId;
  const WorkshopFormPage({super.key, this.workshopId});
  @override
  ConsumerState<WorkshopFormPage> createState() => _WorkshopFormPageState();
}

class _WorkshopFormPageState extends ConsumerState<WorkshopFormPage> {
  final _formKey = GlobalKey<FormState>();
  final _codeCtrl = TextEditingController();
  final _nameCtrl = TextEditingController();
  final _legalNameCtrl = TextEditingController();
  final _taxIdCtrl = TextEditingController();
  final _contactCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _cityCtrl = TextEditingController();
  final _countryCtrl = TextEditingController();
  final _responseHoursCtrl = TextEditingController(text: '48');
  final _repairHoursCtrl = TextEditingController(text: '72');
  final _notesCtrl = TextEditingController();

  bool _saving = false;
  String? _error;
  bool _loadingDetail = false;

  bool get _isEdit => widget.workshopId != null;

  @override
  void initState() {
    super.initState();
    if (_isEdit) _loadDetail();
  }

  Future<void> _loadDetail() async {
    setState(() => _loadingDetail = true);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('service-workshops/${widget.workshopId}');
      final d = r.data as Map<String, dynamic>;
      _codeCtrl.text = d['code'] ?? '';
      _nameCtrl.text = d['name'] ?? '';
      _legalNameCtrl.text = d['legalName'] ?? '';
      _taxIdCtrl.text = d['taxId'] ?? '';
      _contactCtrl.text = d['contactName'] ?? '';
      _phoneCtrl.text = d['phone'] ?? '';
      _emailCtrl.text = d['email'] ?? '';
      _addressCtrl.text = d['address'] ?? '';
      _cityCtrl.text = d['city'] ?? '';
      _countryCtrl.text = d['country'] ?? '';
      _responseHoursCtrl.text = '${d['avgResponseHours'] ?? 48}';
      _repairHoursCtrl.text = '${d['avgRepairHours'] ?? 72}';
      _notesCtrl.text = d['notes'] ?? '';
    } catch (e) {
      setState(() => _error = 'Error al cargar taller');
    } finally {
      if (mounted) setState(() => _loadingDetail = false);
    }
  }

  @override
  void dispose() {
    _codeCtrl.dispose(); _nameCtrl.dispose(); _legalNameCtrl.dispose();
    _taxIdCtrl.dispose(); _contactCtrl.dispose(); _phoneCtrl.dispose();
    _emailCtrl.dispose(); _addressCtrl.dispose(); _cityCtrl.dispose();
    _countryCtrl.dispose(); _responseHoursCtrl.dispose(); _repairHoursCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    setState(() { _saving = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final data = {
        'code': _codeCtrl.text.trim(),
        'name': _nameCtrl.text.trim(),
        'legalName': _legalNameCtrl.text.trim().isNotEmpty ? _legalNameCtrl.text.trim() : null,
        'taxId': _taxIdCtrl.text.trim().isNotEmpty ? _taxIdCtrl.text.trim() : null,
        'contactName': _contactCtrl.text.trim().isNotEmpty ? _contactCtrl.text.trim() : null,
        'phone': _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
        'email': _emailCtrl.text.trim().isNotEmpty ? _emailCtrl.text.trim() : null,
        'address': _addressCtrl.text.trim().isNotEmpty ? _addressCtrl.text.trim() : null,
        'city': _cityCtrl.text.trim().isNotEmpty ? _cityCtrl.text.trim() : null,
        'country': _countryCtrl.text.trim().isNotEmpty ? _countryCtrl.text.trim() : null,
        'avgResponseHours': int.tryParse(_responseHoursCtrl.text) ?? 48,
        'avgRepairHours': int.tryParse(_repairHoursCtrl.text) ?? 72,
        'notes': _notesCtrl.text.trim().isNotEmpty ? _notesCtrl.text.trim() : null,
      };

      if (_isEdit) {
        await dio.put('service-workshops/${widget.workshopId}', data: data);
      } else {
        data['branchId'] = '00000000-0000-0000-0000-000000000001';
        await dio.post('service-workshops', data: data);
      }
      if (mounted) context.pop(true);
    } catch (e) {
      setState(() => _error = 'Error al guardar: $e');
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loadingDetail) {
      return Scaffold(
        appBar: AppBar(title: const Text('Taller')),
        body: const Center(child: CircularProgressIndicator(color: ZColors.brandPrimary)),
      );
    }

    final isWide = MediaQuery.of(context).size.width > 700;

    return Scaffold(
      appBar: AppBar(
        title: Text(_isEdit ? 'Editar taller' : 'Nuevo taller'),
        actions: [
          ZButton(
            text: _saving ? 'Guardando...' : 'Guardar',
            icon: Icons.check,
            type: ZButtonType.primary,
            onPressed: _saving ? () {} : _save,
          ),
        ],
      ),
      body: _error != null
          ? Center(child: ZAlertCard(message: _error!, severity: 'high'))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(ZSpacing.lg),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Identity
                    _SectionTitle(title: 'Identificación'),
                    const SizedBox(height: ZSpacing.md),
                    if (isWide)
                      Row(children: [
                        Expanded(child: ZTextField(controller: _codeCtrl, label: 'Código *', validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null)),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: ZTextField(controller: _nameCtrl, label: 'Nombre *', validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null)),
                      ])
                    else ...[
                      ZTextField(controller: _codeCtrl, label: 'Código *', validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null),
                      const SizedBox(height: ZSpacing.md),
                      ZTextField(controller: _nameCtrl, label: 'Nombre *', validator: (v) => v == null || v.trim().isEmpty ? 'Requerido' : null),
                    ],
                    const SizedBox(height: ZSpacing.md),
                    if (isWide)
                      Row(children: [
                        Expanded(child: ZTextField(controller: _legalNameCtrl, label: 'Razón social')),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: ZTextField(controller: _taxIdCtrl, label: 'NIT / RIF')),
                      ])
                    else ...[
                      ZTextField(controller: _legalNameCtrl, label: 'Razón social'),
                      const SizedBox(height: ZSpacing.md),
                      ZTextField(controller: _taxIdCtrl, label: 'NIT / RIF'),
                    ],

                    const SizedBox(height: ZSpacing.xl),
                    _SectionTitle(title: 'Contacto'),
                    const SizedBox(height: ZSpacing.md),
                    ZTextField(controller: _contactCtrl, label: 'Persona de contacto'),
                    const SizedBox(height: ZSpacing.md),
                    if (isWide)
                      Row(children: [
                        Expanded(child: ZTextField(controller: _phoneCtrl, label: 'Teléfono', keyboardType: TextInputType.phone)),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: ZTextField(controller: _emailCtrl, label: 'Email', keyboardType: TextInputType.emailAddress)),
                      ])
                    else ...[
                      ZTextField(controller: _phoneCtrl, label: 'Teléfono', keyboardType: TextInputType.phone),
                      const SizedBox(height: ZSpacing.md),
                      ZTextField(controller: _emailCtrl, label: 'Email', keyboardType: TextInputType.emailAddress),
                    ],

                    const SizedBox(height: ZSpacing.xl),
                    _SectionTitle(title: 'Ubicación'),
                    const SizedBox(height: ZSpacing.md),
                    ZTextField(controller: _addressCtrl, label: 'Dirección'),
                    const SizedBox(height: ZSpacing.md),
                    if (isWide)
                      Row(children: [
                        Expanded(child: ZTextField(controller: _cityCtrl, label: 'Ciudad')),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: ZTextField(controller: _countryCtrl, label: 'País')),
                      ])
                    else ...[
                      ZTextField(controller: _cityCtrl, label: 'Ciudad'),
                      const SizedBox(height: ZSpacing.md),
                      ZTextField(controller: _countryCtrl, label: 'País'),
                    ],

                    const SizedBox(height: ZSpacing.xl),
                    _SectionTitle(title: 'Métricas de SLA'),
                    const SizedBox(height: ZSpacing.md),
                    if (isWide)
                      Row(children: [
                        Expanded(child: ZTextField(controller: _responseHoursCtrl, label: 'Tiempo respuesta (horas)', keyboardType: TextInputType.number)),
                        const SizedBox(width: ZSpacing.md),
                        Expanded(child: ZTextField(controller: _repairHoursCtrl, label: 'Tiempo reparación (horas)', keyboardType: TextInputType.number)),
                      ])
                    else ...[
                      ZTextField(controller: _responseHoursCtrl, label: 'Tiempo respuesta (horas)', keyboardType: TextInputType.number),
                      const SizedBox(height: ZSpacing.md),
                      ZTextField(controller: _repairHoursCtrl, label: 'Tiempo reparación (horas)', keyboardType: TextInputType.number),
                    ],

                    const SizedBox(height: ZSpacing.xl),
                    _SectionTitle(title: 'Notas'),
                    const SizedBox(height: ZSpacing.md),
                    ZTextField(controller: _notesCtrl, label: 'Notas internas', maxLines: 3),

                    const SizedBox(height: ZSpacing.xxl),
                    SizedBox(
                      width: double.infinity,
                      child: ZButton(
                        text: _saving ? 'Guardando...' : 'Guardar taller',
                        icon: Icons.check,
                        type: ZButtonType.primary,
                        isLoading: _saving,
                        onPressed: _saving ? () {} : _save,
                      ),
                    ),
                    const SizedBox(height: ZSpacing.xl),
                  ],
                ),
              ),
            ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  final String title;
  const _SectionTitle({required this.title});
  @override
  Widget build(BuildContext context) {
    return Text(title, style: ZTypography.titleMedium.copyWith(color: ZColors.brandPrimary));
  }
}
