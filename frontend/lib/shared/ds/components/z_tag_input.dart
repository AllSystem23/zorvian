import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../ds.dart';

/// Tag input field with chips
class ZTagInput extends StatefulWidget {
  final List<String> initialTags;
  final ValueChanged<List<String>> onChanged;
  final String hintText;
  final List<String>? suggestions;
  final int maxTags;
  final bool allowDuplicates;
  final IconData? icon;

  const ZTagInput({
    super.key,
    this.initialTags = const [],
    required this.onChanged,
    this.hintText = 'Agregar etiqueta...',
    this.suggestions,
    this.maxTags = 20,
    this.allowDuplicates = false,
    this.icon,
  });

  @override
  State<ZTagInput> createState() => _ZTagInputState();
}

class _ZTagInputState extends State<ZTagInput> {
  late List<String> _tags;
  final TextEditingController _controller = TextEditingController();
  final FocusNode _focusNode = FocusNode();

  @override
  void initState() {
    super.initState();
    _tags = List.from(widget.initialTags);
  }

  @override
  void dispose() {
    _controller.dispose();
    _focusNode.dispose();
    super.dispose();
  }

  void _addTag(String value) {
    final tag = value.trim();
    if (tag.isEmpty) return;
    if (_tags.length >= widget.maxTags) {
      _showSnack('Máximo ${widget.maxTags} etiquetas');
      return;
    }
    if (!widget.allowDuplicates && _tags.contains(tag.toLowerCase())) {
      _showSnack('Etiqueta duplicada');
      return;
    }
    setState(() {
      _tags.add(tag);
      _controller.clear();
    });
    widget.onChanged(_tags);
  }

  void _removeTag(String tag) {
    setState(() => _tags.remove(tag));
    widget.onChanged(_tags);
  }

  void _showSnack(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(message), duration: const Duration(seconds: 2)));
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (_tags.isNotEmpty) ...[
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: _tags.map((tag) => _buildChip(theme, tag)).toList(),
            ),
            const SizedBox(height: ZSpacing.sm),
            const Divider(height: 1),
            const SizedBox(height: ZSpacing.sm),
          ],
          Row(
            children: [
              if (widget.icon != null) ...[
                Icon(widget.icon, size: 18, color: ZColors.neutral500),
                const SizedBox(width: 8),
              ],
              Expanded(
                child: TextField(
                  controller: _controller,
                  focusNode: _focusNode,
                  decoration: InputDecoration(
                    hintText: widget.hintText,
                    border: InputBorder.none,
                    enabledBorder: InputBorder.none,
                    focusedBorder: InputBorder.none,
                    filled: false,
                    contentPadding: EdgeInsets.zero,
                    isDense: true,
                  ),
                  inputFormatters: [
                    FilteringTextInputFormatter.deny(RegExp(r'\s')),
                  ],
                  onSubmitted: _addTag,
                ),
              ),
              IconButton(
                icon: const Icon(Icons.add_circle, size: 22),
                color: theme.colorScheme.primary,
                onPressed: () => _addTag(_controller.text),
                tooltip: 'Agregar',
              ),
            ],
          ),
          if (widget.suggestions != null && widget.suggestions!.isNotEmpty) ...[
            const SizedBox(height: ZSpacing.sm),
            Text('Sugerencias:', style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
            const SizedBox(height: 4),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: widget.suggestions!
                  .where((s) => !_tags.contains(s.toLowerCase()))
                  .take(8)
                  .map((s) => ActionChip(
                        label: Text(s, style: const TextStyle(fontSize: 12)),
                        onPressed: () => _addTag(s),
                      ))
                  .toList(),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildChip(ThemeData theme, String tag) {
    return InputChip(
      label: Text(tag, style: const TextStyle(fontSize: 12)),
      onDeleted: () => _removeTag(tag),
      deleteIcon: const Icon(Icons.close, size: 14),
      visualDensity: VisualDensity.compact,
    );
  }
}
