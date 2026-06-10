import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../shared/ds/components/z_command_palette.dart';

/// Global keyboard shortcuts service
/// Press Cmd+K (Mac) or Ctrl+K (Other) to open Command Palette
class KeyboardShortcutService extends StatefulWidget {
  final Widget child;

  const KeyboardShortcutService({super.key, required this.child});

  @override
  State<KeyboardShortcutService> createState() => _KeyboardShortcutServiceState();
}

class _KeyboardShortcutServiceState extends State<KeyboardShortcutService> {
  final FocusNode _focusNode = FocusNode();

  @override
  void dispose() {
    _focusNode.dispose();
    super.dispose();
  }

  bool _handleKey(KeyEvent event) {
    if (event is! KeyDownEvent) return false;

    final isMac = Theme.of(context).platform == TargetPlatform.macOS;
    final modifier = isMac
        ? HardwareKeyboard.instance.isMetaPressed
        : HardwareKeyboard.instance.isControlPressed;

    // Cmd/Ctrl + K → Command Palette
    if (modifier && event.logicalKey == LogicalKeyboardKey.keyK) {
      ZCommandPalette.show(context);
      return true;
    }
    return false;
  }

  @override
  Widget build(BuildContext context) {
    return Focus(
      focusNode: _focusNode,
      autofocus: true,
      onKeyEvent: (node, event) {
        if (_handleKey(event)) {
          return KeyEventResult.handled;
        }
        return KeyEventResult.ignored;
      },
      child: widget.child,
    );
  }
}