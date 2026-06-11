import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/ds.dart';
import '../providers/goal_provider.dart';
import '../../../core/entities/goal_definition.dart';
import '../data/goal_repository.dart';

class GoalsConfigScreen extends ConsumerStatefulWidget {
  const GoalsConfigScreen({super.key});

  @override
  ConsumerState<GoalsConfigScreen> createState() => _GoalsConfigScreenState();
}

class _GoalsConfigScreenState extends ConsumerState<GoalsConfigScreen> {
  final _nameController = TextEditingController();
  final _descriptionController = TextEditingController();
  String _goalType = 'Ventas';
  final String _metricType = 'amount';
  String _frequency = 'monthly';
  bool _isSaving = false;

  @override
  void dispose() {
    _nameController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _saveGoal() async {
    if (_nameController.text.isEmpty) return;

    setState(() => _isSaving = true);
    try {
      final repository = ref.read(goalRepositoryProvider);
      final newGoal = GoalDefinition(
        id: '', // Backend genera el GUID
        name: _nameController.text,
        description: _descriptionController.text,
        goalType: _goalType,
        metricType: _metricType,
        frequency: _frequency,
        dataSource: 'Manual', // Por defecto para esta versión
      );
      
      // En un escenario real, tendríamos un endpoint POST en el backend
      // que ya verifiqué que existe: [HttpPost("definitions")]
      await repository.createDefinition(newGoal);

      if (mounted) {
        ZToast.show(context, 'Meta creada con éxito');
        ref.invalidate(goalDefinitionsProvider);
        _nameController.clear();
        _descriptionController.clear();
      }
    } catch (e) {
      if (mounted) ZToast.show(context, 'Error al crear meta', type: ZToastType.error);
    } finally {
      if (mounted) setState(() => _isSaving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final definitionsAsync = ref.watch(goalDefinitionsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('Configurador de Metas')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Nueva Definición de Meta', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            ZCard(
              child: Column(
                children: [
                  ZTextField(
                    label: 'Nombre de la Meta',
                    controller: _nameController,
                    hint: 'Ej: Ventas Mensuales',
                  ),
                  const SizedBox(height: ZSpacing.md),
                  ZTextField(
                    label: 'Descripción',
                    controller: _descriptionController,
                    hint: 'Opcional',
                    maxLines: 2,
                  ),
                  const SizedBox(height: ZSpacing.md),
                  Row(
                    children: [
                      Expanded(
                        child: ZSelect<String>(
                          label: 'Tipo de Meta',
                          value: _goalType,
                          items: const ['Ventas', 'Nuevos Clientes', 'Servicio', 'Calidad'].map((o) => DropdownMenuItem(value: o, child: Text(o))).toList(),
                          onChanged: (val) => setState(() => _goalType = val!),
                        ),
                      ),
                      const SizedBox(width: ZSpacing.md),
                      Expanded(
                        child: ZSelect<String>(
                          label: 'Frecuencia',
                          value: _frequency,
                          items: const ['monthly', 'quarterly', 'annual'].map((o) => DropdownMenuItem(value: o, child: Text(o))).toList(),
                          onChanged: (val) => setState(() => _frequency = val!),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: ZSpacing.lg),
                  ZButton(
                    text: 'Guardar Definición',
                    onPressed: _isSaving ? () {} : _saveGoal,
                    isLoading: _isSaving,
                    fullWidth: true,
                  ),
                ],
              ),
            ),
            const SizedBox(height: ZSpacing.xl),
            Text('Metas Existentes', style: ZTypography.titleLarge),
            const SizedBox(height: ZSpacing.md),
            definitionsAsync.when(
              data: (definitions) => definitions.isEmpty 
                ? const ZEmptyState(icon: Icons.flag_outlined, title: 'No hay metas definidas', subtitle: 'Crea la primera meta arriba.')
                : ListView.separated(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    itemCount: definitions.length,
                    separatorBuilder: (_, _) => const SizedBox(height: ZSpacing.md),
                    itemBuilder: (context, index) {
                      final def = definitions[index];
                      return ZCard(
                        child: ListTile(
                          title: Text(def.name, style: ZTypography.titleMedium),
                          subtitle: Text('${def.goalType} - ${def.frequency}'),
                          trailing: ZBadge(text: def.status, type: def.status == 'active' ? ZBadgeType.success : ZBadgeType.neutral),
                        ),
                      );
                    },
                  ),
              loading: () => const ZSkeleton(height: 200),
              error: (err, _) => ZAlertCard(
                message: 'No se pudieron cargar las metas: $err',
                severity: 'high',
              ),
            ),
          ],
        ),
      ),
    );
  }
}
