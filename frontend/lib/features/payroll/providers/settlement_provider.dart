import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

/// Settlement calculation result model
class SettlementResult {
  final String id;
  final String employeeId;
  final String reason;
  final DateTime terminationDate;
  final DateTime hireDate;
  final int daysWorked;
  final double monthsWorked;
  final double yearsWorked;
  final double monthlySalary;
  final double dailySalary;

  // Prestaciones
  final double aguinaldoPay;
  final double vacationDaysAccrued;
  final double vacationDaysTaken;
  final double vacationDaysToPay;
  final double vacationPay;
  final double severanceDays;
  final double severancePay;
  final bool isTrustPosition;
  final double trustPositionPay;

  // Salario pendiente
  final int pendingSalaryDays;
  final double pendingSalaryPay;

  // Horas extras
  final double overtimeHours;
  final double overtimePay;

  // Deducciones
  final double inssLaboralAmount;
  final double irSalaryAmount;
  final double irTotalAmount;

  // Costos patronales
  final double inssPatronalAmount;
  final double inatecAmount;

  // Totales
  final double grossSettlement;
  final double totalDeductions;
  final double netSettlement;

  // Multi-country info
  final String countryCode;
  final String currency;
  final String countryName;
  final double inssEmployeeRate;
  final double inssEmployerRate;
  final double otherEmployerRate;
  final String otherEmployerName;

  // Computed helpers
  double get irOnVacation => irTotalAmount - irSalaryAmount;
  String get currencySymbol => _currencySymbols[countryCode] ?? currency;

  SettlementResult({
    required this.id,
    required this.employeeId,
    required this.reason,
    required this.terminationDate,
    required this.hireDate,
    required this.daysWorked,
    required this.monthsWorked,
    required this.yearsWorked,
    required this.monthlySalary,
    required this.dailySalary,
    required this.aguinaldoPay,
    required this.vacationDaysAccrued,
    required this.vacationDaysTaken,
    required this.vacationDaysToPay,
    required this.vacationPay,
    required this.severanceDays,
    required this.severancePay,
    required this.isTrustPosition,
    required this.trustPositionPay,
    required this.pendingSalaryDays,
    required this.pendingSalaryPay,
    required this.overtimeHours,
    required this.overtimePay,
    required this.inssLaboralAmount,
    required this.irSalaryAmount,
    required this.irTotalAmount,
    required this.inssPatronalAmount,
    required this.inatecAmount,
    required this.grossSettlement,
    required this.totalDeductions,
    required this.netSettlement,
    required this.countryCode,
    required this.currency,
    required this.countryName,
    required this.inssEmployeeRate,
    required this.inssEmployerRate,
    required this.otherEmployerRate,
    required this.otherEmployerName,
  });

  static const _currencySymbols = {
    'NIC': 'C\$',
    'HND': 'L',
    'SLV': 'US\$',
    'GTM': 'Q',
    'CRI': '₡',
    'PAN': 'B/.',
  };

