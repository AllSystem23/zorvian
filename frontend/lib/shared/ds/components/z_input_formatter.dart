import 'package:flutter/services.dart';

/// Input formatters for common field types
class ZInputFormatters {
  ZInputFormatters._();

  /// Formatter for currency/numbers (allows decimals)
  static TextInputFormatter currency() {
    return FilteringTextInputFormatter.allow(RegExp(r'^\d*\.?\d{0,2}$'));
  }

  /// Formatter for integer numbers
  static TextInputFormatter integer() {
    return FilteringTextInputFormatter.digitsOnly;
  }

  /// Formatter for phone numbers
  static TextInputFormatter phone() {
    return FilteringTextInputFormatter.allow(RegExp(r'[\d\s\-\+\(\)]'));
  }

  /// Formatter for letters only
  static TextInputFormatter letters() {
    return FilteringTextInputFormatter.allow(RegExp(r'[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]'));
  }

  /// Formatter for alphanumeric
  static TextInputFormatter alphanumeric() {
    return FilteringTextInputFormatter.allow(RegExp(r'[a-zA-Z0-9]'));
  }

  /// Formatter for emails (basic)
  static TextInputFormatter email() {
    return FilteringTextInputFormatter.allow(RegExp(r'[a-zA-Z0-9@._\-\+]'));
  }

  /// Uppercase formatter
  static TextInputFormatter uppercase() {
    return TextInputFormatter.withFunction((oldValue, newValue) {
      return newValue.copyWith(text: newValue.text.toUpperCase());
    });
  }

  /// Limit text length
  static TextInputFormatter maxLength(int max) {
    return LengthLimitingTextInputFormatter(max);
  }
}
