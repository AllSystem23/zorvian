/// Reusable validation functions for forms
class ZValidators {
  ZValidators._();

  /// Required field validator
  static String? required(String? value, {String fieldName = 'Este campo'}) {
    if (value == null || value.trim().isEmpty) {
      return '$fieldName es requerido';
    }
    return null;
  }

  /// Email validator
  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'El correo es requerido';
    }
    final regex = RegExp(r'^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$');
    if (!regex.hasMatch(value.trim())) {
      return 'Ingrese un correo válido';
    }
    return null;
  }

  /// Phone validator (Nicaragua format)
  static String? phone(String? value) {
    if (value == null || value.trim().isEmpty) {
      return 'El teléfono es requerido';
    }
    final digits = value.replaceAll(RegExp(r'\D'), '');
    if (digits.length < 7 || digits.length > 11) {
      return 'Ingrese un teléfono válido';
    }
    return null;
  }

  /// Password validator
  static String? password(String? value, {int minLength = 8}) {
    if (value == null || value.isEmpty) {
      return 'La contraseña es requerida';
    }
    if (value.length < minLength) {
      return 'Mínimo $minLength caracteres';
    }
    if (!value.contains(RegExp(r'[A-Z]'))) {
      return 'Debe incluir al menos una mayúscula';
    }
    if (!value.contains(RegExp(r'[0-9]'))) {
      return 'Debe incluir al menos un número';
    }
    return null;
  }

  /// Minimum length validator
  static String? minLength(String? value, int min, {String fieldName = 'Este campo'}) {
    if (value == null || value.trim().isEmpty) {
      return '$fieldName es requerido';
    }
    if (value.trim().length < min) {
      return '$fieldName debe tener al menos $min caracteres';
    }
    return null;
  }

  /// Maximum length validator
  static String? maxLength(String? value, int max, {String fieldName = 'Este campo'}) {
    if (value != null && value.trim().length > max) {
      return '$fieldName no debe exceder $max caracteres';
    }
    return null;
  }

  /// Number validator
  static String? number(String? value, {String fieldName = 'Este campo'}) {
    if (value == null || value.trim().isEmpty) {
      return '$fieldName es requerido';
    }
    if (double.tryParse(value.trim()) == null) {
      return '$fieldName debe ser un número válido';
    }
    return null;
  }

  /// Positive number validator
  static String? positiveNumber(String? value, {String fieldName = 'Este campo'}) {
    final numError = number(value, fieldName: fieldName);
    if (numError != null) return numError;
    if (double.parse(value!) <= 0) {
      return '$fieldName debe ser mayor a 0';
    }
    return null;
  }

  /// Date validator (not in the future)
  static String? dateNotFuture(DateTime? value, {String fieldName = 'La fecha'}) {
    if (value == null) {
      return '$fieldName es requerida';
    }
    if (value.isAfter(DateTime.now())) {
      return '$fieldName no puede ser en el futuro';
    }
    return null;
  }

  /// Date range validator
  static String? dateRange(DateTime? start, DateTime? end, {String startField = 'Fecha inicio', String endField = 'Fecha fin'}) {
    if (start == null) return '$startField es requerida';
    if (end == null) return '$endField es requerida';
    if (end.isBefore(start)) return '$endField debe ser posterior a $startField';
    return null;
  }

  /// RUC/Cédula validator with country support
  static String? identification(String? value, {String? country}) {
    if (value == null || value.trim().isEmpty) {
      return 'La identificación es requerida';
    }
    final digits = value.replaceAll(RegExp(r'\D'), '');

    switch (country?.toUpperCase()) {
      case 'PA': // Panamá - Cédula: 8 dígitos, RUC: 10 dígitos
        if (digits.length != 8 && digits.length != 10) {
          return 'Ingrese un número de identificación válido (8 o 10 dígitos)';
        }
        return null;
      case 'NI': // Nicaragua - Cédula: 14 dígitos, RUC: 13 dígitos
        if (digits.length != 13 && digits.length != 14) {
          return 'Ingrese un número de identificación válido (13 o 14 dígitos)';
        }
        return null;
      case 'CR': // Costa Rica - Cédula: 9-12 dígitos
        if (digits.length < 9 || digits.length > 12) {
          return 'Ingrese un número de cédula válido (9 a 12 dígitos)';
        }
        return null;
      case 'GT': // Guatemala - DPI: 13 dígitos
        if (digits.length != 13) {
          return 'Ingrese un número de DPI válido (13 dígitos)';
        }
        return null;
      case 'HN': // Honduras - ID: 13 dígitos
        if (digits.length != 13) {
          return 'Ingrese un número de identidad válido (13 dígitos)';
        }
        return null;
      case 'SV': // El Salvador - DUI: 9 dígitos
        if (digits.length != 9) {
          return 'Ingrese un número de DUI válido (9 dígitos)';
        }
        return null;
      default:
        // Generic: 8-14 digits
        if (digits.length < 8 || digits.length > 14) {
          return 'Ingrese un número de identificación válido (8 a 14 dígitos)';
        }
        return null;
    }
  }

  /// Compose multiple validators
  static String? compose(String? value, List<String? Function(String?)> validators) {
    for (final validator in validators) {
      final error = validator(value);
      if (error != null) return error;
    }
    return null;
  }
}