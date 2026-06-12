import 'package:flutter/material.dart';

/// Screen size breakpoints for responsive design
enum ScreenSize { mobile, tablet, desktop }

/// Helper widget for responsive layouts
class ResponsiveBuilder extends StatelessWidget {
  final Widget Function(BuildContext context, ScreenSize size) builder;

  const ResponsiveBuilder({super.key, required this.builder});

  static ScreenSize getScreenSize(BuildContext context) {
    final width = MediaQuery.of(context).size.width;
    if (width < 576) return ScreenSize.mobile;
    if (width < 992) return ScreenSize.tablet;
    return ScreenSize.desktop;
  }

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        ScreenSize size;
        if (constraints.maxWidth < 576) {
          size = ScreenSize.mobile;
        } else if (constraints.maxWidth < 992) {
          size = ScreenSize.tablet;
        } else {
          size = ScreenSize.desktop;
        }
        return builder(context, size);
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
        EdgeInsetsGeometry padding;
        switch (size) {
          case ScreenSize.mobile:
            padding = mobilePadding ?? const EdgeInsets.all(12);
            break;
          case ScreenSize.tablet:
            padding = tabletPadding ?? const EdgeInsets.all(16);
            break;
          case ScreenSize.desktop:
            padding = desktopPadding ?? const EdgeInsets.all(24);
            break;
        }
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
        int columns;
        if (width < 576) {
          columns = mobileColumns;
        } else if (width < 992) {
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