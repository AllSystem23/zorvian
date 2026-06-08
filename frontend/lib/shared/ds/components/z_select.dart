import 'package:flutter/material.dart';
import '../ds.dart';

class ZSelect<T> extends StatelessWidget {
  final T? value;
  final List<DropdownMenuItem<T>> items;
  final String label;
  final String? hint;
  final ValueChanged<T?>? onChanged;
  final FormFieldValidator<T>? validator;

  const ZSelect({
    super.key,
    this.value,
    required this.items,
    required this.label,
    this.hint,
    this.onChanged,
    this.validator,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (label.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(bottom: ZSpacing.xs),
            child: Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: Theme.of(context).brightness == Brightness.dark ? ZColors.neutral300 : ZColors.neutral600,
            )),
          ),
        DropdownButtonFormField<T>(
          initialValue: value,
          hint: hint != null ? Text(hint!) : null,
          items: items,
          onChanged: onChanged,
          validator: validator,
          decoration: InputDecoration(
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
            contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.md),
          ),
        ),
      ],
    );
  }
}
