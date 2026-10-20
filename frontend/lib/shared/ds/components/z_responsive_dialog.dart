import 'package:flutter/material.dart';
import '../ds.dart';

/// Abre un contenedor modal adaptativo.
///
/// - **Teléfono** (`< ZBreakpoints.phone`): hoja inferior
///   (`showModalBottomSheet` a pantalla completa, con handle y arrastre).
/// - **Tablet/escritorio**: `showDialog` clásico.
///
/// Úsalo en lugar de `showDialog` para que un diálogo con ancho fijo no se
/// salga de un teléfono de 360 px.
Future<T?> showZAdaptiveDialog<T>(
  BuildContext context, {
  required WidgetBuilder builder,
  bool barrierDismissible = true,
  bool useRootNavigator = true,
}) {
  final isPhone = MediaQuery.sizeOf(context).width < ZBreakpoints.phone;
  if (isPhone) {
    return showModalBottomSheet<T>(
      context: context,
      useRootNavigator: useRootNavigator,
      isScrollControlled: true,
      useSafeArea: true,
      isDismissible: barrierDismissible,
      enableDrag: barrierDismissible,
      backgroundColor: Colors.transparent,
      builder: builder,
    );
  }
  return showDialog<T>(
    context: context,
    useRootNavigator: useRootNavigator,
    barrierDismissible: barrierDismissible,
    builder: builder,
  );
}

/// Diálogo adaptativo: `Dialog` centrado en pantallas anchas y hoja inferior
/// a pantalla completa en teléfono, con las acciones fijas al pie.
///
/// El contenido siempre tiene altura acotada y scroll propio, y el pie sube
/// con el teclado ([ZStickyActionBar.includeViewInsets]).
final class ZResponsiveDialog extends StatelessWidget {
  final String? title;
  final IconData? icon;
  final Color? iconColor;
  final Widget child;

  /// Acciones del pie. En escritorio van alineadas a la derecha; en teléfono
  /// se apilan a ancho completo con la **primaria arriba** (la última de la
  /// lista es la primaria, igual que en `AlertDialog.actions`).
  final List<Widget> actions;

  final double maxWidth;
  final bool scrollable;
  final bool showCloseButton;
  final EdgeInsetsGeometry? contentPadding;

  const ZResponsiveDialog({
    super.key,
    this.title,
    this.icon,
    this.iconColor,
    required this.child,
    this.actions = const [],
    this.maxWidth = 560,
    this.scrollable = true,
    this.showCloseButton = true,
    this.contentPadding,
  });

