import 'package:flutter/material.dart';
import 'package:zorvian/core/utils/country_config.dart';
import '../ds.dart';

/// Info card showing country-specific configuration
class ZCountryInfoCard extends StatelessWidget {
  final String countryCode;
  final bool compact;

  const ZCountryInfoCard({
    super.key,
    required this.countryCode,
    this.compact = false,
  });

  @override
  Widget build(BuildContext context) {
    final country = CountryConfig.getCountry(countryCode);
    if (country == null) {
      return ZEmptyState(
        icon: Icons.error_outline,
        title: 'País no encontrado',
        subtitle: 'Código: $countryCode',
      );
    }

    final theme = Theme.of(context);

    if (compact) {
      return _buildCompactCard(theme, country);
    }
    return _buildFullCard(theme, country);
  }

  Widget _buildCompactCard(ThemeData theme, CountryConfig country) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.md, vertical: ZSpacing.sm),
      decoration: BoxDecoration(
        color: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: theme.colorScheme.primary.withValues(alpha: 0.3)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(country.name, style: theme.textTheme.bodySmall?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(width: ZSpacing.sm),
          Text('${country.currencySymbol} ${country.currency}', style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
        ],
      ),
    );
  }

  Widget _buildFullCard(ThemeData theme, CountryConfig country) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Container(
                  width: 48, height: 48,
                  decoration: BoxDecoration(
                    color: theme.colorScheme.primaryContainer,
                    borderRadius: BorderRadius.circular(ZRadii.md),
                  ),
                  child: Icon(Icons.public, color: theme.colorScheme.primary, size: 24),
                ),
                const SizedBox(width: ZSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(country.name, style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
                      Text('${country.code} · ${country.timezone}', style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: ZSpacing.lg),
            const Divider(),
            const SizedBox(height: ZSpacing.md),
            _buildInfoRow(theme, Icons.attach_money, 'Moneda', '${country.name} (${country.currencyCode})'),
            _buildInfoRow(theme, Icons.badge, 'Identificación', '${country.idType} (${country.idLength} dígitos)'),
            _buildInfoRow(theme, Icons.beach_access, 'Vacaciones', '${country.vacationDaysPerYear} días al año'),
            _buildInfoRow(theme, Icons.child_care, 'Maternidad', '${country.maternityWeeks} semanas'),
            _buildInfoRow(theme, Icons.family_restroom, 'Paternidad', '${country.paternityDays} días hábiles'),
            _buildInfoRow(theme, Icons.account_balance, 'Seguridad Social', country.taxSystem),
            _buildInfoRow(theme, Icons.phone, 'Teléfono', country.phonePrefix),
          ],
        ),
      ),
    );
  }

  Widget _buildInfoRow(ThemeData theme, IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: ZSpacing.xs),
      child: Row(
        children: [
          Icon(icon, size: 18, color: ZColors.neutral500),
          const SizedBox(width: ZSpacing.sm),
          Text('$label:', style: theme.textTheme.bodyMedium?.copyWith(color: ZColors.neutral500)),
          const Spacer(),
          Text(value, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

extension on CountryConfig {
  String get currencyCode => currency;
}