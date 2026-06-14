import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../features/bi/providers/bi_provider.dart';
import '../../../shared/ds/ds.dart';

class ExpenseClassifierSection extends ConsumerStatefulWidget {
  const ExpenseClassifierSection({super.key});

  @override
  ConsumerState<ExpenseClassifierSection> createState() => _ExpenseClassifierSectionState();
}

class _ExpenseClassifierSectionState extends ConsumerState<ExpenseClassifierSection> {
  final _controller = TextEditingController();
  Timer? _debounce;

  @override
  void dispose() {
    _controller.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    if (value.trim().isEmpty) return;
    _debounce = Timer(const Duration(milliseconds: 500), () {
      ref.read(expenseClassificationProvider((description: value.trim(), amount: 0)).future);
    });
  }

  @override
  Widget build(BuildContext context) {
    final palette = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('Clasificador de Gastos (IA)', style: Theme.of(context).textTheme.titleMedium),
        const SizedBox(height: 8),
        TextField(
          controller: _controller,
          decoration: InputDecoration(
            hintText: 'Describe el gasto...',
            prefixIcon: const Icon(Icons.search),
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
            filled: true,
            fillColor: palette.surfaceContainerHighest,
          ),
          onChanged: _onChanged,
        ),
        const SizedBox(height: 8),
        _buildResult(palette),
      ],
    );
  }

  Widget _buildResult(ColorScheme palette) {
    final description = _controller.text.trim();
    if (description.isEmpty) return const SizedBox.shrink();

    return Container(
      constraints: const BoxConstraints(minHeight: 60),
      child: ref.watch(expenseClassificationProvider((description: description, amount: 0))).when(
        loading: () => const Center(child: CircularProgressIndicator(strokeWidth: 2)),
        error: (e, _) => ZCard(
          padding: const EdgeInsets.all(12),
          child: Row(
            children: [
              Icon(Icons.info_outline, color: palette.onSurfaceVariant, size: 18),
              const SizedBox(width: 8),
              Expanded(child: Text('Modelo en entrenamiento. Los resultados aparecerán tras el primer entrenamiento.', style: Theme.of(context).textTheme.bodySmall)),
            ],
          ),
        ),
        data: (resp) {
          if (resp.suggestions.isEmpty) return const SizedBox.shrink();
          return Column(
            children: resp.suggestions.map((s) => ZCard(
              margin: const EdgeInsets.only(bottom: 6),
              padding: const EdgeInsets.all(12),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('${s.accountCode} — ${s.accountName}', style: Theme.of(context).textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
                        Text('Confianza: ${(s.confidence * 100).toStringAsFixed(0)}%', style: Theme.of(context).textTheme.bodySmall?.copyWith(color: palette.onSurfaceVariant)),
                      ],
                    ),
                  ),
                  Icon(Icons.check_circle_outline, color: s.confidence > 0.7 ? palette.primary : palette.onSurfaceVariant),
                ],
              ),
            )).toList(),
          );
        },
      ),
    );
  }
}
