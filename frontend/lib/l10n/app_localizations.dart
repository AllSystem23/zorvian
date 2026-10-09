import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_es.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('es'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In es, this message translates to:
  /// **'Zorvian ERP'**
  String get appTitle;

  /// No description provided for @login.
  ///
  /// In es, this message translates to:
  /// **'Iniciar Sesión'**
  String get login;

  /// No description provided for @email.
  ///
  /// In es, this message translates to:
  /// **'Correo Electrónico'**
  String get email;

  /// No description provided for @password.
  ///
  /// In es, this message translates to:
  /// **'Contraseña'**
  String get password;

  /// No description provided for @welcome.
  ///
  /// In es, this message translates to:
  /// **'Bienvenido a Zorvian ERP'**
  String get welcome;

  /// No description provided for @employees.
  ///
  /// In es, this message translates to:
  /// **'Trabajadores'**
  String get employees;

  /// No description provided for @payroll.
  ///
  /// In es, this message translates to:
  /// **'Nómina'**
  String get payroll;

  /// No description provided for @vacations.
  ///
  /// In es, this message translates to:
  /// **'Vacaciones'**
  String get vacations;

  /// No description provided for @attendance.
  ///
  /// In es, this message translates to:
  /// **'Asistencia'**
  String get attendance;

  /// No description provided for @settings.
  ///
  /// In es, this message translates to:
  /// **'Configuración'**
  String get settings;

  /// No description provided for @common_save.
  ///
  /// In es, this message translates to:
  /// **'Guardar'**
  String get common_save;

  /// No description provided for @common_cancel.
  ///
  /// In es, this message translates to:
  /// **'Cancelar'**
  String get common_cancel;

  /// No description provided for @common_delete.
  ///
  /// In es, this message translates to:
  /// **'Eliminar'**
  String get common_delete;

  /// No description provided for @common_edit.
  ///
  /// In es, this message translates to:
  /// **'Editar'**
  String get common_edit;

  /// No description provided for @common_create.
  ///
  /// In es, this message translates to:
  /// **'Crear'**
  String get common_create;

  /// No description provided for @common_search.
  ///
  /// In es, this message translates to:
  /// **'Buscar'**
  String get common_search;

  /// No description provided for @common_filter.
  ///
  /// In es, this message translates to:
  /// **'Filtrar'**
  String get common_filter;

  /// No description provided for @common_clear.
  ///
  /// In es, this message translates to:
  /// **'Limpiar'**
  String get common_clear;

  /// No description provided for @common_apply.
  ///
  /// In es, this message translates to:
  /// **'Aplicar'**
  String get common_apply;

  /// No description provided for @common_close.
  ///
  /// In es, this message translates to:
  /// **'Cerrar'**
  String get common_close;

  /// No description provided for @common_yes.
  ///
  /// In es, this message translates to:
  /// **'Sí'**
  String get common_yes;

  /// No description provided for @common_no.
  ///
  /// In es, this message translates to:
  /// **'No'**
  String get common_no;

  /// No description provided for @common_confirm.
  ///
  /// In es, this message translates to:
  /// **'Confirmar'**
  String get common_confirm;

  /// No description provided for @common_loading.
  ///
  /// In es, this message translates to:
  /// **'Cargando...'**
  String get common_loading;

  /// No description provided for @common_no_data.
  ///
  /// In es, this message translates to:
  /// **'No hay datos disponibles'**
  String get common_no_data;

  /// No description provided for @common_error.
  ///
  /// In es, this message translates to:
  /// **'Ocurrió un error'**
  String get common_error;

  /// No description provided for @common_retry.
  ///
  /// In es, this message translates to:
  /// **'Reintentar'**
  String get common_retry;

  /// No description provided for @common_required_field.
  ///
  /// In es, this message translates to:
  /// **'Este campo es obligatorio'**
  String get common_required_field;

  /// No description provided for @common_optional.
  ///
  /// In es, this message translates to:
  /// **'Opcional'**
  String get common_optional;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'es'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'es':
      return AppLocalizationsEs();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
