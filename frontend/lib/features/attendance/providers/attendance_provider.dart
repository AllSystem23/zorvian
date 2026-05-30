import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

class AttendanceRecord {
  final String id;
  final String date;
  final String? checkInTime;
  final String? checkOutTime;
  final String status;
  final double? totalHours;

  const AttendanceRecord({
    required this.id,
    required this.date,
    this.checkInTime,
    this.checkOutTime,
    required this.status,
    this.totalHours,
  });

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) => AttendanceRecord(
    id: json['id'] as String,
    date: json['date'] as String,
    checkInTime: json['checkInTime'] as String?,
    checkOutTime: json['checkOutTime'] as String?,
    status: json['status'] as String? ?? '',
    totalHours: (json['totalHours'] as num?)?.toDouble(),
  );
}

class AttendanceSummary {
  final int presentDays;
  final int lateDays;
  final int absentDays;
  final double totalHours;
  final double averageHoursPerDay;
  final List<AttendanceRecord> records;

  const AttendanceSummary({
    required this.presentDays,
    required this.lateDays,
    required this.absentDays,
    required this.totalHours,
    required this.averageHoursPerDay,
    required this.records,
  });

  factory AttendanceSummary.fromJson(Map<String, dynamic> json) => AttendanceSummary(
    presentDays: json['presentDays'] as int,
    lateDays: json['lateDays'] as int,
    absentDays: json['absentDays'] as int,
    totalHours: (json['totalHours'] as num).toDouble(),
    averageHoursPerDay: (json['averageHoursPerDay'] as num).toDouble(),
    records: (json['records'] as List).map((e) => AttendanceRecord.fromJson(e)).toList(),
  );
}

class AttendanceState {
  final AttendanceSummary? summary;
  final AttendanceRecord? todayRecord;
  final bool loading;
  final bool checking;
  final String? error;
  final String? checkResult;

  const AttendanceState({
    this.summary,
    this.todayRecord,
    this.loading = false,
    this.checking = false,
    this.error,
    this.checkResult,
  });

  AttendanceState copyWith({
    AttendanceSummary? summary,
    AttendanceRecord? todayRecord,
    bool? loading,
    bool? checking,
    String? error,
    String? checkResult,
  }) => AttendanceState(
    summary: summary ?? this.summary,
    todayRecord: todayRecord ?? this.todayRecord,
    loading: loading ?? this.loading,
    checking: checking ?? this.checking,
    error: error ?? this.error,
    checkResult: checkResult ?? this.checkResult,
  );
}

class AttendanceNotifier extends Notifier<AttendanceState> {
  @override
  AttendanceState build() => const AttendanceState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('/attendance/my');
      final summary = AttendanceSummary.fromJson(r.data);

      // Find today's record if it exists
      final today = DateTime.now();
      final todayStr = '${today.year.toString().padLeft(4, '0')}-${today.month.toString().padLeft(2, '0')}-${today.day.toString().padLeft(2, '0')}';
      final todayRecord = summary.records.where((r) => r.date == todayStr).firstOrNull;

      state = AttendanceState(summary: summary, todayRecord: todayRecord);
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar asistencia', loading: false);
    }
  }

  Future<bool> checkInQR(String qrCode, {double? lat, double? lng}) async {
    state = state.copyWith(checking: true, error: null, checkResult: null);
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{'qrCode': qrCode};
      if (lat != null) body['latitude'] = lat;
      if (lng != null) body['longitude'] = lng;
      final r = await dio.post('/attendance/qr-check-in', data: body);
      final record = AttendanceRecord.fromJson(r.data);
      state = state.copyWith(todayRecord: record, checking: false, checkResult: 'check-in');
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error al registrar con QR', checking: false);
      return false;
    }
  }

  Future<bool> checkIn({double? lat, double? lng}) async {
    state = state.copyWith(checking: true, error: null, checkResult: null);
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{};
      if (lat != null) body['latitude'] = lat;
      if (lng != null) body['longitude'] = lng;
      final r = await dio.post('/attendance/check-in', data: body);
      final record = AttendanceRecord.fromJson(r.data);
      state = state.copyWith(todayRecord: record, checking: false, checkResult: 'check-in');
      return true;
    } catch (e) {
      final msg = e.toString().contains('Ya existe') ? 'Ya registraste entrada hoy' : 'Error al marcar entrada';
      state = state.copyWith(error: msg, checking: false);
      return false;
    }
  }

  Future<bool> checkOut({double? lat, double? lng}) async {
    state = state.copyWith(checking: true, error: null, checkResult: null);
    try {
      final dio = ref.read(dioClientProvider);
      final body = <String, dynamic>{};
      if (lat != null) body['latitude'] = lat;
      if (lng != null) body['longitude'] = lng;
      final r = await dio.post('/attendance/check-out', data: body);
      final record = AttendanceRecord.fromJson(r.data);
      state = state.copyWith(todayRecord: record, checking: false, checkResult: 'check-out');
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error al marcar salida', checking: false);
      return false;
    }
  }
}

final attendanceProvider = NotifierProvider<AttendanceNotifier, AttendanceState>(
  AttendanceNotifier.new,
);
