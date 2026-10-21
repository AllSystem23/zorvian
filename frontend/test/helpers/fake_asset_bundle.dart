import 'dart:convert';

import 'package:flutter/services.dart';

/// Bundle de assets falso para los tests.
///
/// Las páginas de autenticación muestran `assets/Zorvian.png`. En los tests no
/// existe el bundle real, así que este lo simula:
///
/// * `AssetManifest.bin` → manifiesto válido (lo exige `AssetImage.obtainKey`:
///   sin él, la resolución de la imagen revienta con "Message corrupted").
/// * cualquier otro asset → un PNG transparente de 1x1.
///
/// Uso:
/// ```dart
/// await tester.pumpWidget(
///   DefaultAssetBundle(bundle: FakeAssetBundle(), child: app),
/// );
/// ```
class FakeAssetBundle extends CachingAssetBundle {
  static const _png =
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';

  /// Assets que el manifiesto declara (el primero es el que se pinta).
  static const _assets = ['assets/Zorvian.png', 'assets/logo_erp.png'];

  @override
  Future<ByteData> load(String key) async {
    if (key == 'AssetManifest.bin') return _manifest();

    final bytes = base64Decode(_png);
    return ByteData.view(Uint8List.fromList(bytes).buffer);
  }

  ByteData _manifest() {
    const codec = StandardMessageCodec();
    final message = <String, Object?>{
      'assets': <Object?>[
        for (final asset in _assets)
          <Object?>[
            asset,
            <Object?>[
              <String, Object?>{'type': 'asset', 'url': asset},
            ],
          ],
      ],
    };

    // `encodeMessage` ya devuelve un ByteData con el mensaje completo: hay que
    // devolverlo tal cual (re-armarlo sobre el buffermetería bytes de más y
    // el decoder fallaría con "Message corrupted").
    return codec.encodeMessage(message)!;
  }
}
