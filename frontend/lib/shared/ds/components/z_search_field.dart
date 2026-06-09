import 'package:flutter/material.dart';
import 'package:zorvian/core/utils/debouncer.dart';

/// Search field with built-in debouncing
class ZSearchField extends StatefulWidget {
  final String? hintText;
  final ValueChanged<String>? onChanged;
  final int debounceMs;
  final TextEditingController? controller;
  final VoidCallback? onClear;
  final bool autofocus;
  final double? width;

  const ZSearchField({
    super.key,
    this.hintText = 'Buscar...',
    this.onChanged,
    this.debounceMs = 400,
    this.controller,
    this.onClear,
    this.autofocus = false,
    this.width,
  });

  @override
  State<ZSearchField> createState() => _ZSearchFieldState();
}

class _ZSearchFieldState extends State<ZSearchField> {
  late final TextEditingController _controller;
  late final ZDebouncer _debouncer;
  bool _hasText = false;

  @override
  void initState() {
    super.initState();
    _controller = widget.controller ?? TextEditingController();
    _debouncer = ZDebouncer(milliseconds: widget.debounceMs);
    _hasText = _controller.text.isNotEmpty;
    _controller.addListener(_updateHasText);
  }

  @override
  void dispose() {
    _debouncer.dispose();
    if (widget.controller == null) _controller.dispose();
    super.dispose();
  }

  void _updateHasText() {
    final hasText = _controller.text.isNotEmpty;
    if (hasText != _hasText) setState(() => _hasText = hasText);
  }

  void _handleChange(String value) {
    _debouncer.run(() => widget.onChanged?.call(value));
  }

  void _clear() {
    _controller.clear();
    widget.onClear?.call();
    widget.onChanged?.call('');
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: widget.width,
      child: TextField(
        controller: _controller,
        onChanged: _handleChange,
        autofocus: widget.autofocus,
        decoration: InputDecoration(
          hintText: widget.hintText,
          prefixIcon: const Icon(Icons.search, size: 20),
          suffixIcon: _hasText
              ? IconButton(
                  icon: const Icon(Icons.close, size: 18),
                  onPressed: _clear,
                  tooltip: 'Limpiar',
                )
              : null,
        ),
      ),
    );
  }
}
