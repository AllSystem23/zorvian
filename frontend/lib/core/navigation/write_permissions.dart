/// Permisos de ESCRITURA por módulo (fuente única de verdad del frontend).
///
/// Lo usan dos consumidores con la misma semántica:
/// - el guard del router (`_hasAccess`), que bloquea la navegación a pantallas
///   de creación/edición sin el permiso del módulo, y
/// - `ZQuickActionsFAB`, que oculta las acciones rápidas que llevarían a una
///   ruta bloqueada.
///
/// La clave es el prefijo del módulo (la ruta raíz); el permiso se resuelve por
/// el prefijo MÁS específico que contenga la location, igual que
/// `_routeRoles`/`_routePermissions` del router. Sin `permissions` cargados
/// (SSO/compat/API Key) y para SuperAdmin no se gatea nada — la misma regla que
/// `AuthState.hasPermission`.
library;

/// Módulo (prefijo de ruta) → permiso de escritura que exige.
const Map<String, String> moduleWritePermissions = {
  // Ventas y clientes
  '/sales': 'sale.write',
  '/quotes': 'sale.write',
  '/clients': 'client.write',
  '/credits': 'credit.write',
  '/credit-notes': 'credit.write',
  '/crm': 'sale.write',
  // Inventario
  '/products': 'inventory.write',
  '/categories': 'inventory.write',
  '/brands': 'inventory.write',
  '/warehouses': 'inventory.write',
  '/stock-transfers': 'inventory.write',
  '/inventory-movements': 'inventory.write',
  '/warranties': 'warranty.write',
  '/workshops': 'fleet.write',
  // Compras
  '/purchases': 'purchase.write',
  '/purchase-orders': 'purchase.write',
  '/purchase-requests': 'purchase.write',
  '/suppliers': 'purchase.write',
  // Flota
  '/fleet': 'fleet.write',
  // Finanzas
  '/cash-registers': 'cash.write',
  '/accounting': 'accounting.write',
  '/budgets': 'accounting.write',
  '/cost-centers': 'accounting.write',
  '/exchange-rates': 'accounting.write',
  '/reconciliations': 'accounting.write',
  '/treasury': 'accounting.write',
  '/providers': 'provider.write',
  // Talento
  '/employees': 'employee.write',
  '/departments': 'employee.write',
  '/leave-types': 'employee.write',
  '/service-payments': 'payroll.write',
  '/payroll': 'payroll.write',
  '/goals': 'goal.write',
  // Configuración / admin
  // `/branches` es una pantalla de ADMINISTRACIÓN (lista + form de creación y
  // de edición en `/branches/:id`, que no termina en `/edit`): el prefijo
  // completo exige el permiso de escritura de configuración, igual que el
  // `BranchesController`.
  '/branches': 'settings.write',
  '/webhooks': 'settings.write',
  '/approval-flows': 'settings.write',
  '/documents': 'document.write',
  '/custom-reports': 'report.read',
};

/// Rutas EXACTAS que son pantallas de escritura aunque no lleven el marcador
/// `new`/`edit` en sus últimos segmentos.
const Map<String, String> explicitWriteRoutes = {
  '/goals/configurator': 'goal.write',
  // `extra` = warehouseId para editar; null = crear.
  '/warehouses/form': 'inventory.write',
};

/// Devuelve el permiso de escritura exigido por [location], o `null` si la
/// ruta no es una pantalla de creación/edición (y por tanto no se gatea).
String? writePermissionFor(String location) {
  final explicit = explicitWriteRoutes[location];
  if (explicit != null) return explicit;

  final segments = location.split('/').where((s) => s.isNotEmpty).toList();
  if (segments.isEmpty) return null;

  // Solo pantallas dedicadas a crear/editar. El marcador de acción es el último
  // segmento (`/sales/new`, `/sales/:id/edit`) o el PENÚLTIMO (`/credit-notes/new/:id`,
  // `/service-payments/edit/:id`): el router usa ambas variantes.
  final last = segments.last;
  final previous = segments.length > 1 ? segments[segments.length - 2] : last;
  if (last != 'new' && last != 'edit' && previous != 'new' && previous != 'edit') {
    return null;
  }

  String? bestKey;
  var bestLen = -1;
  for (final key in moduleWritePermissions.keys) {
    final keySegments = key.split('/').where((s) => s.isNotEmpty).toList();
    if (keySegments.length > segments.length) continue;
    if (!location.startsWith(key) ||
        (location.length > key.length &&
            location.codeUnitAt(key.length) != 0x2F /* '/' */)) {
      continue;
    }
    if (keySegments.length > bestLen) {
      bestKey = key;
      bestLen = keySegments.length;
    }
  }

  return bestKey == null ? null : moduleWritePermissions[bestKey];
}

/// True si el usuario puede navegar a [location] según su permiso de escritura.
/// Sin [permissions] (compat/SSO) o con SuperAdmin siempre devuelve true.
bool canWriteRoute(String location, List<String>? permissions, String role) {
  if (permissions == null || role == 'SuperAdmin') return true;
  final required = writePermissionFor(location);
  return required == null || permissions.contains(required);
}
