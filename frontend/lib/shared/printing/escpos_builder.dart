import 'dart:typed_data';

class EscPosBuilder {
  final List<int> _buffer = [];

  EscPosBuilder init() {
    _buffer.addAll([0x1B, 0x40]);
    return this;
  }

  EscPosBuilder feed(int lines) {
    _buffer.addAll([0x1B, 0x64, lines]);
    return this;
  }

  EscPosBuilder lineFeed() {
    _buffer.add(0x0A);
    return this;
  }

  EscPosBuilder alignLeft() {
    _buffer.addAll([0x1B, 0x61, 0x00]);
    return this;
  }

  EscPosBuilder alignCenter() {
    _buffer.addAll([0x1B, 0x61, 0x01]);
    return this;
  }

  EscPosBuilder alignRight() {
    _buffer.addAll([0x1B, 0x61, 0x02]);
    return this;
  }

  EscPosBuilder boldOn() {
    _buffer.addAll([0x1B, 0x45, 0x01]);
    return this;
  }

  EscPosBuilder boldOff() {
    _buffer.addAll([0x1B, 0x45, 0x00]);
    return this;
  }

  EscPosBuilder doubleHeightOn() {
    _buffer.addAll([0x1B, 0x21, 0x10]);
    return this;
  }

  EscPosBuilder doubleHeightOff() {
    _buffer.addAll([0x1B, 0x21, 0x00]);
    return this;
  }

  EscPosBuilder text(String value) {
    _buffer.addAll(_encode(value));
    return this;
  }

  EscPosBuilder textLine(String value) {
    _buffer.addAll(_encode(value));
    _buffer.add(0x0A);
    return this;
  }

  EscPosBuilder cut() {
    _buffer.addAll([0x1D, 0x56, 0x00]);
    return this;
  }

  EscPosBuilder separator({String char = '-', int width = 48}) {
    _buffer.addAll(_encode(char * width));
    _buffer.add(0x0A);
    return this;
  }

  EscPosBuilder beep() {
    _buffer.addAll([0x1B, 0x42, 0x03, 0x03]);
    return this;
  }

  List<int> build() => List.unmodifiable(_buffer);

  Uint8List buildUint8() => Uint8List.fromList(_buffer);

  static List<int> _encode(String text) {
    final codePage = <int>[];
    for (var i = 0; i < text.length; i++) {
      final c = text.codeUnitAt(i);
      if (c <= 127) {
        codePage.add(c);
      } else if (c == 0x00D1) {
        codePage.add(0x00D1);
      } else if (c == 0x00F1) {
        codePage.add(0x00F1);
      } else if (c >= 0xC0 && c <= 0xFF) {
        codePage.add(c - 64);
      } else {
        codePage.add(0x20);
      }
    }
    return codePage;
  }

  Map<String, dynamic> toThermalData({
    required String companyName,
    required String? taxId,
    required String? address,
    required String? phone,
    required String invoiceNumber,
    required String date,
    required String clientName,
    required String saleType,
    required double subtotal,
    required double discount,
    required double tax,
    required double total,
    required double paidAmount,
    required double balance,
    required String status,
    String? notes,
    required List<Map<String, dynamic>> items,
  }) {
    return {
      'companyName': companyName,
      'taxId': taxId,
      'address': address,
      'phone': phone,
      'invoiceNumber': invoiceNumber,
      'date': date,
      'clientName': clientName,
      'saleType': saleType,
      'subtotal': subtotal,
      'discount': discount,
      'tax': tax,
      'total': total,
      'paidAmount': paidAmount,
      'balance': balance,
      'status': status,
      'notes': notes,
      'items': items,
    };
  }

  EscPosBuilder buildReceipt(Map<String, dynamic> data) {
    final isCredit = data['saleType'] == 'credit';
    final dateStr = data['date'].toString().length >= 10
        ? data['date'].toString().substring(0, 10)
        : data['date'].toString();
    final items = (data['items'] as List?)?.cast<Map<String, dynamic>>() ?? <Map<String, dynamic>>[];

    init();

    alignCenter();
    boldOn();
    doubleHeightOn();
    textLine(data['companyName'] ?? '');
    doubleHeightOff();
    boldOff();

    if (data['taxId'] != null && (data['taxId'] as String).isNotEmpty) {
      textLine('RUC: ${data['taxId']}');
    }
    if (data['address'] != null && (data['address'] as String).isNotEmpty) {
      textLine(data['address'] as String);
    }
    if (data['phone'] != null && (data['phone'] as String).isNotEmpty) {
      textLine('Tel: ${data['phone']}');
    }
    separator();

    alignCenter();
    boldOn();
    textLine(isCredit ? 'FACTURA A CRÉDITO' : 'FACTURA');
    boldOff();

    alignLeft();
    textLine('No: ${data['invoiceNumber']}');
    textLine('Fecha: $dateStr');
    textLine('Cliente: ${data['clientName']}');
    separator();

    boldOn();
    textLine('${'PRODUCTO'.padRight(24)}${'CANT'.padLeft(4)}${'TOTAL'.padLeft(10)}');
    boldOff();

    for (final item in items) {
      final name = (item['productName'] ?? '').toString();
      final qty = (item['quantity'] ?? 0).toString();
      final sub = (item['subtotal'] as num?)?.toDouble() ?? 0;
      final line = '${name.padRight(24)}${qty.padLeft(4)}${sub.toStringAsFixed(2).padLeft(10)}';
      textLine(line);
    }
    separator();

    textLine('${'Subtotal:'.padRight(34)}${data['subtotal'].toStringAsFixed(2).padLeft(8)}');
    if (((data['discount'] as num?)?.toDouble() ?? 0) > 0) {
      textLine('${'Descuento:'.padRight(34)}-${(data['discount'] as num).toStringAsFixed(2).padLeft(7)}');
    }
    textLine('${'IVA:'.padRight(34)}${(data['tax'] as num).toStringAsFixed(2).padLeft(8)}');

    boldOn();
    textLine('${'TOTAL:'.padRight(34)}${(data['total'] as num).toStringAsFixed(2).padLeft(8)}');
    boldOff();

    if (isCredit) {
      textLine('${'Pagado:'.padRight(34)}${(data['paidAmount'] as num).toStringAsFixed(2).padLeft(8)}');
      textLine('${'Saldo:'.padRight(34)}${(data['balance'] as num).toStringAsFixed(2).padLeft(8)}');
    }

    if (data['notes'] != null && (data['notes'] as String).isNotEmpty) {
      separator();
      textLine('Notas: ${data['notes']}');
    }

    separator();
    alignCenter();
    textLine('¡Gracias por su compra!');
    textLine('Generado por Zorvian ERP');
    feed(4);
    cut();
    return this;
  }
}
