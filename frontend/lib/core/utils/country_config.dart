/// Country-specific configuration for Zorvian ERP
class CountryConfig {
  final String code;
  final String name;
  final String currency;
  final String currencySymbol;
  final String timezone;
  final String phonePrefix;
  final String idType; // Cédula, RUC, DPI, etc.
  final int idLength;
  final int vacationDaysPerYear;
  final int maternityWeeks;
  final int paternityDays;
  final String taxSystem; // INSS, CCSS, IGSS, etc.

  const CountryConfig({
    required this.code,
    required this.name,
    required this.currency,
    required this.currencySymbol,
    required this.timezone,
    required this.phonePrefix,
    required this.idType,
    required this.idLength,
    required this.vacationDaysPerYear,
    required this.maternityWeeks,
    required this.paternityDays,
    required this.taxSystem,
  });

  static const Map<String, CountryConfig> countries = {
    'NI': CountryConfig(
      code: 'NI', name: 'Nicaragua', currency: 'NIO', currencySymbol: 'C\$',
      timezone: 'America/Managua', phonePrefix: '+505', idType: 'Cédula', idLength: 14,
      vacationDaysPerYear: 15, maternityWeeks: 12, paternityDays: 5, taxSystem: 'INSS',
    ),
    'CR': CountryConfig(
      code: 'CR', name: 'Costa Rica', currency: 'CRC', currencySymbol: '₡',
      timezone: 'America/Costa_Rica', phonePrefix: '+506', idType: 'Cédula', idLength: 10,
      vacationDaysPerYear: 15, maternityWeeks: 16, paternityDays: 5, taxSystem: 'CCSS',
    ),
    'GT': CountryConfig(
      code: 'GT', name: 'Guatemala', currency: 'GTQ', currencySymbol: 'Q',
      timezone: 'America/Guatemala', phonePrefix: '+502', idType: 'DPI', idLength: 13,
      vacationDaysPerYear: 15, maternityWeeks: 12, paternityDays: 2, taxSystem: 'IGSS',
    ),
    'HN': CountryConfig(
      code: 'HN', name: 'Honduras', currency: 'HNL', currencySymbol: 'L',
      timezone: 'America/Tegucigalpa', phonePrefix: '+504', idType: 'ID', idLength: 13,
      vacationDaysPerYear: 20, maternityWeeks: 12, paternityDays: 5, taxSystem: 'IHSS',
    ),
    'SV': CountryConfig(
      code: 'SV', name: 'El Salvador', currency: 'SVC', currencySymbol: '\$',
      timezone: 'America/El_Salvador', phonePrefix: '+503', idType: 'DUI', idLength: 9,
      vacationDaysPerYear: 15, maternityWeeks: 12, paternityDays: 5, taxSystem: 'ISSS',
    ),
    'PA': CountryConfig(
      code: 'PA', name: 'Panamá', currency: 'PAB', currencySymbol: 'B/.',
      timezone: 'America/Panama', phonePrefix: '+507', idType: 'Cédula', idLength: 8,
      vacationDaysPerYear: 15, maternityWeeks: 14, paternityDays: 3, taxSystem: 'CSS',
    ),
  };

  static CountryConfig? getCountry(String code) => countries[code.toUpperCase()];
  static List<CountryConfig> get allCountries => countries.values.toList();
}