  /// Abre el diálogo. Devuelve el valor con el que se cerró.
  static Future<T?> show<T>(
    BuildContext context, {
    String? title,
    IconData? icon,
    Color? iconColor,
    required Widget child,
    List<Widget> actions = const [],
    double maxWidth = 560,
    bool scrollable = true,
    bool showCloseButton = true,
    bool barrierDismissible = true,
  }) {
    return showZAdaptiveDialog<T>(
      context,
      barrierDismissible: barrierDismissible,
      builder: (_) => ZResponsiveDialog(
        title: title,
        icon: icon,
        iconColor: iconColor,
        maxWidth: maxWidth,
        scrollable: scrollable,
        showCloseButton: showCloseButton,
        actions: actions,
        child: child,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final isPhone = MediaQuery.sizeOf(context).width < ZBreakpoints.phone;
    final surface = isDark ? ZColors.darkSurface : ZColors.surface;
    final padding = contentPadding ??
        EdgeInsets.fromLTRB(
          isPhone ? ZSpacing.lg : ZSpacing.xl,
          0,
          isPhone ? ZSpacing.lg : ZSpacing.xl,
          ZSpacing.lg,
        );

    final body = Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (isPhone)
          Padding(
            padding: const EdgeInsets.only(top: ZSpacing.sm),
            child: Center(
              child: Container(
                width: 40,
                height: 4,
                decoration: BoxDecoration(
                  color: isDark ? ZColors.neutral600 : ZColors.neutral300,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
          ),
        if (title != null || showCloseButton) _buildHeader(context, isDark, isPhone),
        Flexible(
          child: scrollable
              ? SingleChildScrollView(padding: padding, child: child)
              : Padding(padding: padding, child: child),
        ),
        if (actions.isNotEmpty)
          ZStickyActionBar(
            actions: actions,
            stacked: isPhone,
            includeViewInsets: isPhone,
            showTopBorder: true,
            padding: EdgeInsets.fromLTRB(
              isPhone ? ZSpacing.lg : ZSpacing.xl,
              ZSpacing.md,
              isPhone ? ZSpacing.lg : ZSpacing.xl,
              ZSpacing.md,
            ),
          ),
      ],
    );

    if (isPhone) {
      return SafeArea(
        top: false,
        child: Container(
          constraints: BoxConstraints(
            maxHeight: MediaQuery.sizeOf(context).height * 0.94,
          ),
          decoration: BoxDecoration(
            color: surface,
            borderRadius: const BorderRadius.vertical(
              top: Radius.circular(ZRadii.xl),
            ),
          ),
          clipBehavior: Clip.antiAlias,
          child: body,
        ),
      );
    }

    return Dialog(
      backgroundColor: surface,
      surfaceTintColor: Colors.transparent,
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.xl),
      ),
      child: ConstrainedBox(
        constraints: BoxConstraints(
          maxWidth: maxWidth,
          maxHeight: MediaQuery.sizeOf(context).height * 0.85,
        ),
        child: body,
      ),
    );
  }

  Widget _buildHeader(BuildContext context, bool isDark, bool isPhone) {
    final titleStyle = ZTypography.headlineSmall.copyWith(
      color: isDark ? ZColors.neutral100 : ZColors.neutral800,
    );
    return Padding(
      padding: EdgeInsets.fromLTRB(
        isPhone ? ZSpacing.lg : ZSpacing.xl,
        isPhone ? ZSpacing.md : ZSpacing.xl,
        isPhone ? ZSpacing.sm : ZSpacing.md,
        0,
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (icon != null) ...[
            Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Icon(
                icon,
                size: 26,
                color: iconColor ??
                    (isDark ? ZColors.neutral200 : ZColors.neutral700),
              ),
            ),
            const SizedBox(width: ZSpacing.md),
          ],
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(top: icon != null ? 2 : 4),
              child: Text(
                title ?? '',
                style: titleStyle,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ),
          if (showCloseButton)
            IconButton(
              icon: const Icon(Icons.close, size: 20),
              tooltip: 'Cerrar',
              onPressed: () => Navigator.of(context).pop(),
            ),
        ],
      ),
    );
  }
}

/// Barra de acciones pegajosa al pie de un diálogo, hoja o formulario.
///
/// - `stacked`: acciones a ancho completo, primaria arriba (teléfono).
/// - `includeViewInsets`: suma el alto del teclado para que la acción primaria
///   nunca quede tapada (la hoja inferior no lo hace sola).
final class ZStickyActionBar extends StatelessWidget {
  final List<Widget> actions;
  final Widget? leading;
  final bool stacked;
  final bool includeViewInsets;
  final bool showTopBorder;
  final EdgeInsetsGeometry padding;

  const ZStickyActionBar({
    super.key,
    required this.actions,
    this.leading,
    this.stacked = false,
    this.includeViewInsets = false,
    this.showTopBorder = false,
    this.padding = const EdgeInsets.all(ZSpacing.lg),
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final inset = includeViewInsets
        ? MediaQuery.viewInsetsOf(context).bottom
        : 0.0;

    final content = stacked
        ? Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              if (leading != null) ...[
                leading!,
                const SizedBox(height: ZSpacing.sm),
              ],
              for (final action in actions.reversed)
                Padding(
                  padding: const EdgeInsets.only(top: ZSpacing.sm),
                  child: SizedBox(width: double.infinity, child: action),
                ),
            ],
          )
        : Row(
            children: [
              if (leading != null) Expanded(child: leading!),
              for (final action in actions)
                Padding(
                  padding: const EdgeInsets.only(left: ZSpacing.sm),
                  child: action,
                ),
            ],
          );

    return AnimatedPadding(
      duration: const Duration(milliseconds: 120),
      curve: Curves.easeOut,
      padding: EdgeInsets.only(bottom: inset),
      child: Container(
        padding: padding,
        decoration: BoxDecoration(
          color: isDark ? ZColors.darkSurface : ZColors.surface,
          border: showTopBorder
              ? Border(
                  top: BorderSide(
                    color: isDark ? ZColors.darkBorder : ZColors.border,
                  ),
                )
              : null,
        ),
        child: content,
      ),
    );
  }
}
