import 'package:flutter/material.dart';
import '../ds.dart';

class ZCard extends StatelessWidget {
  final Widget child;
  final EdgeInsetsGeometry? padding;
  final double? minHeight;
  final EdgeInsetsGeometry? margin;

  const ZCard({
    super.key,
    required this.child,
    this.padding,
    this.minHeight,
    this.margin,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      margin: margin ?? EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        side: const BorderSide(color: ZColors.border),
      ),
      child: Container(
        width: double.infinity,
        constraints: minHeight != null ? BoxConstraints(minHeight: minHeight!) : null,
        padding: padding ?? const EdgeInsets.all(ZSpacing.lg),
        child: child,
      ),
    );
  }
}
