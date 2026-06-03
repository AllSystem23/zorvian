import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/biometric_provider.dart';

class BiometricUnlockPage extends ConsumerStatefulWidget {
  final Widget child;
  const BiometricUnlockPage({super.key, required this.child});

  @override
  ConsumerState<BiometricUnlockPage> createState() => _BiometricUnlockPageState();
}

class _BiometricUnlockPageState extends ConsumerState<BiometricUnlockPage> with WidgetsBindingObserver {
  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _tryUnlock();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      final bioState = ref.read(biometricProvider);
      if (bioState.isEnabled && !bioState.isUnlocked) {
        _tryUnlock();
      }
    }
    if (state == AppLifecycleState.paused) {
      ref.read(biometricProvider.notifier).lockNow();
    }
  }

  Future<void> _tryUnlock() async {
    await ref.read(biometricProvider.notifier).tryUnlock();
  }

  @override
  Widget build(BuildContext context) {
    final bioState = ref.watch(biometricProvider);
    if (bioState.isEnabled && !bioState.isUnlocked) {
      return Scaffold(
        body: Container(
          width: double.infinity,
          height: double.infinity,
          decoration: BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topCenter,
              end: Alignment.bottomCenter,
              colors: [Theme.of(context).colorScheme.primary, Theme.of(context).colorScheme.surface],
            ),
          ),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.fingerprint, size: 80, color: Theme.of(context).colorScheme.onPrimary),
              const SizedBox(height: 24),
              Text(
                'Zorvian ERP',
                style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                  color: Theme.of(context).colorScheme.onPrimary,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Toque para desbloquear',
                style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                  color: Theme.of(context).colorScheme.onPrimary.withValues(alpha: 0.8),
                ),
              ),
              const SizedBox(height: 32),
              FilledButton.icon(
                onPressed: _tryUnlock,
                icon: const Icon(Icons.fingerprint),
                label: const Text('Desbloquear'),
                style: FilledButton.styleFrom(
                  backgroundColor: Theme.of(context).colorScheme.onPrimary,
                  foregroundColor: Theme.of(context).colorScheme.primary,
                  padding: const EdgeInsets.symmetric(horizontal: 32, vertical: 14),
                ),
              ),
            ],
          ),
        ),
      );
    }
    return widget.child;
  }
}
