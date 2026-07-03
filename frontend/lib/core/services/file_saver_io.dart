import 'dart:io';

Future<void> saveFile(List<int> data, String fileName) async {
  final dir = Directory.systemTemp;
  final file = File('${dir.path}/$fileName');
  await file.writeAsBytes(data);
}
