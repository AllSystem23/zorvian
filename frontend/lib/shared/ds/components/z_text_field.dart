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
  final ValueChanged<String>? onChanged;
  final FocusNode? focusNode;

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
    this.onChanged,
    this.focusNode,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (label.isNotEmpty)
          Padding(
            padding: const EdgeInsets.only(bottom: 8, left: 4),
            child: Text(
              label.toUpperCase(),
              style: ZTypography.labelSmall.copyWith(
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                letterSpacing: 1.2,
              ),
            ),
          ),
        TextFormField(
          controller: controller,
          focusNode: focusNode,
          validator: validator,
          keyboardType: keyboardType,
          obscureText: obscureText,
          maxLines: maxLines,
          onChanged: onChanged,
          style: ZTypography.bodyMedium.copyWith(
            color: isDark ? Colors.white : ZColors.neutral900,
          ),
          decoration: InputDecoration(
            hintText: hint,
            hintStyle: ZTypography.bodyMedium.copyWith(
              color: isDark ? ZColors.neutral600 : ZColors.neutral400,
            ),
            prefixIcon: prefix != null
                ? IconTheme(
                    data: IconThemeData(
                      color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                      size: 20,
                    ),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 16),
                      child: prefix,
                    ),
                  )
                : null,
            suffixIcon: suffix != null
                ? IconTheme(
                    data: IconThemeData(
                      color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                      size: 20,
                    ),
                    child: Padding(
                      padding: const EdgeInsets.only(right: 12),
                      child: suffix,
                    ),
                  )
                : null,
            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 18),
            // The actual border shape is handled by the theme, 
            // but we ensure it stays consistent here if needed.
          ),
        ),
      ],
    );
  }
}
