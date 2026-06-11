import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/network/dio_client.dart';

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

class AttendanceService {
  final DioClient _dio;
  AttendanceService(this._dio);

  Future<AttendanceSummary> getMyAttendance() async {
    final r = await _dio.get('attendance/my');
    return AttendanceSummary.fromJson(r.data);
  }

  Future<AttendanceRecord> checkIn(Map<String, dynamic> data) async {
    final r = await _dio.post('attendance/check-in', data: data);
    return AttendanceRecord.fromJson(r.data);
  }

  Future<AttendanceRecord> checkOut(Map<String, dynamic> data) async {
    final r = await _dio.post('attendance/check-out', data: data);
    return AttendanceRecord.fromJson(r.data);
  }
}

final attendanceServiceProvider = Provider<AttendanceService>((ref) {
  final dio = ref.read(dioClientProvider);
  return AttendanceService(dio);
});

class _CheckingNotifier extends Notifier<bool> {
  @override
  bool build() => false;
}

final checkingProvider = NotifierProvider<_CheckingNotifier, bool>(_CheckingNotifier.new);

class AttendanceNotifier extends AsyncNotifier<AttendanceSummary> {
  @override
  Future<AttendanceSummary> build() async {
    return ref.read(attendanceServiceProvider).getMyAttendance();
  }

  Future<void> load() async {
    state = const AsyncValue.loading();
    state = await AsyncValue.guard(() => ref.read(attendanceServiceProvider).getMyAttendance());
  }

  Future<bool> checkInQR(String code) async {
    ref.read(checkingProvider.notifier).state = true;
    try {
      final body = <String, dynamic>{'qrCode': code};
      await ref.read(attendanceServiceProvider).checkIn(body);
      await load();
      return true;
    } catch (e) {
      return false;
    } finally {
      ref.read(checkingProvider.notifier).state = false;
    }
  }

  Future<bool> checkIn({double? lat, double? lng}) async {
    ref.read(checkingProvider.notifier).state = true;
    try {
      final body = <String, dynamic>{};
      if (lat != null) body['latitude'] = lat;
      if (lng != null) body['longitude'] = lng;
      await ref.read(attendanceServiceProvider).checkIn(body);
      await load();
      return true;
    } catch (e) {
      return false;
    } finally {
      ref.read(checkingProvider.notifier).state = false;
    }
  }

  Future<bool> checkOut({double? lat, double? lng}) async {
    ref.read(checkingProvider.notifier).state = true;
    try {
      final body = <String, dynamic>{};
      if (lat != null) body['latitude'] = lat;
      if (lng != null) body['longitude'] = lng;
      await ref.read(attendanceServiceProvider).checkOut(body);
      await load();
      return true;
    } catch (e) {
      return false;
    } finally {
      ref.read(checkingProvider.notifier).state = false;
    }
  }
}

final attendanceProvider = AsyncNotifierProvider<AttendanceNotifier, AttendanceSummary>(AttendanceNotifier.new);
