import 'package:flutter/material.dart';
import '../ds.dart';

class ZSkipLink extends StatefulWidget {
  final String label;
  final FocusNode targetFocus;

  const ZSkipLink({
    super.key,
    this.label = 'Saltar al contenido principal',
    required this.targetFocus,
  });

  @override
  State<ZSkipLink> createState() => _ZSkipLinkState();
}

class _ZSkipLinkState extends State<ZSkipLink> {
  bool _focused = false;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      label: widget.label,
      child: GestureDetector(
        onTap: () => widget.targetFocus.requestFocus(),
        child: Focus(
          onFocusChange: (v) => setState(() => _focused = v),
          child: Container(
            width: double.infinity,
            height: _focused ? 44 : 1,
            color: _focused ? ZColors.brandPrimary : Colors.transparent,
            alignment: Alignment.center,
            child: Text(
              widget.label,
              style: TextStyle(
                color: Colors.white,
                fontSize: _focused ? 14 : 0.1,
                fontWeight: FontWeight.w600,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
