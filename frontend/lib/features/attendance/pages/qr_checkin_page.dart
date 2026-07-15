import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import '../providers/attendance_provider.dart';

class QRCheckInPage extends ConsumerStatefulWidget {
  const QRCheckInPage({super.key});

  @override
  ConsumerState<QRCheckInPage> createState() => _QRCheckInPageState();
}

class _QRCheckInPageState extends ConsumerState<QRCheckInPage> {
  MobileScannerController? _scanner;
  bool _processing = false;

  @override
  void dispose() {
    _scanner?.dispose();
    super.dispose();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_processing) return;
    final barcode = capture.barcodes.firstOrNull;
    if (barcode?.rawValue == null) return;
    _processCode(barcode!.rawValue!);
  }

  Future<void> _processCode(String code) async {
    setState(() => _processing = true);
    try {
      final ok = await ref.read(attendanceProvider.notifier).checkInQR(code);
      if (mounted) {
        if (ok) {
          ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
            content: Text('Asistencia registrada correctamente'),
            backgroundColor: Colors.green,
          ));
          Navigator.pop(context);
        } else {
          final err = ref.read(attendanceProvider).error?.toString() ?? 'Error al registrar';
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(err), backgroundColor: Colors.red));
          setState(() => _processing = false);
        }
      }
    } catch (_) {
      setState(() => _processing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Stack(
        children: [
          MobileScanner(
            controller: _scanner ??= MobileScannerController(),
            onDetect: _onDetect,
          ),
          if (_processing)
            Container(
              color: Colors.black54,
              child: const Center(child: CircularProgressIndicator(color: Colors.white)),
            ),
          // Overlay guide
          Center(
            child: Container(
              width: 250,
              height: 250,
              decoration: BoxDecoration(
                border: Border.all(color: Colors.white, width: 3),
                borderRadius: BorderRadius.circular(16),
              ),
            ),
          ),
          Positioned(
            bottom: 40,
            left: 0,
            right: 0,
            child: Text(
              'Apunta al código QR para marcar entrada',
              textAlign: TextAlign.center,
              style: TextStyle(color: Colors.white.withValues(alpha: 0.8), fontSize: 14),
            ),
          ),
        ],
      ),
    );
  }
}
