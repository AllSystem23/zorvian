import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../providers/vacation_provider.dart';

class VacationFormPage extends ConsumerStatefulWidget {
  const VacationFormPage({super.key});

  @override
  ConsumerState<VacationFormPage> createState() => _VacationFormPageState();
}

class _VacationFormPageState extends ConsumerState<VacationFormPage> {
  final _formKey = GlobalKey<FormState>();
  DateTime? _startDate;
  DateTime? _endDate;
  final _commentsCtrl = TextEditingController();
  bool _loading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(vacationProvider.notifier).loadBalance());
  }

  @override
  void dispose() {
    _commentsCtrl.dispose();
    super.dispose();
  }

  int get _totalDays {
    if (_startDate == null || _endDate == null) return 0;
    return _endDate!.difference(_startDate!).inDays + 1;
  }

  Future<void> _pickDate(bool isStart) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: isStart ? (now.add(const Duration(days: 1))) : (_startDate ?? now),
      firstDate: isStart ? now.add(const Duration(days: 1)) : (_startDate ?? now),
      lastDate: now.add(const Duration(days: 365)),
    );
    if (picked == null) return;
    setState(() {
      if (isStart) {
        _startDate = picked;
        if (_endDate != null && _endDate!.isBefore(picked)) _endDate = picked;
      } else {
        _endDate = picked;
      }
    });
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_startDate == null || _endDate == null) {
      setState(() => _error = 'Seleccione fecha inicio y fin');
      return;
    }

    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'startDate': _startDate!.toIso8601String().substring(0, 10),
        'endDate': _endDate!.toIso8601String().substring(0, 10),
        'comments': _commentsCtrl.text.trim(),
      };
      await dio.post('vacations', data: body);
      if (mounted) context.pop(true);
    } catch (e) {
      setState(() => _error = 'Error al crear solicitud');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(vacationProvider);
    final balance = state.balance;

    return Scaffold(
      appBar: AppBar(title: const Text('Solicitar vacaciones')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (state.error != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Row(
                    children: [
                      Icon(Icons.error_outline, size: 18, color: theme.colorScheme.error),
                      const SizedBox(width: 8),
                      Expanded(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error, fontSize: 13))),
                    ],
                  ),
                ),
              if (balance != null)
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      children: [
                        Text('Saldo disponible', style: theme.textTheme.titleMedium),
                        const SizedBox(height: 12),
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceAround,
                          children: [
                            _balanceChip('Acumulados', balance.accruedDays, Colors.blue),
                            _balanceChip('Tomados', balance.takenDays, Colors.red),
                            _balanceChip('Pendientes', balance.pendingDays, Colors.orange),
                            _balanceChip('Disponibles', balance.availableDays, Colors.green),
                          ],
                        ),
                      ],
                    ),
                  ),
                ),
              const SizedBox(height: 16),
              if (_error != null)
                Container(
                  padding: const EdgeInsets.all(12),
                  margin: const EdgeInsets.only(bottom: 16),
                  decoration: BoxDecoration(
                    color: theme.colorScheme.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
                ),
              ListTile(
                leading: const Icon(Icons.calendar_today),
                title: Text(_startDate != null
                    ? '${_startDate!.day}/${_startDate!.month}/${_startDate!.year}'
                    : 'Fecha inicio'),
                trailing: const Icon(Icons.edit),
                onTap: () => _pickDate(true),
              ),
              ListTile(
                leading: const Icon(Icons.calendar_today),
                title: Text(_endDate != null
                    ? '${_endDate!.day}/${_endDate!.month}/${_endDate!.year}'
                    : 'Fecha fin'),
                trailing: const Icon(Icons.edit),
                onTap: () => _pickDate(false),
              ),
              if (_totalDays > 0)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Text('Total: $_totalDays días', style: theme.textTheme.bodyLarge),
                ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _commentsCtrl,
                decoration: const InputDecoration(
                  labelText: 'Comentarios',
                  prefixIcon: Icon(Icons.comment),
                ),
                maxLines: 3,
              ),
              const SizedBox(height: 24),
              ElevatedButton(
                onPressed: _loading ? null : _submit,
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Solicitar vacaciones'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _balanceChip(String label, double value, Color color) {
    return Column(
      children: [
        Text(value.toStringAsFixed(1), style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold, color: color)),
        Text(label, style: const TextStyle(fontSize: 11, color: Colors.grey)),
      ],
    );
  }
}
