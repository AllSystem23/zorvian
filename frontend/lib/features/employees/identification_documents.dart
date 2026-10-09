/// Catálogo de documentos de identidad de Centroamérica.
///
/// Cada tipo define un código estable (se persiste en
/// `Employee.IdentificationType`), una etiqueta para la UI y un patrón de
/// formato (RegularExpression). El patrón debe mantenerse en sincronía con
/// `CreateEmployeeValidator`/`UpdateEmployeeValidator` en el backend
/// (`src/Zorvian.Application/Validators/EmployeeValidators.cs`).
class IdentificationDocumentType {
  final String code;
  final String label;
  final String hint;
  final RegExp? pattern;
  final String patternError;

  const IdentificationDocumentType({
    required this.code,
    required this.label,
    required this.hint,
    this.pattern,
    this.patternError = 'Formato inválido',
  });

  bool isValid(String value) => pattern == null || pattern!.hasMatch(value);
}

const kIdentificationTypeOther = 'otro';

final List<IdentificationDocumentType> kIdentificationDocumentTypes = [
  IdentificationDocumentType(
    code: 'cedula_ni',
    label: 'Cédula (Nicaragua)',
    hint: '000-000000-0000X',
    pattern: RegExp(r'^\d{3}-\d{6}-\d{4}[A-Za-z]$'),
    patternError: 'Cédula nicaragüense inválida (use 000-000000-0000X)',
  ),
  IdentificationDocumentType(
    code: 'nit_ni',
    label: 'NIT (Nicaragua)',
    hint: '0000-000000-000-0',
    pattern: RegExp(r'^\d{4}-\d{6}-\d{3}-\d$'),
    patternError: 'NIT nicaragüense inválido (use 0000-000000-000-0)',
  ),
  IdentificationDocumentType(
    code: 'cedula_cr',
    label: 'Cédula (Costa Rica)',
    hint: '000000000',
    pattern: RegExp(r'^\d{9}$'),
    patternError: 'La cédula costarricense debe tener 9 dígitos',
  ),
  IdentificationDocumentType(
    code: 'cedula_pa',
    label: 'Cédula (Panamá)',
    hint: '81234567',
    pattern: RegExp(r'^\d{7,8}[A-Za-z]?$'),
    patternError: 'La cédula panameña debe tener 7 u 8 dígitos',
  ),
  IdentificationDocumentType(
    code: 'dui_sv',
    label: 'DUI (El Salvador)',
    hint: '00000000-9',
    pattern: RegExp(r'^\d{8}-?\d$'),
    patternError: 'El DUI salvadoreño debe ser 00000000-9',
  ),
  IdentificationDocumentType(
    code: 'nit_sv',
    label: 'NIT (El Salvador)',
    hint: '06140000000000',
    pattern: RegExp(r'^\d{14}$'),
    patternError: 'El NIT salvadoreño debe tener 14 dígitos',
  ),
  IdentificationDocumentType(
    code: 'dpi_gt',
    label: 'DPI (Guatemala)',
    hint: '1234567890123',
    pattern: RegExp(r'^\d{13}$'),
    patternError: 'El DPI guatemalteco debe tener 13 dígitos',
  ),
  IdentificationDocumentType(
    code: 'nit_gt',
    label: 'NIT (Guatemala)',
    hint: '1234-567890-123',
    pattern: RegExp(r'^\d{4}-?\d{6}-?\d{3}$'),
    patternError: 'El NIT guatemalteco debe ser 0000-000000-000',
  ),
  IdentificationDocumentType(
    code: 'dui_hn',
    label: 'DUI (Honduras)',
    hint: '0801199012345',
    pattern: RegExp(r'^\d{13}$'),
    patternError: 'El DUI hondureño debe tener 13 dígitos',
  ),
  IdentificationDocumentType(
    code: 'rtn_hn',
    label: 'RTN (Honduras)',
    hint: '08019901234567',
    pattern: RegExp(r'^\d{14}$'),
    patternError: 'El RTN hondureño debe tener 14 dígitos',
  ),
  IdentificationDocumentType(
    code: 'pasaporte',
    label: 'Pasaporte',
    hint: 'A1234567',
    pattern: RegExp(r'^[A-Za-z0-9-]{6,20}$'),
    patternError: 'Pasaporte inválido (6 a 20 caracteres)',
  ),
  IdentificationDocumentType(
    code: kIdentificationTypeOther,
    label: 'Otro documento',
    hint: 'Número de documento',
  ),
];

IdentificationDocumentType? identificationDocumentByCode(String? code) {
  for (final t in kIdentificationDocumentTypes) {
    if (t.code == code) return t;
  }
  return null;
}

/// Valida el número de documento según el tipo seleccionado.
/// Devuelve el mensaje de error, o `null` si es válido.
String? validateIdentificationNumber(String typeCode, String? value) {
  final v = value?.trim() ?? '';
  if (v.isEmpty) return 'Requerido';
  if (v.length > 30) return 'Máximo 30 caracteres';
  final type = identificationDocumentByCode(typeCode);
  if (type != null && !type.isValid(v)) return type.patternError;
  return null;
}
