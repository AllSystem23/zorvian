import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/core/navigation/nav_config.dart';

/// Drift entre el catálogo de permisos del backend
/// (`src/Zorvian.Core/Authorization/PermissionCatalog.cs`) y los mapas de gating
/// del frontend (`NavConfig.itemPermissions` para el sidebar y
/// `routePermissionsForTest` para el router).
///
/// El catálogo se LEE del archivo C# en vez de espejarlo a mano: si el backend
/// agrega o renombra un permiso, este test lo ve sin que nadie recuerde
/// actualizar una lista duplicada. El backend tiene su propio test de
/// coherencia (`PermissionEnforcementTests`) que cierra el otro extremo
/// (catálogo ↔ `[RequirePermission]` de los controllers).
Set<String> _catalogCodes() {
  const candidates = <String>[
    'src/Zorvian.Core/Authorization/PermissionCatalog.cs',
    '../src/Zorvian.Core/Authorization/PermissionCatalog.cs',
    '../../src/Zorvian.Core/Authorization/PermissionCatalog.cs',
  ];

  for (final path in candidates) {
    final file = File(path);
    if (!file.existsSync()) continue;
    return RegExp(r'new\("([^"]+)"')
        .allMatches(file.readAsStringSync())
        .map((m) => m.group(1)!)
        .toSet();
  }

  fail('No se encontró PermissionCatalog.cs (CWD: ${Directory.current.path})');
}

void main() {
  final catalog = _catalogCodes();

  test('el catálogo del backend se pudo leer', () {
    expect(catalog.length, greaterThanOrEqualTo(50),
        reason: 'el regex o la ruta del catálogo cambiaron');
  });

  test('el gating del sidebar solo usa códigos del catálogo', () {
    final unknown = NavConfig.itemPermissions.values.toSet().difference(catalog);

    expect(unknown, isEmpty, reason: 'códigos que no existen en el backend: $unknown');
  });

  test('el gating de rutas solo usa códigos del catálogo', () {
    final unknown = routePermissionsForTest.values.toSet().difference(catalog);

    expect(unknown, isEmpty, reason: 'códigos que no existen en el backend: $unknown');
  });
}
