import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import 'package:share_plus/share_plus.dart';
import 'package:path_provider/path_provider.dart';
import 'dart:io';
import '../../../auth/auth_provider.dart';
import '../../../core/providers/company_branch_provider.dart';

class VacationReceiptItem {
  final String id;
  final String vacationRequestId;
  final String employeeName;
  final String receiptNumber;
  final String startDate;
  final String endDate;
  final double businessDays;
  final double netPay;
  final String status;
  final String? paymentDate;
  final String countryCode;
  final String currency;

  const VacationReceiptItem({
    required this.id,
    required this.vacationRequestId,
    required this.employeeName,
    required this.receiptNumber,
    required this.startDate,
    required this.endDate,
    required this.businessDays,
    required this.netPay,
    required this.status,
    this.paymentDate,
    this.countryCode = '',
    this.currency = 'NIO',
  });

  factory VacationReceiptItem.fromJson(Map<String, dynamic> json) => VacationReceiptItem(
    id: json['id'] as String? ?? json['Id']?.toString() ?? '',
    vacationRequestId: json['vacationRequestId'] as String? ?? json['VacationRequestId']?.toString() ?? '',
    employeeName: json['employeeName'] as String? ?? json['EmployeeName'] as String? ?? '',
    receiptNumber: json['receiptNumber'] as String? ?? json['ReceiptNumber'] as String? ?? '',
    startDate: json['startDate'] as String? ?? json['StartDate']?.toString() ?? '',
    endDate: json['endDate'] as String? ?? json['EndDate']?.toString() ?? '',
    businessDays: (json['businessDays'] as num?)?.toDouble() ?? (json['BusinessDays'] as num?)?.toDouble() ?? 0,
    netPay: (json['netPay'] as num?)?.toDouble() ?? (json['NetPay'] as num?)?.toDouble() ?? 0,
    status: json['status'] as String? ?? json['Status'] as String? ?? 'paid',
    paymentDate: json['paymentDate'] as String? ?? json['PaymentDate']?.toString(),
    countryCode: json['countryCode'] as String? ?? json['CountryCode'] as String? ?? '',
    currency: json['currency'] as String? ?? json['Currency'] as String? ?? 'NIO',
  );
}

class VacationReceiptDetail {
  final String id;
  final String vacationRequestId;
  final String employeeId;
  final String employeeName;
  final String employeeCode;
  final String receiptNumber;
  final String startDate;
  final String endDate;
  final String returnDate;
  final double businessDays;
  final double dailySalary;
  final double monthlySalary;
  final double vacationPay;
  final double inssEmployeeAmount;
  final double inssEmployeeRate;
  final double totalAssignments;
  final double totalDeductions;
  final double netPay;
  final String countryCode;
  final String currency;
  final String paymentDate;
  final String? observations;
  final String? paidBy;
  final String status;

  const VacationReceiptDetail({
    required this.id,
    required this.vacationRequestId,
    required this.employeeId,
    required this.employeeName,
    required this.employeeCode,
    required this.receiptNumber,
    required this.startDate,
    required this.endDate,
    required this.returnDate,
    required this.businessDays,
    required this.dailySalary,
    required this.monthlySalary,
    required this.vacationPay,
    required this.inssEmployeeAmount,
    required this.inssEmployeeRate,
    required this.totalAssignments,
    required this.totalDeductions,
    required this.netPay,
    required this.countryCode,
    required this.currency,
    required this.paymentDate,
    required this.status,
    this.observations,
    this.paidBy,
  });

  factory VacationReceiptDetail.fromJson(Map<String, dynamic> json) => VacationReceiptDetail(
    id: json['id'] as String? ?? json['Id']?.toString() ?? '',
    vacationRequestId: json['vacationRequestId'] as String? ?? json['VacationRequestId']?.toString() ?? '',
    employeeId: json['employeeId'] as String? ?? json['EmployeeId']?.toString() ?? '',
    employeeName: json['employeeName'] as String? ?? json['EmployeeName'] as String? ?? '',
    employeeCode: json['employeeCode'] as String? ?? json['EmployeeCode'] as String? ?? '',
    receiptNumber: json['receiptNumber'] as String? ?? json['ReceiptNumber'] as String? ?? '',
    startDate: json['startDate'] as String? ?? json['StartDate']?.toString() ?? '',
    endDate: json['endDate'] as String? ?? json['EndDate']?.toString() ?? '',
    returnDate: json['returnDate'] as String? ?? json['ReturnDate']?.toString() ?? '',
    businessDays: (json['businessDays'] as num?)?.toDouble() ?? (json['BusinessDays'] as num?)?.toDouble() ?? 0,
    dailySalary: (json['dailySalary'] as num?)?.toDouble() ?? (json['DailySalary'] as num?)?.toDouble() ?? 0,
    monthlySalary: (json['monthlySalary'] as num?)?.toDouble() ?? (json['MonthlySalary'] as num?)?.toDouble() ?? 0,
    vacationPay: (json['vacationPay'] as num?)?.toDouble() ?? (json['VacationPay'] as num?)?.toDouble() ?? 0,
    inssEmployeeAmount: (json['inssEmployeeAmount'] as num?)?.toDouble() ?? (json['InssEmployeeAmount'] as num?)?.toDouble() ?? 0,
    inssEmployeeRate: (json['inssEmployeeRate'] as num?)?.toDouble() ?? (json['InssEmployeeRate'] as num?)?.toDouble() ?? 0,
    totalAssignments: (json['totalAssignments'] as num?)?.toDouble() ?? (json['TotalAssignments'] as num?)?.toDouble() ?? 0,
    totalDeductions: (json['totalDeductions'] as num?)?.toDouble() ?? (json['TotalDeductions'] as num?)?.toDouble() ?? 0,
    netPay: (json['netPay'] as num?)?.toDouble() ?? (json['NetPay'] as num?)?.toDouble() ?? 0,
    countryCode: json['countryCode'] as String? ?? json['CountryCode'] as String? ?? '',
    currency: json['currency'] as String? ?? json['Currency'] as String? ?? 'NIO',
    paymentDate: json['paymentDate'] as String? ?? json['PaymentDate']?.toString() ?? '',
    status: json['status'] as String? ?? json['Status'] as String? ?? 'paid',
    observations: json['observations'] as String? ?? json['Observations'] as String?,
    paidBy: json['paidBy'] as String? ?? json['PaidBy'] as String?,
  );

