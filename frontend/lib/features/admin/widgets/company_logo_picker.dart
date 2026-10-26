import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:http_parser/http_parser.dart';

import '../../../core/network/dio_client.dart';
import '../../../shared/ds/ds.dart';

/// Tamaño máximo aceptado por el API (`[RequestSizeLimit(5 * 1024 * 1024)]`).
const int maxCompanyLogoBytes = 5 * 1024 * 1024;

/// Logo elegido por el usuario, todavía sin subir.
class PickedLogo {
  final Uint8List bytes;
  final String fileName;

  const PickedLogo({required this.bytes, required this.fileName});
}

/// Error legible al elegir el archivo (se muestra tal cual al usuario).
class LogoPickException implements Exception {
  final String message;
  const LogoPickException(this.message);

  @override
  String toString() => message;
}

/// MIME type que espera el API según la extensión del archivo.
///
/// Solo acepta PNG, JPG/JPEG y WebP; cualquier otra extensión se envía como
/// PNG (el archivo sigue siendo válido porque el picker filtra por imágenes).
String logoContentType(String fileName) {
  final ext = fileName.split('.').last.toLowerCase();
  return switch (ext) {
    'png' => 'image/png',
    'jpg' || 'jpeg' => 'image/jpeg',
    'webp' => 'image/webp',
    _ => 'image/png',
  };
}

/// Lee los bytes del archivo elegido.
///
/// `PlatformFile.readAsBytes()` solo funciona cuando el picker ya cargó los
/// bytes en memoria (`withData`, que por defecto es `true` únicamente en web).
/// En Windows/Android/iOS `bytes` es `null` y ese método lanza `StateError`:
/// esa era la razón por la que el logo nunca se subía desde la app de
/// escritorio/móvil. `readAsByteStream()` funciona en todas las plataformas.
Future<Uint8List> readPickedFileBytes(PlatformFile file) async {
  final builder = BytesBuilder(copy: false);
  await for (final chunk in file.readAsByteStream()) {
    builder.add(chunk);
  }
  return builder.toBytes();
}

/// Abre el selector de imágenes y devuelve el logo validado.
///
/// Devuelve `null` si el usuario cancela. Lanza [LogoPickException] con un
/// mensaje legible si el archivo excede el límite o no se puede leer.
Future<PickedLogo?> pickCompanyLogo() async {
  final result = await FilePicker.pickFiles(type: FileType.image);
  if (result == null || result.files.isEmpty) return null;

  final file = result.files.first;
  if (file.size > maxCompanyLogoBytes) {
    throw const LogoPickException('El logo no puede superar 5 MB');
  }

  final bytes = await readPickedFileBytes(file);
  if (bytes.isEmpty) {
    throw const LogoPickException('No se pudo leer el archivo seleccionado');
  }

  return PickedLogo(bytes: bytes, fileName: file.name);
}

/// Seam de test: los diálogos leen el selector desde aquí para poder sustituir
/// el diálogo nativo del sistema operativo (no disponible en `flutter test`).
final logoPickerProvider =
    Provider<Future<PickedLogo?> Function()>((_) => pickCompanyLogo);

/// Sube el logo de la compañía y devuelve la URL firmada que responde el API.
Future<String?> uploadCompanyLogo(
  DioClient dio,
  String companyId, {
  required Uint8List bytes,
  required String fileName,
}) async {
  final formData = FormData.fromMap({
    'file': MultipartFile.fromBytes(
      bytes,
      filename: fileName,
      contentType: MediaType.parse(logoContentType(fileName)),
    ),
  });

  // `suppressGlobalError`: el fallo se muestra en el diálogo con un mensaje
  // contextual (y en el caso de creación, como aviso de "empresa creada, logo
  // pendiente"); sin esto el interceptor global mostraría además su propio toast.
  final response = await dio.post(
    'companies/$companyId/logo',
    data: formData,
    options: Options(extra: {'suppressGlobalError': true}),
  );
  final data = response.data;
  if (data is Map && data['logoUrl'] is String) {
    final url = data['logoUrl'] as String;
    return url.isEmpty ? null : url;
  }
  return null;
}

