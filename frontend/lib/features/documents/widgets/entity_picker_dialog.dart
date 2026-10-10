import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../shared/ds/ds.dart';

class EntityPickResult {
  final String id;
  final String displayName;
  final String? subtitle;

  const EntityPickResult({
    required this.id,
    required this.displayName,
    this.subtitle,
  });
}

/// Abre un selector buscable de entidades (trabajador / venta / cliente).
/// Devuelve el [EntityPickResult] elegido o null si se cancela.
Future<EntityPickResult?> showEntityPickerDialog(
  BuildContext context, {
  required String entityType,
}) {
  return showModalBottomSheet<EntityPickResult>(
    context: context,
    isScrollControlled: true,
    useSafeArea: true,
    backgroundColor: Theme.of(context).brightness == Brightness.dark
        ? ZColors.darkBackground
        : ZColors.neutral50,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
    ),
    builder: (_) => EntityPickerSheet(entityType: entityType),
  );
}

class EntityPickerSheet extends ConsumerStatefulWidget {
  final String entityType;

  const EntityPickerSheet({super.key, required this.entityType});

  @override
  ConsumerState<EntityPickerSheet> createState() => _EntityPickerSheetState();
}

class _EntityPickerSheetState extends ConsumerState<EntityPickerSheet> {
  bool _loading = false;
  String? _error;
  List<Map<String, dynamic>> _items = [];
  int _requestId = 0;

  String get _title => switch (widget.entityType) {
        'employee' => 'Buscar trabajador',
        'sale' => 'Buscar venta',
        'client' => 'Buscar cliente',
        _ => 'Buscar entidad',
      };

  String get _searchHint => switch (widget.entityType) {
        'employee' => 'Nombre, código o correo...',
        'sale' => 'N° de factura o cliente...',
        'client' => 'Nombre o cédula...',
        _ => 'Buscar...',
      };

  String get _endpoint => switch (widget.entityType) {
        'employee' => 'employees',
        'sale' => 'sales',
        'client' => 'clients',
        _ => 'employees',
      };

  @override
  void initState() {
    super.initState();
    _search('');
  }

  Future<void> _search(String query) async {
    final requestId = ++_requestId;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get(_endpoint, params: {
        'page': 1,
        'pageSize': 20,
        if (query.trim().isNotEmpty) 'search': query.trim(),
      });
      if (!mounted || requestId != _requestId) return;
      final data = response.data as Map<String, dynamic>;
      final items = (data['items'] as List? ?? [])
          .whereType<Map<String, dynamic>>()
          .toList();
      setState(() {
        _items = items;
        _loading = false;
      });
    } catch (e) {
      if (!mounted || requestId != _requestId) return;
      setState(() {
        _loading = false;
        _error = 'No se pudo cargar. Intenta de nuevo.';
      });
    }
  }

  EntityPickResult? _toResult(Map<String, dynamic> j) {
    final id = j['id']?.toString();
    if (id == null || id.isEmpty) return null;
    switch (widget.entityType) {
      case 'employee':
        final name = (j['fullName'] ?? '').toString();
        if (name.isEmpty) return null;
        final extra = [
          (j['position'] ?? '').toString(),
          (j['employeeCode'] ?? '').toString(),
        ].where((s) => s.isNotEmpty).join(' • ');
        return EntityPickResult(id: id, displayName: name, subtitle: extra.isEmpty ? null : extra);
      case 'sale':
        final number = (j['invoiceNumber'] ?? '').toString();
        if (number.isEmpty) return null;
        final client = (j['clientName'] ?? '').toString();
        final currency = (j['currencyCode'] ?? 'C\$').toString();
        final total = (j['total'] as num?)?.toStringAsFixed(2) ?? '0';
        final extra = [
          if (client.isNotEmpty) client,
          '$currency $total',
        ].join(' • ');
        return EntityPickResult(id: id, displayName: number, subtitle: extra.isEmpty ? null : extra);
      case 'client':
        final name = (j['fullName'] ?? '').toString();
        if (name.isEmpty) return null;
        final doc = (j['identificationNumber'] ?? j['documentNumber'] ?? '').toString();
        final code = (j['code'] ?? '').toString();
        final extra = [doc, code].where((s) => s.isNotEmpty).join(' • ');
        return EntityPickResult(id: id, displayName: name, subtitle: extra.isEmpty ? null : extra);
      default:
        return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final results = _items.map(_toResult).whereType<EntityPickResult>().toList();

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.75,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      builder: (context, scrollController) {
        return Column(
          children: [
            Container(
              margin: const EdgeInsets.only(top: 12),
              width: 40,
              height: 4,
              decoration: BoxDecoration(
                color: isDark ? ZColors.darkBorder : ZColors.neutral200,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(20, 16, 12, 8),
              child: Row(
                children: [
                  Expanded(
                    child: Text(_title, style: ZTypography.titleMedium),
                  ),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.of(context).pop(),
                  ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: ZSearchField(
                hintText: _searchHint,
                autofocus: true,
                onChanged: _search,
              ),
            ),
            const SizedBox(height: 8),
            Expanded(
              child: _loading
                  ? const Center(child: CircularProgressIndicator())
                  : _error != null
                      ? ZEmptyState(
                          icon: Icons.error_outline,
                          title: 'Error',
                          subtitle: _error!,
                          actionLabel: 'Reintentar',
                          onAction: () => _search(''),
                        )
                      : results.isEmpty
                          ? ZEmptyState(
                              icon: Icons.search_off,
                              title: 'Sin resultados',
                              subtitle: 'No se encontraron coincidencias para tu búsqueda.',
                            )
                          : ListView.separated(
                              controller: scrollController,
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                              itemCount: results.length,
                              separatorBuilder: (_, _) => const Divider(height: 1),
                              itemBuilder: (context, index) {
                                final r = results[index];
                                return ListTile(
                                  leading: CircleAvatar(
                                    backgroundColor:
                                        ZColors.brandAccent.withValues(alpha: 0.1),
                                    child: Icon(
                                      switch (widget.entityType) {
                                        'employee' => Icons.person_outline,
                                        'sale' => Icons.receipt_outlined,
                                        _ => Icons.people_outline,
                                      },
                                      size: 20,
                                      color: ZColors.brandAccent,
                                    ),
                                  ),
                                  title: Text(r.displayName,
                                      style: ZTypography.bodyMedium
                                          .copyWith(fontWeight: FontWeight.w600)),
                                  subtitle: r.subtitle != null
                                      ? Text(r.subtitle!,
                                          style: ZTypography.labelSmall)
                                      : null,
                                  trailing: const Icon(Icons.chevron_right,
                                      size: 20, color: ZColors.neutral400),
                                  onTap: () => Navigator.of(context).pop(r),
                                );
                              },
                            ),
            ),
          ],
        );
      },
    );
  }
}
