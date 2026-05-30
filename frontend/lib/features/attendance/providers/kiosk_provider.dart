import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final kioskLookupProvider = FutureProvider.autoDispose.family<List<Map<String, dynamic>>, String>((ref, code) async {
  if (code.length < 2) return [];
  final dio = ref.read(dioClientProvider);
  final res = await dio.get('/kiosk/lookup', params: {'code': code, 'max': 8});
  return (res.data as List<dynamic>).cast<Map<String, dynamic>>();
});

class KioskState {
  final String employeeCode;
  final String? employeeName;
  final String? error;
  final bool loading;
  final String? successType; // 'check-in' | 'check-out' | null

  const KioskState({
    this.employeeCode = '',
    this.employeeName,
    this.error,
    this.loading = false,
    this.successType,
  });

  KioskState copyWith({
    String? employeeCode,
    String? employeeName,
    String? error,
    bool? loading,
    String? successType,
  }) => KioskState(
    employeeCode: employeeCode ?? this.employeeCode,
    employeeName: employeeName ?? this.employeeName,
    error: error,
    loading: loading ?? this.loading,
    successType: successType,
  );
}

class KioskNotifier extends Notifier<KioskState> {
  @override
  KioskState build() => const KioskState();

  void updateCode(String code) {
    state = state.copyWith(employeeCode: code.toUpperCase(), error: null, successType: null);
  }

  void clear() {
    state = const KioskState();
  }

  Future<bool> checkIn() async {
    return _performAction('check-in');
  }

  Future<bool> checkOut() async {
    return _performAction('check-out');
  }

  Future<bool> _performAction(String action) async {
    if (state.employeeCode.isEmpty) {
      state = state.copyWith(error: 'Ingrese un código de empleado');
      return false;
    }

    state = state.copyWith(loading: true, error: null, successType: null);
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('/kiosk/$action', data: {
        'employeeCode': state.employeeCode,
      });

      final name = state.employeeName;
      state = state.copyWith(loading: false, successType: action, error: null, employeeName: name);
      return true;
    } catch (e) {
      final msg = e.toString().contains('Empleado no encontrado')
          ? 'Empleado no encontrado'
          : e.toString().contains('Ya existe')
              ? 'Ya registró asistencia hoy'
              : 'Error al registrar';
      state = state.copyWith(loading: false, error: msg, successType: null);
      return false;
    }
  }

  void dismissSuccess() {
    state = state.copyWith(successType: null, employeeCode: '', employeeName: null);
  }
}

final kioskProvider = NotifierProvider<KioskNotifier, KioskState>(
  KioskNotifier.new,
);
