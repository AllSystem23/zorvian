import 'package:flutter/material.dart';

enum ScreenSize { mobile, tablet, desktop }

ScreenSize getScreenSize(BuildContext context) {
  final width = MediaQuery.of(context).size.width;
  if (width < 600) return ScreenSize.mobile;
  if (width < 1024) return ScreenSize.tablet;
  return ScreenSize.desktop;
}

class ResponsiveBuilder extends StatelessWidget {
  final Widget Function(BuildContext context, ScreenSize size) builder;

  const ResponsiveBuilder({super.key, required this.builder});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (context, constraints) {
        final size = getScreenSize(context);
        return builder(context, size);
      },
    );
  }
}

class ResponsivePadding extends StatelessWidget {
  final Widget child;

  const ResponsivePadding({super.key, required this.child});

  @override
  Widget build(BuildContext context) {
    final size = getScreenSize(context);
    final horizontal = switch (size) {
      ScreenSize.mobile => 12.0,
      ScreenSize.tablet => 24.0,
      ScreenSize.desktop => 32.0,
    };
    return Padding(
      padding: EdgeInsets.symmetric(horizontal: horizontal, vertical: 16),
      child: child,
    );
  }
}

class ResponsiveGrid extends StatelessWidget {
  final List<Widget> children;
  final double spacing;

  const ResponsiveGrid({super.key, required this.children, this.spacing = 12});

  @override
  Widget build(BuildContext context) {
    final size = getScreenSize(context);
    final crossAxisCount = switch (size) {
      ScreenSize.mobile => 2,
      ScreenSize.tablet => 3,
      ScreenSize.desktop => 4,
    };
    return GridView.builder(
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      gridDelegate: SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: crossAxisCount,
        mainAxisSpacing: spacing,
        crossAxisSpacing: spacing,
        childAspectRatio: 1,
      ),
      itemCount: children.length,
      itemBuilder: (_, i) => children[i],
    );
  }
}
