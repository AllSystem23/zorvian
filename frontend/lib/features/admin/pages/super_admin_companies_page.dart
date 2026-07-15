import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:dio/dio.dart';
import 'package:http_parser/http_parser.dart';
import 'package:file_picker/file_picker.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';
import '../config/subscription_plans_config.dart';

/// SuperAdmin-only page to list, edit, create, deactivate, and switch between companies.
/// Uses ZDataTable for table rendering with search and column toggle.
class SuperAdminCompaniesPage extends ConsumerStatefulWidget {
  const SuperAdminCompaniesPage({super.key});

  @override
  ConsumerState<SuperAdminCompaniesPage> createState() => _SuperAdminCompaniesPageState();
}

class _SuperAdminCompaniesPageState extends ConsumerState<SuperAdminCompaniesPage> {
  List<Map<String, dynamic>> _companies = [];
  List<Map<String, dynamic>> _filtered = [];
  bool _loading = true;
  String? _error;
  String _searchQuery = '';

  @override
  void initState() {
    super.initState();
    _loadCompanies();
  }

  Future<void> _loadCompanies() async {
    setState(() { _loading = true; _error = null; });
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('companies/all');
      final data = response.data as List<dynamic>;
      setState(() {
        _companies = data.map((e) => Map<String, dynamic>.from(e as Map)).toList();
        _applyFilter();
        _loading = false;
      });
    } catch (e) {
      if (mounted) setState(() { _error = e.toString(); _loading = false; });
    }
  }      void _applyFilter() {
    if (_searchQuery.isEmpty) {
      _filtered = List.from(_companies);
    } else {
      final q = _searchQuery.toLowerCase();
      _filtered = _companies.where((c) {
        return (c['name'] as String? ?? '').toLowerCase().contains(q) ||
            (c['legalName'] as String? ?? '').toLowerCase().contains(q) ||
            (c['country'] as String? ?? '').toLowerCase().contains(q) ||
            (c['taxId'] as String? ?? '').toLowerCase().contains(q) ||
            (c['email'] as String? ?? '').toLowerCase().contains(q) ||
            (c['phone'] as String? ?? '').toLowerCase().contains(q);
      }).toList();
    }
  }

  Future<void> _switchToCompany(String tenantId, String name) async {
    final confirmed = await _confirmAction(
      'Cambiar de empresa',
      '¿Deseas entrar a "$name"? Se cambiará el contexto activo.',
    );
    if (!confirmed || !mounted) return;
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final success = await ref.read(authProvider.notifier).switchTenant(tenantId);
    if (success && mounted) {
      messenger.showSnackBar(SnackBar(content: Text('Entrando a: $name')));
      router.go('/dashboard');
    } else if (mounted) {
      _showError('No se pudo cambiar de empresa');
    }
  }

  Future<void> _deactivateCompany(String id, String name) async {
    final confirmed = await _confirmDestructive(
      'Desactivar empresa',
      '¿Estás seguro de desactivar "$name"? Esta acción puede revertirse editando la empresa.',
    );
    if (!confirmed || !mounted) return;
    try {
      final dio = ref.read(dioClientProvider);
      await dio.delete('companies/$id');
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$name desactivada')),
      );
      await _loadCompanies();
    } on Exception catch (e) {
      if (!mounted) return;
      final msg = e.toString().contains('409')
          ? 'No se puede desactivar la última empresa activa'
          : 'Error al desactivar: $e';
      _showError(msg);
    }
  }

  void _showEditDialog(Map<String, dynamic> company) {
    showDialog(
      context: context,
      builder: (_) => _CompanyEditDialog(
        company: company,
        onSaved: _loadCompanies,
      ),
    );
  }

  Future<bool> _confirmAction(String title, String message) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: Icon(Icons.help_outline, color: ZColors.brandAccent, size: 36),
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Confirmar')),
        ],
      ),
    );
    return result ?? false;
  }

  Future<bool> _confirmDestructive(String title, String message) async {
    final result = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        icon: const Icon(Icons.warning_amber_rounded, color: ZColors.danger, size: 36),
        title: Text(title),
        content: Text(message),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Cancelar')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: ZColors.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Desactivar'),
          ),
        ],
      ),
    );
    return result ?? false;
  }

  void _showError(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(msg), backgroundColor: ZColors.danger),
    );
  }

  @override
  Widget build(BuildContext context) {
    final currentTenant = ref.watch(authProvider).tenantId;

    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 8),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.end,
              children: [
                IconButton(
                  icon: const Icon(Icons.refresh),
                  tooltip: 'Actualizar',
                  onPressed: _loadCompanies,
                ),
              ],
            ),
          ),
          Expanded(
            child: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? _buildError()
              : _buildTable(currentTenant),
          ),
        ],
      ),
    );
  }

  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.cloud_off, size: 48, color: ZColors.danger),
            const SizedBox(height: 16),
            Text('Error al cargar empresas', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
            const SizedBox(height: 8),
            Text(_error!, style: ZTypography.labelSmall, textAlign: TextAlign.center),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: _loadCompanies,
              icon: const Icon(Icons.refresh, size: 18),
              label: const Text('Reintentar'),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildTable(String? currentTenant) {
    if (_companies.isEmpty) {
      return const ZEmptyState(
        icon: Icons.business_outlined,
        title: 'Sin empresas registradas',
        subtitle: 'Crea la primera empresa para comenzar',
      );
    }

    final columns = [
      const ZColumn(id: 'name', label: 'Empresa', width: 220),
      const ZColumn(id: 'email', label: 'Email', width: 180),
      const ZColumn(id: 'phone', label: 'Teléfono', width: 140),
      const ZColumn(id: 'country', label: 'País', width: 110),
      const ZColumn(id: 'currency', label: 'Moneda', width: 70),
      const ZColumn(id: 'plan', label: 'Plan', width: 90),
      const ZColumn(id: 'status', label: 'Estado', width: 90),
      const ZColumn(id: 'actions', label: 'Acciones', width: 160),
    ];

    return Padding(
      padding: const EdgeInsets.all(24),
      child: ZDataTable<Map<String, dynamic>>(
        columns: columns,
        rows: _filtered,
        isLoading: false,
        emptyMessage: 'No se encontraron empresas',
        onSearch: (q) {
          setState(() {
            _searchQuery = q;
            _applyFilter();
          });
        },
        rowMapper: (company) {
          final tenantId = company['tenantId'] as String? ?? '';
          final companyId = company['id'] as String? ?? '';
          final name = company['name'] as String? ?? '';
          final logoUrl = company['logoUrl'] as String?;
          final isCurrent = tenantId == currentTenant;
          final isActive = company['isActive'] == true;

          return DataRow(cells: [
            DataCell(
              Row(
                children: [
                  _buildLogoAvatar(logoUrl, isCurrent),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Text(name, style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                        if (company['legalName'] != null && company['legalName'] != name)
                          Text(
                            company['legalName'] as String,
                            style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
                            overflow: TextOverflow.ellipsis,
                          ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            DataCell(Text(company['email'] as String? ?? '—', style: ZTypography.bodyMedium.copyWith(
              color: (company['email'] as String? ?? '').isNotEmpty ? null : ZColors.neutral400,
            ))),
            DataCell(Text(company['phone'] as String? ?? '—', style: ZTypography.bodyMedium.copyWith(
              color: (company['phone'] as String? ?? '').isNotEmpty ? null : ZColors.neutral400,
            ))),
            DataCell(ZBadge(
              text: company['country'] as String? ?? '',
              type: ZBadgeType.neutral,
            )),
            DataCell(Text(company['currency'] as String? ?? '', style: ZTypography.bodyMedium)),
            DataCell(ZBadge(
              text: (company['subscriptionPlan'] as String? ?? 'starter').toUpperCase(),
              type: _planBadgeType(company['subscriptionPlan'] as String? ?? 'starter'),
            )),
            DataCell(ZBadge(
              text: isCurrent ? 'ACTIVA' : (isActive ? 'ACTIVA' : 'INACTIVA'),
              type: isCurrent
                  ? ZBadgeType.accent
                  : (isActive ? ZBadgeType.success : ZBadgeType.danger),
            )),
            DataCell(
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  if (!isCurrent)
                    IconButton(
                      icon: const Icon(Icons.login, size: 18),
                      tooltip: 'Entrar a esta empresa',
                      onPressed: () => _switchToCompany(tenantId, name),
                    ),
                  IconButton(
                    icon: const Icon(Icons.edit_outlined, size: 18),
                    tooltip: 'Editar empresa',
                    onPressed: () => _showEditDialog(company),
                  ),
                  if (!isCurrent)
                    IconButton(
                      icon: Icon(Icons.block, size: 18, color: isActive ? ZColors.danger : ZColors.neutral400),
                      tooltip: isActive ? 'Desactivar empresa' : 'Ya está desactivada',
                      onPressed: isActive ? () => _deactivateCompany(companyId, name) : null,
                    ),
                ],
              ),
            ),
          ]);
        },
      ),
    );
  }

  Widget _buildLogoAvatar(String? logoUrl, bool isCurrent) {
    if (logoUrl != null && logoUrl.isNotEmpty) {
      return ClipRRect(
        borderRadius: BorderRadius.circular(8),
        child: Image.network(
          logoUrl,
          width: 36,
          height: 36,
          fit: BoxFit.cover,
          errorBuilder: (context, error, stack) => _buildFallbackIcon(isCurrent),
        ),
      );
    }
    return _buildFallbackIcon(isCurrent);
  }

  ZBadgeType _planBadgeType(String plan) => switch (plan) {
    'professional' => ZBadgeType.accent,
    'enterprise' => ZBadgeType.warning,
    _ => ZBadgeType.neutral,
  };

  Widget _buildFallbackIcon(bool isCurrent) {
    return Container(
      width: 36,
      height: 36,
      decoration: BoxDecoration(
        color: isCurrent
            ? ZColors.brandAccent.withValues(alpha: 0.15)
            : ZColors.brandPrimary.withValues(alpha: 0.1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Icon(
        Icons.business,
        size: 18,
        color: isCurrent ? ZColors.brandAccent : ZColors.brandPrimary,
      ),
    );
  }
}

// ─── Create Dialog (public for ZQuickActionsFAB callback) ───

class CompanyCreateDialog extends ConsumerStatefulWidget {
  final VoidCallback onCreated;
  const CompanyCreateDialog({super.key, required this.onCreated});

  @override
  ConsumerState<CompanyCreateDialog> createState() => _CompanyCreateDialogState();
}

class _CompanyCreateDialogState extends ConsumerState<CompanyCreateDialog> {
  final _formKey = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _legalNameCtrl = TextEditingController();
  final _taxIdCtrl = TextEditingController();
  final _phoneCtrl = TextEditingController();
  final _addressCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _maxEmployeesCtrl = TextEditingController(text: '50');
  String _selectedCountry = 'Nicaragua';
  bool _loading = false;
  String? _error;

  static const _countries = ['Nicaragua', 'Costa Rica', 'Guatemala', 'Honduras', 'El Salvador', 'Panamá'];

  String _countryToCurrency(String country) => switch (country) {
    'Nicaragua' => 'NIO',
    'Costa Rica' => 'CRC',
    'Guatemala' => 'GTQ',
    'Honduras' => 'HNL',
    'El Salvador' => 'USD',
    'Panamá' => 'USD',
    _ => 'NIO',
  };

  String _countryToTimezone(String country) => switch (country) {
    'Nicaragua' => 'America/Managua',
    'Costa Rica' => 'America/Costa_Rica',
    'Guatemala' => 'America/Guatemala',
    'Honduras' => 'America/Tegucigalpa',
    'El Salvador' => 'America/El_Salvador',
    'Panamá' => 'America/Panama',
    _ => 'America/Managua',
  };

  @override
  void dispose() {
    _nameCtrl.dispose();
    _legalNameCtrl.dispose();
    _taxIdCtrl.dispose();
    _phoneCtrl.dispose();
    _addressCtrl.dispose();
    _emailCtrl.dispose();
    _maxEmployeesCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('companies', data: {
        'name': _nameCtrl.text.trim(),
        'legalName': _legalNameCtrl.text.trim(),
        'taxId': _taxIdCtrl.text.trim().isNotEmpty ? _taxIdCtrl.text.trim() : null,
        'phone': _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
        'address': _addressCtrl.text.trim().isNotEmpty ? _addressCtrl.text.trim() : null,
        'email': _emailCtrl.text.trim().isNotEmpty ? _emailCtrl.text.trim() : null,
        'country': _selectedCountry,
        'currency': _countryToCurrency(_selectedCountry),
        'timezone': _countryToTimezone(_selectedCountry),
        'maxEmployees': int.tryParse(_maxEmployeesCtrl.text) ?? 50,
      });
      widget.onCreated();
      if (mounted) Navigator.of(context).pop();
    } catch (e) {
      if (mounted) setState(() { _error = e.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: const Text('Nueva Empresa'),
      content: Form(
        key: _formKey,
        child: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (_error != null)
                ZAlertCard(message: _error!, severity: 'high'),
              ZTextField(
                controller: _nameCtrl,
                label: 'Nombre comercial',
                prefix: const Icon(Icons.business),
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _legalNameCtrl,
                label: 'Razón social',
                prefix: const Icon(Icons.description),
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _taxIdCtrl,
                label: 'RUC / NIT',
                prefix: const Icon(Icons.numbers),
              ),
              const SizedBox(height: ZSpacing.md),
              DropdownButtonFormField<String>(
                initialValue: _selectedCountry,
                decoration: const InputDecoration(
                  labelText: 'País',
                  prefixIcon: Icon(Icons.public),
                ),
                items: _countries.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
                onChanged: (v) => setState(() => _selectedCountry = v!),
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _emailCtrl,
                label: 'Correo Electrónico',
                prefix: const Icon(Icons.email_outlined),
                keyboardType: TextInputType.emailAddress,
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _phoneCtrl,
                label: 'Teléfono',
                prefix: const Icon(Icons.phone_outlined),
                keyboardType: TextInputType.phone,
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _addressCtrl,
                label: 'Dirección',
                prefix: const Icon(Icons.location_on_outlined),
              ),
              const SizedBox(height: ZSpacing.md),
              ZTextField(
                controller: _maxEmployeesCtrl,
                label: 'Máximo de Empleados',
                prefix: const Icon(Icons.people_outline),
                keyboardType: TextInputType.number,
              ),
            ],
          ),
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        FilledButton(
          onPressed: _loading ? null : _submit,
          child: _loading
              ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Crear Empresa'),
        ),
      ],
    );
  }
}

// ─── Edit Dialog ───

class _CompanyEditDialog extends ConsumerStatefulWidget {
  final Map<String, dynamic> company;
  final VoidCallback onSaved;
  const _CompanyEditDialog({required this.company, required this.onSaved});

  @override
  ConsumerState<_CompanyEditDialog> createState() => _CompanyEditDialogState();
}

class _CompanyEditDialogState extends ConsumerState<_CompanyEditDialog> {
  late final TextEditingController _nameCtrl;
  late final TextEditingController _legalNameCtrl;
  late final TextEditingController _taxIdCtrl;
  late final TextEditingController _phoneCtrl;
  late final TextEditingController _addressCtrl;
  late final TextEditingController _emailCtrl;
  late final TextEditingController _maxEmployeesCtrl;
  late String _selectedCountry;
  late String _selectedCurrency;
  late String _selectedTimezone;
  late bool _isActive;
  late String _subscriptionPlan;
  bool _loading = false;
  String? _error;

  // Logo upload state
  Uint8List? _logoBytes;
  String? _logoFileName;
  String? _currentLogoUrl;
  bool _logoUploading = false;

  static const _countries = ['Nicaragua', 'Costa Rica', 'Guatemala', 'Honduras', 'El Salvador', 'Panamá'];
  static const _currencies = ['NIO', 'CRC', 'GTQ', 'HNL', 'USD'];
  static const _timezones = ['America/Managua', 'America/Costa_Rica', 'America/Guatemala', 'America/Tegucigalpa', 'America/El_Salvador', 'America/Panama', 'UTC'];

  @override
  void initState() {
    super.initState();
    final c = widget.company;
    _nameCtrl = TextEditingController(text: c['name'] as String? ?? '');
    _legalNameCtrl = TextEditingController(text: c['legalName'] as String? ?? '');
    _taxIdCtrl = TextEditingController(text: c['taxId'] as String? ?? '');
    _phoneCtrl = TextEditingController(text: c['phone'] as String? ?? '');
    _addressCtrl = TextEditingController(text: c['address'] as String? ?? '');
    _emailCtrl = TextEditingController(text: c['email'] as String? ?? '');
    _maxEmployeesCtrl = TextEditingController(text: (c['maxEmployees'] ?? 50).toString());
    _selectedCountry = c['country'] as String? ?? 'Nicaragua';
    _selectedCurrency = c['currency'] as String? ?? 'NIO';
    _selectedTimezone = c['timezone'] as String? ?? 'America/Managua';
    _isActive = c['isActive'] == true;
    _subscriptionPlan = c['subscriptionPlan'] as String? ?? 'starter';
    _currentLogoUrl = c['logoUrl'] as String?;
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _legalNameCtrl.dispose();
    _taxIdCtrl.dispose();
    _phoneCtrl.dispose();
    _addressCtrl.dispose();
    _emailCtrl.dispose();
    _maxEmployeesCtrl.dispose();
    super.dispose();
  }

  Future<void> _pickLogo() async {
    final result = await FilePicker.pickFiles(type: FileType.image);
    if (!mounted) return;
    if (result != null && result.files.isNotEmpty) {
      final file = result.files.first;
      if (file.size > 5 * 1024 * 1024) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('El logo no puede superar 5 MB'), backgroundColor: ZColors.danger),
        );
        return;
      }
      final bytes = await file.readAsBytes();
      if (!mounted) return;
      setState(() {
        _logoBytes = bytes;
        _logoFileName = file.name;
      });
    }
  }

  Future<bool> _uploadLogo(String companyId) async {
    if (_logoBytes == null || _logoFileName == null) return true;

    setState(() => _logoUploading = true);
    try {
      final dio = ref.read(dioClientProvider);
      final fileName = _logoFileName!;
      final ext = fileName.split('.').last.toLowerCase();
      final contentType = switch (ext) {
        'png' => 'image/png',
        'jpg' || 'jpeg' => 'image/jpeg',
        'webp' => 'image/webp',
        _ => 'image/png',
      };

      final formData = FormData.fromMap({
        'file': MultipartFile.fromBytes(
          _logoBytes!,
          filename: fileName,
          contentType: MediaType.parse(contentType),
        ),
      });

      await dio.post('companies/$companyId/logo', data: formData);
      return true;
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error al subir logo: $e'), backgroundColor: ZColors.danger),
        );
      }
      return false;
    } finally {
      if (mounted) setState(() => _logoUploading = false);
    }
  }

  Future<void> _submit() async {
    if (_nameCtrl.text.trim().isEmpty) {
      setState(() => _error = 'El nombre es requerido');
      return;
    }

    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      final companyId = widget.company['id'] as String;

      // Upload logo first if changed
      if (_logoBytes != null) {
        final logoOk = await _uploadLogo(companyId);
        if (!logoOk || !mounted) {
          if (mounted) setState(() { _loading = false; });
          return;
        }
      }

      if (!mounted) return;
      await dio.put('companies/$companyId', data: {
        'name': _nameCtrl.text.trim(),
        'legalName': _legalNameCtrl.text.trim(),
        'taxId': _taxIdCtrl.text.trim().isNotEmpty ? _taxIdCtrl.text.trim() : null,
        'phone': _phoneCtrl.text.trim().isNotEmpty ? _phoneCtrl.text.trim() : null,
        'address': _addressCtrl.text.trim().isNotEmpty ? _addressCtrl.text.trim() : null,
        'email': _emailCtrl.text.trim().isNotEmpty ? _emailCtrl.text.trim() : null,
        'country': _selectedCountry,
        'currency': _selectedCurrency,
        'timezone': _selectedTimezone,
        'maxEmployees': int.tryParse(_maxEmployeesCtrl.text) ?? 50,
        'isActive': _isActive,
        'subscriptionPlan': _subscriptionPlan,
      });
      widget.onSaved();
      if (mounted) Navigator.pop(context);
    } catch (e) {
      if (mounted) setState(() { _error = e.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text('Editar: ${widget.company['name'] ?? ''}'),
      content: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (_error != null)
              ZAlertCard(message: _error!, severity: 'high'),

            // ── Logo Upload Section ──
            _buildLogoSection(),
            const SizedBox(height: ZSpacing.lg),

            ZTextField(controller: _nameCtrl, label: 'Nombre comercial', prefix: const Icon(Icons.business)),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _legalNameCtrl, label: 'Razón social', prefix: const Icon(Icons.description)),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _taxIdCtrl, label: 'RUC / NIT', prefix: const Icon(Icons.numbers)),
            const SizedBox(height: ZSpacing.md),
            DropdownButtonFormField<String>(
              initialValue: _selectedCountry,
              decoration: const InputDecoration(labelText: 'País', prefixIcon: Icon(Icons.public)),
              items: _countries.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
              onChanged: (v) => setState(() => _selectedCountry = v!),
            ),
            const SizedBox(height: ZSpacing.md),
            DropdownButtonFormField<String>(
              initialValue: _selectedCurrency,
              decoration: const InputDecoration(labelText: 'Moneda', prefixIcon: Icon(Icons.monetization_on)),
              items: _currencies.map((c) => DropdownMenuItem(value: c, child: Text(c))).toList(),
              onChanged: (v) => setState(() => _selectedCurrency = v!),
            ),
            const SizedBox(height: ZSpacing.md),
            DropdownButtonFormField<String>(
              initialValue: _selectedTimezone,
              decoration: const InputDecoration(labelText: 'Zona Horaria', prefixIcon: Icon(Icons.access_time)),
              items: _timezones.map((t) => DropdownMenuItem(value: t, child: Text(t))).toList(),
              onChanged: (v) => setState(() => _selectedTimezone = v!),
            ),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _emailCtrl, label: 'Correo Electrónico', prefix: const Icon(Icons.email_outlined), keyboardType: TextInputType.emailAddress),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _phoneCtrl, label: 'Teléfono', prefix: const Icon(Icons.phone_outlined), keyboardType: TextInputType.phone),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _addressCtrl, label: 'Dirección', prefix: const Icon(Icons.location_on_outlined)),
            const SizedBox(height: ZSpacing.md),
            ZTextField(controller: _maxEmployeesCtrl, label: 'Máximo de Empleados', prefix: const Icon(Icons.people_outline), keyboardType: TextInputType.number),
            const SizedBox(height: ZSpacing.md),
            SwitchListTile(
              title: const Text('Empresa activa'),
              subtitle: Text(
                _isActive ? 'La empresa está activa y accesible' : 'La empresa está desactivada',
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
              ),
              value: _isActive,
              activeThumbColor: ZColors.brandAccent,
              onChanged: (v) => setState(() => _isActive = v),
            ),
            const SizedBox(height: ZSpacing.md),
            // ── Subscription Plan Selector ──
            _buildPlanSelector(ref.watch(subscriptionPlansProvider).value ?? SubscriptionPlanConfig.fallbackAll),
          ],
        ),
      ),
      actions: [
        TextButton(onPressed: () => Navigator.pop(context), child: const Text('Cancelar')),
        FilledButton(
          onPressed: (_loading || _logoUploading) ? null : _submit,
          child: (_loading || _logoUploading)
              ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
              : const Text('Guardar Cambios'),
        ),
      ],
    );
  }

  Widget _buildLogoSection() {
    final hasPreview = _logoBytes != null;
    final hasCurrent = _currentLogoUrl != null;

    return Center(
      child: Column(
        children: [
          // Logo preview
          GestureDetector(
            onTap: _pickLogo,
            child: Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                color: ZColors.brandPrimary.withValues(alpha: 0.05),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: hasPreview ? ZColors.brandAccent : ZColors.neutral400,
                  width: hasPreview ? 2 : 1,
                ),
              ),
              child: hasPreview
                  ? ClipRRect(
                      borderRadius: BorderRadius.circular(14),
                      child: Image.memory(_logoBytes!, fit: BoxFit.cover),
                    )
                  : hasCurrent
                      ? ClipRRect(
                          borderRadius: BorderRadius.circular(14),
                          child: Image.network(
                            _currentLogoUrl!,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stack) => _buildLogoPlaceholder(),
                          ),
                        )
                      : _buildLogoPlaceholder(),
            ),
          ),
          const SizedBox(height: 8),
          // Upload button
          FilledButton.tonalIcon(
            onPressed: _logoUploading ? null : _pickLogo,
            icon: _logoUploading
                ? const SizedBox(width: 16, height: 16, child: CircularProgressIndicator(strokeWidth: 2))
                : Icon(Icons.camera_alt_outlined, size: 16, color: _logoBytes != null ? ZColors.brandAccent : null),
            label: Text(
              _logoBytes != null ? 'Cambiar logo' : 'Subir logo',
              style: TextStyle(color: _logoBytes != null ? ZColors.brandAccent : null),
            ),
          ),
          if (_logoFileName != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(_logoFileName!, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
            ),
        ],
      ),
    );
  }

  Widget _buildLogoPlaceholder() {
    return Center(
      child: Icon(
        Icons.add_a_photo_outlined,
        size: 32,
        color: ZColors.neutral400,
      ),
    );
  }

  Widget _buildPlanSelector(List<SubscriptionPlanConfig> plans) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('PLAN DE SUSCRIPCIÓN', style: ZTypography.labelSmall.copyWith(
          color: ZColors.neutral500, letterSpacing: 1.2)),
        const SizedBox(height: 12),
        ...plans.map((plan) {
          final isSelected = _subscriptionPlan == plan.id;
          final priceLabel = plan.displayPrice;
          return GestureDetector(
            onTap: () => setState(() => _subscriptionPlan = plan.id),
            child: Container(
              margin: const EdgeInsets.only(bottom: 8),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: isSelected ? plan.color.withValues(alpha: 0.08) : Colors.transparent,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(
                  color: isSelected ? plan.color : ZColors.neutral300,
                  width: isSelected ? 2 : 1,
                ),
              ),
              child: Row(
                children: [
                  Container(
                    width: 36, height: 36,
                    decoration: BoxDecoration(
                      color: plan.color.withValues(alpha: 0.12),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Icon(plan.icon, size: 18, color: plan.color),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Text(plan.name, style: ZTypography.titleSmall.copyWith(
                              fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500)),
                            const SizedBox(width: 8),
                            Text(priceLabel, style: ZTypography.labelSmall.copyWith(
                              color: plan.color, fontWeight: FontWeight.w600)),
                          ],
                        ),
                        Text(plan.shortDescription, style: ZTypography.bodySmall.copyWith(
                          color: ZColors.neutral500)),
                      ],
                    ),
                  ),
                  if (isSelected)
                    Icon(Icons.check_circle, color: plan.color, size: 20),
                ],
              ),
            ),
          );
        }),
      ],
    );
  }
}
