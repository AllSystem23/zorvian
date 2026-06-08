import 'package:flutter/material.dart';
import '../ds.dart';

class ZTextField extends StatelessWidget {
  final TextEditingController? controller;
  final String label;
  final String? hint;
  final FormFieldValidator<String>? validator;
  final TextInputType keyboardType;
  final bool obscureText;
  final int maxLines;
  final Widget? prefix;
  final Widget? suffix;

  const ZTextField({
    super.key,
    this.controller,
    required this.label,
    this.hint,
    this.validator,
    this.keyboardType = TextInputType.text,
    this.obscureText = false,
    this.maxLines = 1,
    this.prefix,
    this.suffix,
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
        TextFormField(
          controller: controller,
          validator: validator,
          keyboardType: keyboardType,
          obscureText: obscureText,
          maxLines: maxLines,
          decoration: InputDecoration(
            hintText: hint,
            prefixIcon: prefix != null ? Padding(padding: const EdgeInsets.only(left: ZSpacing.md, right: ZSpacing.sm), child: prefix) : null,
            suffixIcon: suffix != null ? Padding(padding: const EdgeInsets.only(right: ZSpacing.md), child: suffix) : null,
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(ZRadii.md)),
            contentPadding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.md),
          ),
        ),
      ],
    );
  }
}
