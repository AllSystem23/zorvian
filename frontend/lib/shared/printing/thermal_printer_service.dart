import 'package:flutter/foundation.dart';
import 'escpos_builder.dart';
import 'platform/thermal_platform.dart';
import 'platform/download_helper.dart' as dh;
import 'thermal_template.dart';

enum PrinterConnectionState { disconnected, connecting, connected, unsupported }

class ThermalPrinterService {
  PrinterConnectionState _state = PrinterConnectionState.disconnected;
  PrinterConnectionState get state => _state;
  bool get isConnected => _state == PrinterConnectionState.connected;

  final ValueNotifier<PrinterConnectionState> stateNotifier =
      ValueNotifier(PrinterConnectionState.disconnected);

  Future<bool> connectUsb() async {
    if (_state == PrinterConnectionState.connecting) return false;
    _state = PrinterConnectionState.connecting;
    stateNotifier.value = _state;

    try {
      final ok = await thermalPlatformConnectUsb();
      _state = ok ? PrinterConnectionState.connected : PrinterConnectionState.unsupported;
      stateNotifier.value = _state;
      return ok;
    } catch (_) {
      _state = PrinterConnectionState.disconnected;
      stateNotifier.value = _state;
      return false;
    }
  }

  Future<void> disconnect() async {
    await thermalPlatformDisconnectUsb();
    _state = PrinterConnectionState.disconnected;
    stateNotifier.value = _state;
  }

  Future<bool> printReceipt(Map<String, dynamic> data) async {
    final bytes = EscPosBuilder().buildReceipt(data).buildUint8();
    
    if (kIsWeb) {
      final usbOk = await thermalPlatformWriteUsb(bytes);
      if (usbOk) return true;
      
      final html = _buildThermalHtmlFromMap(data);
      dh.printHtml(html);
      return true;
    }
    
    return thermalPlatformWriteUsb(bytes);
  }

  static void printViaBrowser(String htmlContent) {
    dh.printHtml(htmlContent);
  }

  String _buildThermalHtmlFromMap(Map<String, dynamic> data) {
    final company = <String, dynamic>{
      'legalName': data['companyName'],
      'taxId': data['taxId'],
      'address': data['address'],
      'phone': data['phone'],
    };
    
    return saleThermalHtml(
      company: company,
      invoiceNumber: data['invoiceNumber'] as String? ?? '',
      date: data['date'] as String? ?? '',
      clientName: data['clientName'] as String? ?? '',
      saleType: data['saleType'] as String? ?? 'cash',
      subtotal: (data['subtotal'] as num?)?.toDouble() ?? 0,
      discount: (data['discount'] as num?)?.toDouble() ?? 0,
      tax: (data['tax'] as num?)?.toDouble() ?? 0,
      total: (data['total'] as num?)?.toDouble() ?? 0,
      paidAmount: (data['paidAmount'] as num?)?.toDouble() ?? 0,
      balance: (data['balance'] as num?)?.toDouble() ?? 0,
      status: data['status'] as String? ?? '',
      notes: data['notes'] as String?,
      items: (data['items'] as List?)?.cast<Map<String, dynamic>>() ?? [],
    );
  }
}

final thermalPrinterService = ThermalPrinterService();
