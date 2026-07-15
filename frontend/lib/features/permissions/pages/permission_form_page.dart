import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:file_picker/file_picker.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/ds/ds.dart';
import '../providers/permission_provider.dart';

class PermissionFormPage extends ConsumerStatefulWidget {
  const PermissionFormPage({super.key});

  @override
  ConsumerState<PermissionFormPage> createState() => _PermissionFormPageState();
}

class _PermissionFormPageState extends ConsumerState<PermissionFormPage> {
  final _formKey = GlobalKey<FormState>();
  String? _selectedTypeId;
  PermissionType? _selectedType;
  DateTime? _startDate;
  DateTime? _endDate;
  final _reasonCtrl = TextEditingController();
  PlatformFile? _selectedFile;
  bool _loading = false;
  bool _uploading = false;
  String? _error;
  String? _uploadedUrl;
  String? _uploadedFileName;

  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(permissionProvider.notifier).loadTypes());
  }

  @override
  void dispose() {
    _reasonCtrl.dispose();
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

  Future<void> _pickFile() async {
    final result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'jpg', 'jpeg', 'png'],
    );
    if (result == null || result.files.isEmpty) return;
    setState(() => _selectedFile = result.files.first);
  }

  Future<void> _uploadFile() async {
    if (_selectedFile == null) return;
    setState(() => _uploading = true);

    try {
      final dio = ref.read(dioClientProvider);
      final formData = FormData.fromMap({
        'file': await MultipartFile.fromFile(
          _selectedFile!.path!,
          filename: _selectedFile!.name,
        ),
      });
      final r = await dio.post('permissions/upload', data: formData);
      setState(() {
        _uploadedUrl = r.data['url'] as String?;
        _uploadedFileName = r.data['fileName'] as String?;
        _uploading = false;
      });
    } catch (e) {
      setState(() {
        _uploading = false;
        _error = 'Error al subir archivo';
      });
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedTypeId == null) {
      setState(() => _error = 'Seleccione un tipo de permiso');
      return;
    }
    if (_startDate == null || _endDate == null) {
      setState(() => _error = 'Seleccione fecha inicio y fin');
      return;
    }
    if (_selectedType!.requiresAttachment && _uploadedUrl == null) {
      setState(() => _error = 'Debe adjuntar un documento');
      return;
    }

    if (_selectedFile != null && _uploadedUrl == null) {
      await _uploadFile();
      if (_uploadedUrl == null) return;
    }

    setState(() { _loading = true; _error = null; });

    try {
      final dio = ref.read(dioClientProvider);
      final body = {
        'leaveTypeId': _selectedTypeId,
        'startDate': _startDate!.toIso8601String().substring(0, 10),
        'endDate': _endDate!.toIso8601String().substring(0, 10),
        'reason': _reasonCtrl.text.trim(),
        if (_uploadedUrl != null) 'supportingDocumentUrl': _uploadedUrl,
        if (_uploadedFileName != null) 'supportingDocumentFileName': _uploadedFileName,
      };
      await dio.post('permissions', data: body);
      if (mounted) context.pop(true);
    } catch (e) {
      final msg = e is DioException
          ? (e.response?.data?['error'] as String? ?? 'Error al crear solicitud')
          : 'Error al crear solicitud';
      setState(() => _error = msg);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final state = ref.watch(permissionProvider);
    final types = state.types;

    return Scaffold(
      body: SingleChildScrollView(
        padding: EdgeInsets.all(MediaQuery.of(context).size.width < 576 ? 12 : MediaQuery.of(context).size.width < 992 ? 16 : 24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
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
              DropdownButtonFormField<String>(
                initialValue: _selectedTypeId,
                decoration: const InputDecoration(
                  labelText: 'Tipo de permiso',
                  prefixIcon: Icon(Icons.category),
                ),
                items: types.map((t) => DropdownMenuItem(
                  value: t.id,
                  child: Text('${t.name}${t.isPaid ? '' : ' (Sin goce)'}'),
                )).toList(),
                onChanged: (v) {
                  setState(() {
                    _selectedTypeId = v;
                    _selectedType = v != null ? types.firstWhere((t) => t.id == v) : null;
                  });
                },
              ),
              if (_selectedType?.description != null)
                Padding(
                  padding: const EdgeInsets.only(top: 4, left: 12),
                  child: Text(
                    _selectedType!.description!,
                    style: theme.textTheme.bodySmall?.copyWith(color: Colors.grey),
                  ),
                ),
              const SizedBox(height: 16),
              ListTile(
                leading: const Icon(Icons.calendar_today),
                title: Text(ZFormatters.dateOrNull(_startDate, fallback: 'Fecha inicio')),
                trailing: const Icon(Icons.edit),
                onTap: () => _pickDate(true),
              ),
              ListTile(
                leading: const Icon(Icons.calendar_today),
                title: Text(ZFormatters.dateOrNull(_endDate, fallback: 'Fecha fin')),
                trailing: const Icon(Icons.edit),
                onTap: () => _pickDate(false),
              ),
              if (_totalDays > 0)
                Padding(
                  padding: const EdgeInsets.symmetric(vertical: 4, horizontal: 16),
                  child: Text('Total: $_totalDays días', style: theme.textTheme.bodyLarge),
                ),
              if (_selectedType?.maxDaysPerRequest != null && _totalDays > _selectedType!.maxDaysPerRequest!)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Text(
                    'Máximo ${_selectedType!.maxDaysPerRequest} días para este tipo',
                    style: TextStyle(color: theme.colorScheme.error, fontSize: 12),
                  ),
                ),
              const SizedBox(height: 16),
              TextFormField(
                controller: _reasonCtrl,
                decoration: const InputDecoration(
                  labelText: 'Motivo',
                  prefixIcon: Icon(Icons.description),
                ),
                maxLines: 3,
              ),
              if (_selectedType?.requiresAttachment == true || _selectedFile != null) ...[
                const SizedBox(height: 16),
                if (_selectedFile == null)
                  OutlinedButton.icon(
                    onPressed: _pickFile,
                    icon: const Icon(Icons.attach_file),
                    label: const Text('Adjuntar documento (PDF/JPG/PNG)'),
                  )
                else
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _selectedFile!.name,
                          overflow: TextOverflow.ellipsis,
                          style: theme.textTheme.bodySmall,
                        ),
                      ),
                      if (_uploading)
                        const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                      else if (_uploadedUrl == null)
                        ZButton(
                          text: 'Subir',
                          onPressed: _uploadFile,
                          type: ZButtonType.ghost,
                          fullWidth: false,
                        )
                      else
                        const Icon(Icons.check_circle, color: Colors.green),
                    ],
                  ),
              ],
              const SizedBox(height: 24),
              ZButton(
                text: 'Solicitar permiso',
                onPressed: _submit,
                isLoading: _loading,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