  String get inssRateDisplay => '${(inssEmployeeRate * 100).toStringAsFixed(1)}%';
}

class VacationReceiptListState {
  final List<VacationReceiptItem> items;
  final int total;
  final int page;
  final int pageSize;
  final int? year;
  final bool loading;
  final String? error;

  const VacationReceiptListState({
    this.items = const [],
    this.total = 0,
    this.page = 1,
    this.pageSize = 20,
    this.year,
    this.loading = false,
    this.error,
  });

  int get totalPages => total == 0 ? 1 : (total / pageSize).ceil();
  bool get hasPrevious => page > 1;
  bool get hasNext => page < totalPages;

  VacationReceiptListState copyWith({
    List<VacationReceiptItem>? items,
    int? total,
    int? page,
    int? pageSize,
    int? year,
    bool? yearFilter,
    bool? loading,
    String? error,
  }) => VacationReceiptListState(
    items: items ?? this.items,
    total: total ?? this.total,
    page: page ?? this.page,
    pageSize: pageSize ?? this.pageSize,
    year: yearFilter == true ? year : this.year,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

class VacationReceiptNotifier extends Notifier<VacationReceiptListState> {
  static String? _lastScope;

  @override
  VacationReceiptListState build() {
    final scope = ref.watch(branchScopeProvider);
    if (_lastScope != null && _lastScope != scope) {
      Future.microtask(() => load());
    }
    _lastScope = scope;
    return const VacationReceiptListState();
  }

  Future<void> load({String? employeeId, int? year, int page = 1, int pageSize = 20}) async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final params = <String, dynamic>{
        'page': page,
        'pageSize': pageSize,
        if (employeeId case final e?) 'employeeId': e,
        if (year case final y?) 'year': y,
      };
      final r = await dio.get('vacations/receipts', params: params);
      final data = r.data;
      state = VacationReceiptListState(
        items: (data['items'] as List).map((e) => VacationReceiptItem.fromJson(e)).toList(),
        total: data['total'] as int? ?? 0,
        page: page,
        pageSize: pageSize,
        year: year,
      );
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar recibos', loading: false);
    }
  }

  Future<void> nextPage() async {
    if (!state.hasNext) return;
    await load(year: state.year, page: state.page + 1, pageSize: state.pageSize);
  }

  Future<void> previousPage() async {
    if (!state.hasPrevious) return;
    await load(year: state.year, page: state.page - 1, pageSize: state.pageSize);
  }

  Future<VacationReceiptDetail?> loadDetail(String receiptId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('vacations/receipts/$receiptId');
      return VacationReceiptDetail.fromJson(r.data);
    } catch (_) {
      return null;
    }
  }

  Future<VacationReceiptDetail?> loadReceiptByRequest(String vacationRequestId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('vacations/$vacationRequestId/receipt');
      return VacationReceiptDetail.fromJson(r.data);
    } catch (_) {
      return null;
    }
  }

  Future<bool> payVacation(String vacationRequestId, {String? observations}) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('vacations/$vacationRequestId/pay', data: {
        if (observations != null && observations.isNotEmpty) 'observations': observations,
      });
      return true;
    } catch (_) {
      return false;
    }
  }

  Future<void> downloadPdf(String receiptId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get(
        'vacations/receipts/$receiptId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      final bytes = r.data;
      if (bytes is! List<int> || bytes.isEmpty) return;
      final dir = await getTemporaryDirectory();
      final file = File('${dir.path}/recibo_vacaciones_$receiptId.pdf');
      await file.writeAsBytes(bytes, flush: true);
      await SharePlus.instance.share(
        ShareParams(files: [XFile(file.path, mimeType: 'application/pdf')]),
      );
    } catch (_) {
      // Silent fail — UI shows error via callback
    }
  }
}

final vacationReceiptProvider = NotifierProvider<VacationReceiptNotifier, VacationReceiptListState>(
  VacationReceiptNotifier.new,
);
