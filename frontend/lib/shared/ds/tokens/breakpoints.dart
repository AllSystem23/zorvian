/// Breakpoints y métricas de interacción de la capa responsive.
///
/// Fuente ÚNICA de verdad: ningún widget debe volver a comparar anchos con
/// literales (`576`, `992`, `900`). El objetivo principal es que la app sea
/// operable en teléfonos de 360–430 px de ancho lógico.
abstract final class ZBreakpoints {
  /// Ancho mínimo soportado (teléfonos pequeños / `textScale` alto).
  static const double compactPhone = 360;

  /// Teléfono: una columna, `NavigationBar`, sin scroll horizontal.
  static const double phone = 600;

  /// Tablet: sidebar colapsada + header completo.
  static const double tablet = 992;

  /// Escritorio angosto: la sidebar se auto-colapsa.
  static const double narrowDesktop = 1200;

  /// A partir de aquí el `GlobalHeader` muestra sucursal y bodega.
  static const double headerFullControls = 900;

  /// A partir de aquí el `GlobalHeader` muestra los controles extra.
  static const double headerExtraControls = 700;

  static bool isPhone(double width) => width < phone;

  static bool isTablet(double width) => width >= phone && width < tablet;

  static bool isDesktop(double width) => width >= tablet;

  static bool isNarrowDesktop(double width) =>
      width >= tablet && width < narrowDesktop;

  /// Padding horizontal de una página en teléfono.
  static const double phonePagePadding = 16;
}

/// Áreas táctiles mínimas. Las guías de Material/WCAG piden 48 dp; 44 es el
/// mínimo absoluto aceptado para controles secundarios.
abstract final class ZTouchTarget {
  static const double min = 48;
  static const double compact = 44;
}
