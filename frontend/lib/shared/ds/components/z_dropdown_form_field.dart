import 'package:flutter/material.dart';
import '../ds.dart';

/// A professional, fully controlled dropdown form field that avoids deprecated API usage.
class ZDropdownFormField<T> extends StatelessWidget {
  final T? value;
  final List<DropdownMenuItem<T>> items;
  final ValueChanged<T?>? onChanged;
  final String label;
  final IconData? prefixIcon;
  final FormFieldValidator<T>? validator;

  const ZDropdownFormField({
    super.key,
    required this.value,
    required this.items,
    required this.onChanged,
    required this.label,
    this.prefixIcon,
    this.validator,
  });

  @override
  Widget build(BuildContext context) {
    return FormField<T>(
      initialValue: value,
      validator: validator,
      builder: (FormFieldState<T> field) {
        final isDark = Theme.of(context).brightness == Brightness.dark;
        
        // Ensure the field state matches the controlled value
        if (field.value != value) {
          WidgetsBinding.instance.addPostFrameCallback((_) {
            field.didChange(value);
          });
        }

        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            InputDecorator(
              decoration: InputDecoration(
                labelText: label,
                prefixIcon: prefixIcon != null ? Icon(prefixIcon) : null,
                errorText: field.errorText,
                filled: true,
                fillColor: isDark ? ZColors.darkSurface : ZColors.surface,
              ),
              child: DropdownButtonHideUnderline(
                child: DropdownButton<T>(
                  value: value,
                  items: items,
                  onChanged: (T? newValue) {
                    field.didChange(newValue);
                    final handler = onChanged;
                    if (handler != null) handler(newValue);
                  },
                  isExpanded: true,
                  isDense: true,
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
