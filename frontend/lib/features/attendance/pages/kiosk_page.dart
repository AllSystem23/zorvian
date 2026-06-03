import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../providers/kiosk_provider.dart';

class KioskPage extends ConsumerStatefulWidget {
  const KioskPage({super.key});

  @override
  ConsumerState<KioskPage> createState() => _KioskPageState();
}

class _KioskPageState extends ConsumerState<KioskPage> {
  final _codeCtrl = TextEditingController();
  Timer? _clockTimer;
  DateTime _now = DateTime.now();

  @override
  void initState() {
    super.initState();
    _clockTimer = Timer.periodic(const Duration(seconds: 1), (_) {
      setState(() => _now = DateTime.now());
    });
  }

  @override
  void dispose() {
    _clockTimer?.cancel();
    _codeCtrl.dispose();
    super.dispose();
  }

  void _appendDigit(String d) {
    if (_codeCtrl.text.length < 10) {
      _codeCtrl.text += d;
      _codeCtrl.selection = TextSelection.fromPosition(TextPosition(offset: _codeCtrl.text.length));
      ref.read(kioskProvider.notifier).updateCode(_codeCtrl.text);
    }
  }

  void _clearCode() {
    _codeCtrl.clear();
    ref.read(kioskProvider.notifier).clear();
  }

  void _backspace() {
    if (_codeCtrl.text.isNotEmpty) {
      _codeCtrl.text = _codeCtrl.text.substring(0, _codeCtrl.text.length - 1);
      _codeCtrl.selection = TextSelection.fromPosition(TextPosition(offset: _codeCtrl.text.length));
      ref.read(kioskProvider.notifier).updateCode(_codeCtrl.text);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final kioskState = ref.watch(kioskProvider);
    final isLandscape = MediaQuery.of(context).orientation == Orientation.landscape;

    // Success overlay
    if (kioskState.successType != null) {
      return _SuccessOverlay(
        type: kioskState.successType!,
        employeeName: kioskState.employeeName,
        onDismiss: () {
          ref.read(kioskProvider.notifier).dismissSuccess();
          _clearCode();
        },
      );
    }

    return Scaffold(
      backgroundColor: theme.colorScheme.surface,
      appBar: AppBar(
        title: const Text('Kiosko de Asistencia'),
        actions: [
          IconButton(
            icon: const Icon(Icons.qr_code_scanner),
            tooltip: 'Escaneo QR',
            onPressed: () => context.push('/attendance/qr'),
          ),
          IconButton(
            icon: const Icon(Icons.close),
            onPressed: () => context.pop(),
          ),
        ],
      ),
      body: SafeArea(
        child: isLandscape
            ? _buildLandscapeLayout(theme, kioskState)
            : _buildPortraitLayout(theme, kioskState),
      ),
    );
  }

  Widget _buildClock(ThemeData theme) {
    final timeStr = '${_now.hour.toString().padLeft(2, '0')}:${_now.minute.toString().padLeft(2, '0')}:${_now.second.toString().padLeft(2, '0')}';
    final dateStr = '${_now.day}/${_now.month}/${_now.year}';
    return Column(
      children: [
        Text(timeStr, style: theme.textTheme.displayLarge?.copyWith(
          fontWeight: FontWeight.bold,
          color: theme.colorScheme.primary,
          fontFamily: 'monospace',
        )),
        Text(dateStr, style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.onSurface.withValues(alpha: 0.6))),
      ],
    );
  }

