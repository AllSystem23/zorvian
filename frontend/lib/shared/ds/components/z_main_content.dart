import 'package:flutter/material.dart';

class ZMainContent extends StatelessWidget {
  final FocusNode focusNode;
  final Widget child;

  const ZMainContent({
    super.key,
    required this.focusNode,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Focus(
      focusNode: focusNode,
      child: child,
    );
  }
}
