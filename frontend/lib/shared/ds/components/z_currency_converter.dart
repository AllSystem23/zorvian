import 'package:flutter/material.dart';
import 'package:zorvian/core/utils/country_config.dart';
import 'package:zorvian/core/utils/formatters.dart';
import '../ds.dart';

/// Currency converter widget with real-time conversion
class ZCurrencyConverter extends StatefulWidget {
  final double initialAmount;
  final String initialFrom;
  final String initialTo;

  const ZCurrencyConverter({
    super.key,
    this.initialAmount = 100,
    this.initialFrom = 'USD',
    this.initialTo = 'NIO',
  });

  @override
  State<ZCurrencyConverter> createState() => _ZCurrencyConverterState();
}

class _ZCurrencyConverterState extends State<ZCurrencyConverter> {
  late TextEditingController _amountController;
  late String _fromCurrency;
  late String _toCurrency;

  // Static exchange rates (demo - would come from API in production)
  static const Map<String, double> _toUSD = {
    'NIO': 0.027, 'CRC': 0.0019, 'GTQ': 0.129, 'HNL': 0.041,
    'SVC': 0.114, 'PAB': 1.0, 'USD': 1.0,
  };

  @override
  void initState() {
    super.initState();
    _amountController = TextEditingController(text: widget.initialAmount.toString());
    _fromCurrency = widget.initialFrom;
    _toCurrency = widget.initialTo;
  }

  @override
  void dispose() {
    _amountController.dispose();
    super.dispose();
  }

  double get _convertedAmount {
    final amount = double.tryParse(_amountController.text) ?? 0;
    final fromUSD = _toUSD[_fromCurrency] ?? 1.0;
    final toUSD = _toUSD[_toCurrency] ?? 1.0;
    return (amount / fromUSD) * toUSD;
  }

  void _swap() {
    setState(() {
      final tmp = _fromCurrency;
      _fromCurrency = _toCurrency;
      _toCurrency = tmp;
    });
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final allCurrencies = CountryConfig.allCountries.map((c) => c.currency).toSet().toList()..sort();

    return Container(
      padding: const EdgeInsets.all(ZSpacing.lg),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        border: Border.all(color: theme.colorScheme.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Convertidor de Moneda', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w600)),
          const SizedBox(height: ZSpacing.lg),
          Row(
            children: [
              Expanded(
                flex: 5,
                child: ZTextField(
                  controller: _amountController,
                  label: 'Monto',
                  keyboardType: TextInputType.number,
                  onChanged: (_) => setState(() {}),
                ),
              ),
              const SizedBox(width: ZSpacing.md),
              Expanded(
                flex: 4,
                child: _buildCurrencyDropdown(allCurrencies, _fromCurrency, (v) => setState(() => _fromCurrency = v!)),
              ),
            ],
          ),
          const SizedBox(height: ZSpacing.md),
          Center(
            child: IconButton(
              icon: const Icon(Icons.swap_vert, size: 28),
              tooltip: 'Intercambiar',
              onPressed: _swap,
            ),
          ),
          const SizedBox(height: ZSpacing.md),
          Row(
            children: [
              Expanded(flex: 5, child: _buildResultField(theme)),
              const SizedBox(width: ZSpacing.md),
              Expanded(
                flex: 4,
                child: _buildCurrencyDropdown(allCurrencies, _toCurrency, (v) => setState(() => _toCurrency = v!)),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildCurrencyDropdown(List<String> currencies, String current, ValueChanged<String?> onChanged) {
    return DropdownButtonFormField<String>(
      initialValue: current,
      decoration: InputDecoration(
        labelText: 'Moneda',
        prefixIcon: const Icon(Icons.attach_money, size: 20),
      ),
      items: currencies.map((c) {
        final ctry = CountryConfig.countries.values
            .firstWhere((co) => co.currency == c, orElse: () => CountryConfig.countries['NI']!);
        return DropdownMenuItem(value: c, child: Text('${ctry.currencySymbol} $c'));
      }).toList(),
      onChanged: onChanged,
    );
  }

  Widget _buildResultField(ThemeData theme) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 16),
      decoration: BoxDecoration(
        color: theme.colorScheme.primaryContainer.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(color: theme.colorScheme.primary.withValues(alpha: 0.3)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Resultado', style: theme.textTheme.bodySmall?.copyWith(color: ZColors.neutral500)),
          const SizedBox(height: 4),
          Text(
            ZFormatters.currency(_convertedAmount, currencyCode: _toCurrency),
            style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold, color: theme.colorScheme.primary),
          ),
        ],
      ),
    );
  }
}
