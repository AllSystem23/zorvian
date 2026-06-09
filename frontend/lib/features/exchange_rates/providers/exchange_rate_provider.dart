import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class ExchangeRateState {
  final List<ExchangeRateItem> items;
  final bool loading;
  final String? error;

  const ExchangeRateState({
    this.items = const [],
    this.loading = false,
    this.error,
  });

  ExchangeRateState copyWith({
    List<ExchangeRateItem>? items,
    bool? loading,
    String? error,
  }) =>
      ExchangeRateState(
        items: items ?? this.items,
        loading: loading ?? this.loading,
        error: error ?? this.error,
      );
}

class ExchangeRateItem {
  final String id;
  final String fromCurrency;
  final String toCurrency;
  final double rate;
  final DateTime effectiveDate;

  const ExchangeRateItem({
    required this.id,
    required this.fromCurrency,
    required this.toCurrency,
    required this.rate,
    required this.effectiveDate,
  });

  factory ExchangeRateItem.fromJson(Map<String, dynamic> json) =>
      ExchangeRateItem(
        id: json['id'] as String,
        fromCurrency: json['fromCurrency'] as String? ?? '',
        toCurrency: json['toCurrency'] as String? ?? '',
        rate: (json['rate'] as num?)?.toDouble() ?? 0.0,
        effectiveDate: DateTime.parse((json['effectiveDate'] as String?) ?? ''),
      );

  String get displayRate {
    if (rate == rate.roundToDouble()) return rate.toInt().toString();
    return rate.toStringAsFixed(4);
  }

  String get formattedDate {
    return '${effectiveDate.year}-${effectiveDate.month.toString().padLeft(2, '0')}-${effectiveDate.day.toString().padLeft(2, '0')}';
  }
}

class ExchangeRateNotifier extends Notifier<ExchangeRateState> {
  @override
  ExchangeRateState build() => const ExchangeRateState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('exchange-rates');
      final data = response.data as List;
      state = ExchangeRateState(
        items: data.map((e) => ExchangeRateItem.fromJson(e)).toList(),
      );
    } catch (_) {
      state = state.copyWith(
        error: 'Error al cargar tipos de cambio',
        loading: false,
      );
    }
  }
}

final exchangeRateProvider =
    NotifierProvider<ExchangeRateNotifier, ExchangeRateState>(
  ExchangeRateNotifier.new,
);
