import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/services.dart';

void downloadBytes(Uint8List bytes, String filename) {
  final dir = Directory.systemTemp;
  final file = File('${dir.path}/$filename');
  file.writeAsBytesSync(bytes);
}

void printHtml(String htmlContent) {
  final dir = Directory.systemTemp;
  final file = File('${dir.path}/thermal_print.html');
  file.writeAsStringSync(htmlContent);
}