  Widget _buildCodeField(ThemeData theme, KioskState kioskState) {
    return Column(
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          decoration: BoxDecoration(
            color: theme.colorScheme.surfaceContainerHighest,
            borderRadius: BorderRadius.circular(16),
          ),
          child: Text(
            kioskState.employeeCode.isEmpty ? 'Código de empleado' : kioskState.employeeCode,
            style: theme.textTheme.headlineMedium?.copyWith(
              letterSpacing: 6,
              fontWeight: FontWeight.bold,
              color: kioskState.employeeCode.isEmpty
                  ? theme.colorScheme.onSurface.withValues(alpha: 0.3)
                  : theme.colorScheme.onSurface,
            ),
            textAlign: TextAlign.center,
          ),
        ),
        if (kioskState.employeeName != null)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(kioskState.employeeName!, style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.primary)),
          ),
      ],
    );
  }

  Widget _buildNumpad(ThemeData theme) {
    const keys = [
      ['1', '2', '3'],
      ['4', '5', '6'],
      ['7', '8', '9'],
      ['', '0', '⌫'],
    ];
    return Column(
      children: keys.map((row) => Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: row.map((k) {
          if (k.isEmpty) return const SizedBox(width: 80, height: 60);
          if (k == '⌫') {
            return Padding(
              padding: const EdgeInsets.all(4),
              child: SizedBox(
                width: 80,
                height: 60,
                child: ElevatedButton(
                  onPressed: _backspace,
                  style: ElevatedButton.styleFrom(
                    backgroundColor: theme.colorScheme.errorContainer,
                    foregroundColor: theme.colorScheme.onErrorContainer,
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                  ),
                  child: const Icon(Icons.backspace_outlined, size: 24),
                ),
              ),
            );
          }
          return Padding(
            padding: const EdgeInsets.all(4),
            child: SizedBox(
              width: 80,
              height: 60,
              child: ElevatedButton(
                onPressed: () => _appendDigit(k),
                style: ElevatedButton.styleFrom(
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                child: Text(k, style: const TextStyle(fontSize: 24, fontWeight: FontWeight.bold)),
              ),
            ),
          );
        }).toList(),
      )).toList(),
    );
  }

  Widget _buildActionButtons(ThemeData theme, KioskState kioskState) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        SizedBox(
          width: 160,
          height: 56,
          child: FilledButton.icon(
            onPressed: kioskState.loading || kioskState.employeeCode.isEmpty ? null : () => ref.read(kioskProvider.notifier).checkIn(),
            icon: kioskState.loading
                ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.login, size: 24),
            label: const Text('Entrada', style: TextStyle(fontSize: 18)),
            style: FilledButton.styleFrom(
              backgroundColor: Colors.green,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
          ),
        ),
        const SizedBox(width: 16),
        SizedBox(
          width: 160,
          height: 56,
          child: FilledButton.icon(
            onPressed: kioskState.loading || kioskState.employeeCode.isEmpty ? null : () => ref.read(kioskProvider.notifier).checkOut(),
            icon: kioskState.loading
                ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                : const Icon(Icons.logout, size: 24),
            label: const Text('Salida', style: TextStyle(fontSize: 18)),
            style: FilledButton.styleFrom(
              backgroundColor: Colors.orange,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildLandscapeLayout(ThemeData theme, KioskState kioskState) {
    return Row(
      children: [
        Expanded(
          child: Center(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _buildClock(theme),
                if (kioskState.error != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 16),
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 12),
                      decoration: BoxDecoration(
                        color: theme.colorScheme.errorContainer,
                        borderRadius: BorderRadius.circular(12),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.error_outline, color: theme.colorScheme.onErrorContainer),
                          const SizedBox(width: 8),
                          Text(kioskState.error!, style: TextStyle(color: theme.colorScheme.onErrorContainer)),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
          ),
        ),
        Container(
          width: 1,
          color: theme.dividerColor,
        ),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(24),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                _buildCodeField(theme, kioskState),
                const SizedBox(height: 24),
                _buildNumpad(theme),
                const SizedBox(height: 24),
                _buildActionButtons(theme, kioskState),
                const SizedBox(height: 16),
                TextButton.icon(
                  onPressed: () => _clearCode(),
                  icon: const Icon(Icons.refresh),
                  label: const Text('Limpiar'),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildPortraitLayout(ThemeData theme, KioskState kioskState) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          _buildClock(theme),
          const SizedBox(height: 32),
          _buildCodeField(theme, kioskState),
          if (kioskState.error != null)
            Padding(
              padding: const EdgeInsets.only(top: 12),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                decoration: BoxDecoration(
                  color: theme.colorScheme.errorContainer,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.error_outline, size: 18, color: theme.colorScheme.onErrorContainer),
                    const SizedBox(width: 8),
                    Flexible(child: Text(kioskState.error!, style: TextStyle(color: theme.colorScheme.onErrorContainer))),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 24),
          _buildNumpad(theme),
          const SizedBox(height: 24),
          _buildActionButtons(theme, kioskState),
          const SizedBox(height: 12),
          TextButton.icon(
            onPressed: _clearCode,
            icon: const Icon(Icons.refresh),
            label: const Text('Limpiar'),
          ),
        ],
      ),
    );
  }
}

class _SuccessOverlay extends StatefulWidget {
  final String type;
  final String? employeeName;
  final VoidCallback onDismiss;

  const _SuccessOverlay({required this.type, this.employeeName, required this.onDismiss});

  @override
  State<_SuccessOverlay> createState() => _SuccessOverlayState();
}

class _SuccessOverlayState extends State<_SuccessOverlay> with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _scaleAnim;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 400));
    _scaleAnim = CurvedAnimation(parent: _controller, curve: Curves.elasticOut);
    _controller.forward();
    Future.delayed(const Duration(seconds: 2), widget.onDismiss);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final isCheckIn = widget.type == 'check-in';
    return Scaffold(
      backgroundColor: isCheckIn ? Colors.green : Colors.orange,
      body: Center(
        child: ScaleTransition(
          scale: _scaleAnim,
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                isCheckIn ? Icons.check_circle_outline : Icons.logout,
                size: 100,
                color: Colors.white,
              ),
              const SizedBox(height: 24),
              Text(
                isCheckIn ? 'Entrada Registrada' : 'Salida Registrada',
                style: const TextStyle(fontSize: 36, color: Colors.white, fontWeight: FontWeight.bold),
              ),
              if (widget.employeeName != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: Text(
                    widget.employeeName!,
                    style: const TextStyle(fontSize: 22, color: Colors.white70),
                  ),
                ),
            ],
          ),
        ),
      ),
    );
  }
}