/// Quita el logo de la compañía: el API limpia la referencia en la base y
/// borra el objeto del almacenamiento (sin dejar huérfanos).
Future<void> removeCompanyLogo(DioClient dio, String companyId) async {
  await dio.delete(
    'companies/$companyId/logo',
    options: Options(extra: {'suppressGlobalError': true}),
  );
}

/// Mensaje legible para un fallo de subida (el API responde `{ "error": … }`).
String companyLogoErrorMessage(Object error) {
  if (error is LogoPickException) return error.message;

  if (error is DioException) {
    final data = error.response?.data;
    if (data is Map) {
      for (final key in const ['error', 'message', 'detail', 'title']) {
        final value = data[key];
        if (value is String && value.isNotEmpty) return value;
        if (value is List && value.isNotEmpty) return value.first.toString();
      }
    }
    if (error.response?.statusCode == 404) {
      return 'No se encontró la empresa para asociar el logo';
    }
    return error.message ?? 'Error de red al subir el logo';
  }

  return error.toString();
}

/// Sección de logo compartida por los diálogos de crear/editar empresa.
class CompanyLogoField extends StatelessWidget {
  /// Bytes del logo recién elegido (tiene prioridad sobre [currentLogoUrl]).
  final Uint8List? pendingBytes;

  /// Nombre del archivo elegido.
  final String? fileName;

  /// URL firmada del logo ya guardado (solo para vista previa).
  final String? currentLogoUrl;

  /// Muestra el spinner mientras se sube el archivo.
  final bool uploading;

  final VoidCallback onPick;

  /// Si se indica, muestra "Quitar logo" cuando hay un logo (elegido o
  /// guardado). Los diálogos lo usan para limpiar la selección o llamar al API.
  final VoidCallback? onRemove;

  const CompanyLogoField({
    super.key,
    this.pendingBytes,
    this.fileName,
    this.currentLogoUrl,
    this.uploading = false,
    required this.onPick,
    this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    final hasPreview = pendingBytes != null;
    final hasCurrent = (currentLogoUrl ?? '').isNotEmpty;
    final hasLogo = hasPreview || hasCurrent;
    final canRemove = onRemove != null && hasLogo;

    return Center(
      child: Column(
        children: [
          GestureDetector(
            onTap: uploading ? null : onPick,
            child: Container(
              width: 96,
              height: 96,
              decoration: BoxDecoration(
                color: ZColors.brandPrimary.withValues(alpha: 0.05),
                borderRadius: BorderRadius.circular(16),
                border: Border.all(
                  color: hasPreview ? ZColors.brandAccent : ZColors.neutral400,
                  width: hasPreview ? 2 : 1,
                ),
              ),
              child: hasPreview
                  ? ClipRRect(
                      borderRadius: BorderRadius.circular(14),
                      child: Image.memory(pendingBytes!, fit: BoxFit.cover),
                    )
                  : hasCurrent
                      ? ClipRRect(
                          borderRadius: BorderRadius.circular(14),
                          child: Image.network(
                            currentLogoUrl!,
                            fit: BoxFit.cover,
                            errorBuilder: (context, error, stack) =>
                                _buildLogoPlaceholder(),
                          ),
                        )
                      : _buildLogoPlaceholder(),
            ),
          ),
          const SizedBox(height: 8),
          FilledButton.tonalIcon(
            onPressed: uploading ? null : onPick,
            icon: uploading
                ? const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  )
                : Icon(
                    Icons.camera_alt_outlined,
                    size: 16,
                    color: hasPreview ? ZColors.brandAccent : null,
                  ),
            label: Text(
              hasLogo ? 'Cambiar logo' : 'Subir logo',
              style: TextStyle(color: hasPreview ? ZColors.brandAccent : null),
            ),
          ),
          if (canRemove)
            TextButton.icon(
              onPressed: uploading ? null : onRemove,
              icon: const Icon(Icons.delete_outline, size: 16),
              label: const Text('Quitar logo'),
              style: TextButton.styleFrom(foregroundColor: ZColors.danger),
            ),
          if (fileName != null)
            Padding(
              padding: const EdgeInsets.only(top: 4),
              child: Text(
                fileName!,
                style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500),
              ),
            ),
        ],
      ),
    );
  }

  Widget _buildLogoPlaceholder() {
    return const Center(
      child: Icon(
        Icons.add_a_photo_outlined,
        size: 32,
        color: ZColors.neutral400,
      ),
    );
  }
}
