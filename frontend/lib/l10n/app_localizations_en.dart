// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Zorvian ERP';

  @override
  String get login => 'Iniciar Sesión';

  @override
  String get email => 'Correo Electrónico';

  @override
  String get password => 'Contraseña';

  @override
  String get welcome => 'Bienvenido a Zorvian ERP';

  @override
  String get employees => 'Empleados';

  @override
  String get payroll => 'Nómina';

  @override
  String get vacations => 'Vacaciones';

  @override
  String get attendance => 'Asistencia';

  @override
  String get settings => 'Configuración';
}
