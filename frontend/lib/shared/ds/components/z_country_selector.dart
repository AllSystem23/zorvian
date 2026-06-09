import 'package:flutter/material.dart';
import 'package:zorvian/core/utils/country_config.dart';
import '../ds.dart';

/// Reusable country selector widget
class ZCountrySelector extends StatelessWidget {
  final String? selectedCountry;
  final ValueChanged<String> onChanged;
  final String? label;
  final bool showFlag;

  const ZCountrySelector({
    super.key,
    required this.selectedCountry,
    required this.onChanged,
    this.label = 'País',
    this.showFlag = true,
  });

  @override
  Widget build(BuildContext context) {
    return DropdownButtonFormField<String>(
      initialValue: selectedCountry,
      decoration: InputDecoration(
        labelText: label,
        prefixIcon: const Icon(Icons.public, size: 20),
      ),
      items: CountryConfig.allCountries.map((country) {
        return DropdownMenuItem(
          value: country.code,
          child: Row(
            children: [
              if (showFlag) ...[
                Text(_flagEmoji(country.code), style: const TextStyle(fontSize: 18)),
                const SizedBox(width: ZSpacing.sm),
              ],
              Text('${country.name} (${country.code})'),
            ],
          ),
        );
      }).toList(),
      onChanged: (value) {
        if (value != null) onChanged(value);
      },
    );
  }

  String _flagEmoji(String countryCode) {
    // Convert country code to regional indicator symbols
    final codePoints = countryCode.toUpperCase().codeUnits;
    return String.fromCharCode(0x1F1E6 + codePoints[0] - 0x41) +
        String.fromCharCode(0x1F1E6 + codePoints[1] - 0x41);
  }
}