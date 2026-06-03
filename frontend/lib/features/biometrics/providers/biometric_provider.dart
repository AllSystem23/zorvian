import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/services/biometric_service.dart';

final biometricServiceProvider = Provider<BiometricService>((_) => BiometricService());

class BiometricState {
  final bool isAvailable;
  final bool isEnabled;
  final bool isUnlocked;
  final bool loading;

  const BiometricState({
    this.isAvailable = false,
    this.isEnabled = false,
    this.isUnlocked = false,
    this.loading = false,
  });

  BiometricState copyWith({
    bool? isAvailable,
    bool? isEnabled,
    bool? isUnlocked,
    bool? loading,
  }) => BiometricState(
    isAvailable: isAvailable ?? this.isAvailable,
    isEnabled: isEnabled ?? this.isEnabled,
    isUnlocked: isUnlocked ?? this.isUnlocked,
    loading: loading ?? this.loading,
  );
}

class BiometricNotifier extends Notifier<BiometricState> {
  @override
  BiometricState build() => const BiometricState();

  Future<void> init() async {
    final available = await ref.read(biometricServiceProvider).isAvailable();
    final enabled = await ref.read(secureStorageProvider).isBiometricEnabled();
    state = state.copyWith(isAvailable: available, isEnabled: enabled, isUnlocked: !enabled);
  }

  Future<bool> tryUnlock() async {
    if (!state.isEnabled) return true;
    final success = await ref.read(biometricServiceProvider).authenticate(reason: 'Desbloquear Zorvian ERP');
    if (success) {
      state = state.copyWith(isUnlocked: true);
    }
    return success;
  }

  Future<void> lockNow() {
    state = state.copyWith(isUnlocked: false);
    return Future.value();
  }

  Future<void> toggleEnabled() async {
    state = state.copyWith(loading: true);
    try {
      final newEnabled = !state.isEnabled;
      final storage = ref.read(secureStorageProvider);
      await storage.setBiometricEnabled(newEnabled);

      if (newEnabled) {
        final token = await storage.getAccessToken();
        final deviceId = token?.substring(0, 12) ?? 'unknown';
        await ref.read(dioClientProvider).post('biometrics/register', data: {
          'deviceId': deviceId,
          'deviceName': 'Dispositivo actual',
        });
      }

      state = state.copyWith(isEnabled: newEnabled, isUnlocked: !newEnabled, loading: false);
    } catch (_) {
      state = state.copyWith(loading: false);
      rethrow;
    }
  }
}

final biometricProvider = NotifierProvider<BiometricNotifier, BiometricState>(
  BiometricNotifier.new,
);
