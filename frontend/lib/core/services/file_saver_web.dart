import 'dart:js_interop';
import 'dart:typed_data';
import 'package:web/web.dart';

Future<void> saveFile(List<int> data, String fileName) async {
  final blobParts = [Uint8List.fromList(data).toJS].toJS;
  final blob = Blob(blobParts as JSArray<BlobPart>);
  final url = URL.createObjectURL(blob);
  HTMLAnchorElement()
    ..href = url
    ..download = fileName
    ..click();
  URL.revokeObjectURL(url);
}
