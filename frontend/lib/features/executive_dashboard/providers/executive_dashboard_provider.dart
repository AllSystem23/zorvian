import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

final class TopProductItem {
  final String productName;
  final int totalSold;
  const TopProductItem({required this.productName, required this.totalSold});
  factory TopProductItem.fromJson(Map<String, dynamic> j) => TopProductItem(
    productName: j['productName'] as String? ?? '',
    totalSold: (j['totalSold'] as num?)?.toInt() ?? 0,
  );
}

final class ExecutiveKpis {
  final double todaySales;
  final double monthSales;
  final double averageTicket;
  final int todaySalesCount;
  final int activeCredits;
  final int overdueCredits;
  final double monthlyRecovery;
  final double totalPortfolio;
  final int outOfStockProducts;
  final int lowStockProducts;
  final int totalProducts;
  final List<TopProductItem> topSelling;
  final double todayIncome;
  final double todayExpense;
  final int openRegisters;
  final int activeEmployees;
  final int pendingVacations;
  final int activePermissions;
  final int totalEmployees;

  const ExecutiveKpis({
    required this.todaySales,
    required this.monthSales,
    required this.averageTicket,
    required this.todaySalesCount,
    required this.activeCredits,
    required this.overdueCredits,
    required this.monthlyRecovery,
    required this.totalPortfolio,
    required this.outOfStockProducts,
    required this.lowStockProducts,
    required this.totalProducts,
    required this.topSelling,
    required this.todayIncome,
    required this.todayExpense,
    required this.openRegisters,
    required this.activeEmployees,
    required this.pendingVacations,
    required this.activePermissions,
    required this.totalEmployees,
  });

  factory ExecutiveKpis.fromJson(Map<String, dynamic> json) {
    final c = json['commercial'] as Map<String, dynamic>? ?? {};
    final cr = json['credits'] as Map<String, dynamic>? ?? {};
    final i = json['inventory'] as Map<String, dynamic>? ?? {};
    final ca = json['cash'] as Map<String, dynamic>? ?? {};
    final hr = json['humanResources'] as Map<String, dynamic>? ?? {};
    return ExecutiveKpis(
      todaySales: (c['todaySales'] as num?)?.toDouble() ?? 0,
      monthSales: (c['monthSales'] as num?)?.toDouble() ?? 0,
      averageTicket: (c['averageTicket'] as num?)?.toDouble() ?? 0,
      todaySalesCount: (c['todaySalesCount'] as num?)?.toInt() ?? 0,
      activeCredits: (cr['activeCredits'] as num?)?.toInt() ?? 0,
      overdueCredits: (cr['overdueCredits'] as num?)?.toInt() ?? 0,
      monthlyRecovery: (cr['monthlyRecovery'] as num?)?.toDouble() ?? 0,
      totalPortfolio: (cr['totalPortfolio'] as num?)?.toDouble() ?? 0,
      outOfStockProducts: (i['outOfStockProducts'] as num?)?.toInt() ?? 0,
      lowStockProducts: (i['lowStockProducts'] as num?)?.toInt() ?? 0,
      totalProducts: (i['totalProducts'] as num?)?.toInt() ?? 0,
      topSelling:
          (i['topSelling'] as List?)
              ?.whereType<Map<String, dynamic>>()
              .map((e) => TopProductItem.fromJson(e))
              .toList() ??
          [],
      todayIncome: (ca['todayIncome'] as num?)?.toDouble() ?? 0,
      todayExpense: (ca['todayExpense'] as num?)?.toDouble() ?? 0,
      openRegisters: (ca['openRegisters'] as num?)?.toInt() ?? 0,
      activeEmployees: (hr['activeEmployees'] as num?)?.toInt() ?? 0,
      pendingVacations: (hr['pendingVacations'] as num?)?.toInt() ?? 0,
      activePermissions: (hr['activePermissions'] as num?)?.toInt() ?? 0,
      totalEmployees: (hr['totalEmployees'] as num?)?.toInt() ?? 0,
    );
  }
}

final class ExecutiveDashboardState {
  final ExecutiveKpis? kpis;
  final bool loading;
  final String? error;
  const ExecutiveDashboardState({this.kpis, this.loading = false, this.error});
  ExecutiveDashboardState copyWith({
    ExecutiveKpis? kpis,
    bool? loading,
    String? error,
  }) => ExecutiveDashboardState(
    kpis: kpis ?? this.kpis,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

final class ExecutiveDashboardNotifier
    extends Notifier<ExecutiveDashboardState> {
  @override
  ExecutiveDashboardState build() => const ExecutiveDashboardState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('dashboard/executive');
      final data = r.data;
      state = ExecutiveDashboardState(
        kpis: data is Map<String, dynamic>
            ? ExecutiveKpis.fromJson(data)
            : const ExecutiveKpis(
                todaySales: 0,
                monthSales: 0,
                averageTicket: 0,
                todaySalesCount: 0,
                activeCredits: 0,
                overdueCredits: 0,
                monthlyRecovery: 0,
                totalPortfolio: 0,
                outOfStockProducts: 0,
                lowStockProducts: 0,
                totalProducts: 0,
                topSelling: [],
                todayIncome: 0,
                todayExpense: 0,
                openRegisters: 0,
                activeEmployees: 0,
                pendingVacations: 0,
                activePermissions: 0,
                totalEmployees: 0,
              ),
      );
    } on DioException {
      state = state.copyWith(
        error: 'No se pudo cargar el dashboard ejecutivo',
        loading: false,
      );
    } catch (_) {
      state = state.copyWith(
        error: 'Error al cargar dashboard',
        loading: false,
      );
    }
  }
}

final executiveDashboardProvider =
    NotifierProvider<ExecutiveDashboardNotifier, ExecutiveDashboardState>(
      ExecutiveDashboardNotifier.new,
    );
