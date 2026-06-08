// ignore_for_file: deprecated_member_use, avoid_web_libraries_in_flutter
import 'dart:html' as html;
import 'dart:typed_data';

void downloadBytes(Uint8List bytes, String filename) {
  final mimeType = filename.endsWith('.csv') ? 'text/csv' : 'application/pdf';
  final blob = html.Blob([bytes], mimeType);
  final url = html.Url.createObjectUrl(blob);
  final anchor = html.document.createElement('a') as html.AnchorElement
    ..href = url
    ..download = filename
    ..style.display = 'none';
  html.document.body!.append(anchor);
  anchor.click();
  anchor.remove();
  html.Url.revokeObjectUrl(url);
}

void printHtml(String htmlContent) {
  final win = html.window.open('', '_blank') as dynamic;
  if (win == null) return;
  win.document.write(htmlContent);
  win.document.close();
  win.focus();
  win.print();
}
