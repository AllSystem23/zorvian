import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';
import 'dashboard_provider.dart';
export 'dashboard_provider.dart' show CalendarEvent;

class CalendarState {
  final List<CalendarEvent> events;
  final bool loading;
  final String? error;

  const CalendarState({
    this.events = const [],
    this.loading = false,
    this.error,
  });

  CalendarState copyWith({
    List<CalendarEvent>? events,
    bool? loading,
    String? error,
  }) => CalendarState(
    events: events ?? this.events,
    loading: loading ?? this.loading,
    error: error ?? this.error,
  );
}

class CalendarNotifier extends Notifier<CalendarState> {
  @override
  CalendarState build() => const CalendarState();

  Future<void> load() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final r = await dio.get('/dashboard/vacation-calendar');
      state = CalendarState(
        events: (r.data as List).map((e) => CalendarEvent.fromJson(e)).toList(),
      );
    } catch (e) {
      state = state.copyWith(error: 'Error al cargar calendario', loading: false);
    }
  }
}

final calendarProvider = NotifierProvider<CalendarNotifier, CalendarState>(
  CalendarNotifier.new,
);
