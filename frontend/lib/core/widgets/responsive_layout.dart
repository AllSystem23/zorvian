import 'package:flutter/material.dart';

import '../../shared/ds/tokens/breakpoints.dart';

/// Screen size breakpoints for responsive design
enum ScreenSize { mobile, tablet, desktop }

/// Helper widget for responsive layouts.
///
/// Los cortes viven en [ZBreakpoints] (fuente única). No volver a comparar
/// anchos con literales en widgets nuevos.
class ResponsiveBuilder extends StatelessWidget {
  final Widget Function(BuildContext context, ScreenSize size) builder;

  const ResponsiveBuilder({super.key, required this.builder});

  /// Resuelve el [ScreenSize] a partir de un ancho lógico.
  static ScreenSize fromWidth(double width) {
    if (width < ZBreakpoints.phone) return ScreenSize.mobile;
    if (width < ZBreakpoints.tablet) return ScreenSize.tablet;
    return ScreenSize.desktop;
  }

  static ScreenSize getScreenSize(BuildContext context) {
    return fromWidth(MediaQuery.of(context).size.width);
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        return builder(context, fromWidth(constraints.maxWidth));
      },
    );
  }
}

/// Responsive padding helper
class ResponsivePadding extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? mobilePadding;
  final EdgeInsetsGeometry? tabletPadding;
  final EdgeInsetsGeometry? desktopPadding;

  const ResponsivePadding({
    super.key,
    required this.child,
    this.mobilePadding,
    this.tabletPadding,
    this.desktopPadding,
  });

  @override
  Widget build(BuildContext context) {
    return ResponsiveBuilder(
      builder: (_, size) {
        final padding = switch (size) {
          ScreenSize.mobile => mobilePadding ??
              const EdgeInsets.all(ZBreakpoints.phonePagePadding),
          ScreenSize.tablet => tabletPadding ?? const EdgeInsets.all(16),
          ScreenSize.desktop => desktopPadding ?? const EdgeInsets.all(24),
        };
        return Padding(padding: padding, child: child);
      },
    );
  }
}

/// Responsive grid helper
class ResponsiveGrid extends StatelessWidget {
  final List<Widget> children;
  final int mobileColumns;
  final int tabletColumns;
  final int desktopColumns;
  final double spacing;
  final double runSpacing;

  const ResponsiveGrid({
    super.key,
    required this.children,
    this.mobileColumns = 1,
    this.tabletColumns = 2,
    this.desktopColumns = 3,
    this.spacing = 16,
    this.runSpacing = 16,
  });

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final width = constraints.maxWidth;
        final int columns;
        if (width < ZBreakpoints.phone) {
          columns = mobileColumns;
        } else if (width < ZBreakpoints.tablet) {
          columns = tabletColumns;
        } else {
          columns = desktopColumns;
        }

        return Wrap(
          spacing: spacing,
          runSpacing: runSpacing,
          children: children.map((child) {
            final itemWidth = (width - (spacing * (columns - 1))) / columns;
            return SizedBox(width: itemWidth.floorToDouble(), child: child);
          }).toList(),
        );
      },
    );
  }
}

/// Hide on screen size
class HideOn extends StatelessWidget {
  final ScreenSize screenSize;
  final Widget child;

  const HideOn({super.key, required this.screenSize, required this.child});

  @override
  Widget build(BuildContext context) {
    return ResponsiveBuilder(
      builder: (_, size) => size == screenSize ? const SizedBox.shrink() : child,
    );
  }
}

/// Show only on screen size
class ShowOn extends StatelessWidget {
  final ScreenSize screenSize;
  final Widget child;

  const ShowOn({super.key, required this.screenSize, required this.child});

  @override
  Widget build(BuildContext context) {
    return ResponsiveBuilder(
      builder: (_, size) => size == screenSize ? child : const SizedBox.shrink(),
    );
  }
}
