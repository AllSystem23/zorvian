import 'dart:io';

void main() {
  const templatePath = 'lib/core/firebase_options.template.dart';
  const outputPath = 'lib/core/firebase_options.dart';

  final template = File(templatePath).readAsStringSync();

  final replacements = {
    '__FIREBASE_API_KEY__': _env('FIREBASE_API_KEY', ''),
    '__FIREBASE_AUTH_DOMAIN__': _env('FIREBASE_AUTH_DOMAIN', ''),
    '__FIREBASE_PROJECT_ID__': _env('FIREBASE_PROJECT_ID', ''),
    '__FIREBASE_STORAGE_BUCKET__': _env('FIREBASE_STORAGE_BUCKET', ''),
    '__FIREBASE_MESSAGING_SENDER_ID__': _env('FIREBASE_MESSAGING_SENDER_ID', ''),
    '__FIREBASE_APP_ID__': _env('FIREBASE_APP_ID', ''),
  };

  final missing = replacements.entries.where((e) => e.value.isEmpty).map((e) => e.key).toList();

  if (missing.isNotEmpty) {
    stderr.writeln('ERROR: Variables de entorno faltantes:');
    for (final key in missing) {
      stderr.writeln('  - $key');
    }
    stderr.writeln('\nDefínelas como variables de entorno o en un archivo .env');
    exitCode = 1;
    return;
  }

  var output = template;
  for (final entry in replacements.entries) {
    output = output.replaceAll(entry.key, entry.value);
  }

  File(outputPath).writeAsStringSync(output);
  print('✅ $outputPath generado correctamente');
}

String _env(String key, String defaultValue) =>
    Platform.environment[key]?.trim() ?? defaultValue;
