import 'package:flutter/material.dart';

class ZLiveRegion extends StatelessWidget {
  final Widget child;
  final String? label;

  const ZLiveRegion({
    super.key,
    required this.child,
    this.label,
  });

  @override
  Widget build(BuildContext context) {
    return Semantics(
      liveRegion: true,
      label: label,
      child: child,
    );
  }
}