  factory SettlementResult.fromJson(Map<String, dynamic> json) {
    return SettlementResult(
      id: (json['id'] ?? '').toString(),
      employeeId: (json['employeeId'] ?? '').toString(),
      reason: (json['reason'] ?? '').toString(),
      terminationDate: DateTime.parse(json['terminationDate'] ?? DateTime.now().toIso8601String()),
      hireDate: DateTime.parse(json['hireDate'] ?? DateTime.now().toIso8601String()),
      daysWorked: (json['daysWorked'] as num?)?.toInt() ?? 0,
      monthsWorked: (json['monthsWorked'] ?? 0).toDouble(),
      yearsWorked: (json['yearsWorked'] ?? 0).toDouble(),
      monthlySalary: (json['monthlySalary'] ?? 0).toDouble(),
      dailySalary: (json['dailySalary'] ?? 0).toDouble(),
      aguinaldoPay: (json['aguinaldoPay'] ?? 0).toDouble(),
      vacationDaysAccrued: (json['vacationDaysAccrued'] ?? 0).toDouble(),
      vacationDaysTaken: (json['vacationDaysTaken'] ?? 0).toDouble(),
      vacationDaysToPay: (json['vacationDaysToPay'] ?? 0).toDouble(),
      vacationPay: (json['vacationPay'] ?? 0).toDouble(),
      severanceDays: (json['severanceDays'] ?? 0).toDouble(),
      severancePay: (json['severancePay'] ?? 0).toDouble(),
      isTrustPosition: json['isTrustPosition'] == true,
      trustPositionPay: (json['trustPositionPay'] ?? 0).toDouble(),
      pendingSalaryDays: (json['pendingSalaryDays'] as num?)?.toInt() ?? 0,
      pendingSalaryPay: (json['pendingSalaryPay'] ?? 0).toDouble(),
      overtimeHours: (json['overtimeHours'] ?? 0).toDouble(),
      overtimePay: (json['overtimePay'] ?? 0).toDouble(),
      inssLaboralAmount: (json['inssLaboralAmount'] ?? 0).toDouble(),
      irSalaryAmount: (json['irSalaryAmount'] ?? 0).toDouble(),
      irTotalAmount: (json['irTotalAmount'] ?? 0).toDouble(),
      inssPatronalAmount: (json['inssPatronalAmount'] ?? 0).toDouble(),
      inatecAmount: (json['inatecAmount'] ?? 0).toDouble(),
      grossSettlement: (json['grossSettlement'] ?? 0).toDouble(),
      totalDeductions: (json['totalDeductions'] ?? 0).toDouble(),
      netSettlement: (json['netSettlement'] ?? 0).toDouble(),
      countryCode: (json['countryCode'] ?? '').toString(),
      currency: (json['currency'] ?? 'NIO').toString(),
      countryName: (json['countryName'] ?? '').toString(),
      inssEmployeeRate: (json['inssEmployeeRateDisplay'] ?? 0.07).toDouble(),
      inssEmployerRate: (json['inssEmployerRateDisplay'] ?? 0.215).toDouble(),
      otherEmployerRate: (json['otherEmployerRateDisplay'] ?? 0.02).toDouble(),
      otherEmployerName: (json['otherEmployerName'] ?? 'Seguro Social').toString(),
    );
  }
}

/// Employee basic info for the settlement page
class EmployeeInfo {
  final String id;
  final String name;
  final String? position;
  final DateTime? hireDate;
  final double? salary;
  final String? countryCode;

  EmployeeInfo({
    required this.id,
    required this.name,
    this.position,
    this.hireDate,
    this.salary,
    this.countryCode,
  });

  factory EmployeeInfo.fromJson(Map<String, dynamic> json) {
    return EmployeeInfo(
      id: (json['id'] ?? '').toString(),
      name: '${json['firstName'] ?? ''} ${json['lastName'] ?? ''}'.trim(),
      position: json['position']?.toString(),
      hireDate: json['hireDate'] != null ? DateTime.parse(json['hireDate'].toString()) : null,
      salary: (json['salary'] as num?)?.toDouble(),
      countryCode: json['countryCode']?.toString(),
    );
  }
}

class SettlementNotifier extends AsyncNotifier<SettlementResult?> {
  @override
  SettlementResult? build() => null;

  Future<void> calculate({
    required String employeeId,
    required String reason,
    required DateTime terminationDate,
    DateTime? paidThroughDate,
    double overtimeHours = 0,
    double overtimePay = 0,
  }) async {
    state = const AsyncValue.loading();
    try {
      final dio = ref.read(dioClientProvider);
      final queryParams = <String, dynamic>{
        'employeeId': employeeId,
        'reason': reason,
        'terminationDate': terminationDate.toIso8601String().substring(0, 10),
        'overtimeHours': overtimeHours,
        'overtimePay': overtimePay,
      };
      if (paidThroughDate != null) {
        queryParams['paidThroughDate'] = paidThroughDate.toIso8601String().substring(0, 10);
      }
      final response = await dio.post('termination/calculate', queryParameters: queryParams);
      state = AsyncData(SettlementResult.fromJson(response.data));
    } catch (e, st) {
      state = AsyncError(e, st);
    }
  }

  void clear() {
    state = const AsyncData(null);
  }
}

final settlementProvider = AsyncNotifierProvider<SettlementNotifier, SettlementResult?>(
  SettlementNotifier.new,
);

/// Fetches employee info for the settlement page
final employeeInfoProvider = FutureProvider.family<EmployeeInfo?, String>((ref, employeeId) async {
  final dio = ref.read(dioClientProvider);
  try {
    final res = await dio.get('employees/$employeeId');
    final data = res.data;
    if (data is Map<String, dynamic>) {
      return EmployeeInfo.fromJson(data);
    }
    return null;
  } catch (_) {
    return null;
  }
});
