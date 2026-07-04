import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/auth/auth_provider.dart';
import 'package:zorvian/auth/tenants_provider.dart';
import 'package:zorvian/core/providers/company_branch_provider.dart';
import 'package:zorvian/core/services/signalr_service.dart';
import 'package:zorvian/core/storage/secure_storage.dart';
import 'package:zorvian/core/widgets/header/global_header.dart';
import 'package:zorvian/core/theme/theme_provider.dart';

/// A test notifier that returns a fixed [AuthState] from build().
class _FixedAuthNotifier extends AuthNotifier {
  final AuthState _fixedState;
  _FixedAuthNotifier(this._fixedState);

  @override
  AuthState build() => _fixedState;
}

/// A signalR notifier that returns a fixed state.
class _FixedSignalRNotifier extends SignalRNotifier {
  final NotificationState _fixedState;
  _FixedSignalRNotifier(this._fixedState);

  @override
  NotificationState build() => _fixedState;
}

/// A theme notifier that returns a fixed theme mode.
class _FixedThemeNotifier extends ThemeModeNotifier {
  final ThemeMode _fixedMode;
  _FixedThemeNotifier(this._fixedMode);

  @override
  ThemeMode build() => _fixedMode;
}

/// Fake secure storage that does nothing (avoids platform errors).
class _FakeSecureStorage extends SecureStorage {
  @override
  Future<String?> getAccessToken() async => null;

  @override
  Future<String?> getRefreshToken() async => null;

  @override
  Future<void> saveTokens(String access, String refresh) async {}

  @override
  Future<void> clearTokens() async {}

  @override
  Future<String?> getCurrencyCode() async => null;

  @override
  Future<void> saveCurrencyCode(String code) async {}
}

/// Helper to create a test app with mocked providers.
Widget buildTestApp({
  AuthState? authState,
  NotificationState? notifState,
  ThemeMode themeMode = ThemeMode.light,
}) {
  return ProviderScope(
    overrides: [
      authProvider.overrideWith(
        () => _FixedAuthNotifier(
          authState ??
              const AuthState(
                status: AuthStatus.authenticated,
                displayName: 'Test User',
                email: 'test@zorvian.com',
                role: 'Admin',
                currencyCode: 'NIO',
              ),
        ),
      ),
      signalRProvider.overrideWith(
        () => _FixedSignalRNotifier(
          notifState ?? const NotificationState(),
        ),
      ),
      themeModeProvider.overrideWith(
        () => _FixedThemeNotifier(themeMode),
      ),
      secureStorageProvider.overrideWith((ref) => _FakeSecureStorage()),
      // Override FutureProviders that make HTTP calls to prevent pending timers
      companyListProvider.overrideWith((ref) => Future.value(<Map<String, dynamic>>[])),
      headerBranchListProvider.overrideWith((ref) => Future.value(<Map<String, dynamic>>[])),
      tenantsListProvider.overrideWith((ref) => Future.value(<TenantInfo>[])),
    ],
    child: MaterialApp(
      home: Scaffold(body: GlobalHeader()),
    ),
  );
}

/// Helper to set a large screen size for a test that renders
/// the GlobalHeader (which has many items in a Row).
Future<void> setLargeScreenSize(WidgetTester tester) async {
  addTearDown(tester.view.resetPhysicalSize);
  tester.view.physicalSize = const Size(1920, 1080);
  tester.view.devicePixelRatio = 1.0;
}

void main() {
  group('GlobalHeader connectivity dot', () {
    setUp(() async {
      // Use a default setup — will call setLargeScreenSize per test
    });

    testWidgets('renders connected state', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          connectionState: ZConnectionState.connected,
        ),
      ));

      expect(find.byType(GlobalHeader), findsOneWidget);
    });

    testWidgets('renders disconnected state', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          connectionState: ZConnectionState.disconnected,
        ),
      ));

      expect(find.byType(GlobalHeader), findsOneWidget);
    });

    testWidgets('renders connecting state', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          connectionState: ZConnectionState.connecting,
        ),
      ));

      expect(find.byType(GlobalHeader), findsOneWidget);
    });

    testWidgets('renders reconnecting state', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          connectionState: ZConnectionState.reconnecting,
        ),
      ));

      expect(find.byType(GlobalHeader), findsOneWidget);
    });
  });

  group('GlobalHeader notification panel', () {
    testWidgets('shows empty state when tapped', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          notifications: [],
          connectionState: ZConnectionState.connected,
        ),
      ));

      await tester.tap(find.byIcon(Icons.notifications_outlined));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('Notificaciones'), findsOneWidget);
      expect(find.text('Sin notificaciones'), findsOneWidget);
    });

    testWidgets('shows notification items in list', (tester) async {
      final notification = NotificationItem(
        title: 'Test Title',
        message: 'Test Message',
        type: 'info',
        createdAt: DateTime(2026, 7, 3),
      );

      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: NotificationState(
          notifications: [notification],
          connectionState: ZConnectionState.connected,
        ),
      ));

      await tester.tap(find.byIcon(Icons.notifications_outlined));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('Notificaciones'), findsOneWidget);
      expect(find.text('Test Title'), findsOneWidget);
      expect(find.text('Test Message'), findsOneWidget);
    });

    testWidgets('shows approval icon for approval type', (tester) async {
      final approval = NotificationItem(
        title: 'Aprobacion requerida',
        message: 'Una solicitud de vacaciones',
        type: 'approval',
        createdAt: DateTime(2026, 7, 3),
      );

      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: NotificationState(
          notifications: [approval],
          connectionState: ZConnectionState.connected,
        ),
      ));

      await tester.tap(find.byIcon(Icons.notifications_outlined));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.byIcon(Icons.approval), findsOneWidget);
    });

    testWidgets('shows limpiar todo button when notifications exist',
        (tester) async {
      final notification = NotificationItem(title: 'Test', message: 'Msg');

      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: NotificationState(
          notifications: [notification],
          connectionState: ZConnectionState.connected,
        ),
      ));

      await tester.tap(find.byIcon(Icons.notifications_outlined));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('Limpiar todo'), findsOneWidget);
    });

    testWidgets('hides limpiar todo when no notifications', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          notifications: [],
          connectionState: ZConnectionState.connected,
        ),
      ));

      await tester.tap(find.byIcon(Icons.notifications_outlined));
      await tester.pump();
      await tester.pump(const Duration(milliseconds: 500));

      expect(find.text('Limpiar todo'), findsNothing);
    });
  });

  group('GlobalHeader quick actions', () {
    testWidgets('renders all action icons', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          connectionState: ZConnectionState.connected,
        ),
      ));

      expect(find.byIcon(Icons.currency_exchange), findsOneWidget);
      expect(find.byIcon(Icons.notifications_outlined), findsOneWidget);
      expect(find.byIcon(Icons.chat_bubble_outline), findsOneWidget);
    });

    testWidgets('shows badge count on notifications', (tester) async {
      final notification = NotificationItem(title: 'New', message: 'Msg');

      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: NotificationState(
          notifications: [notification],
          connectionState: ZConnectionState.connected,
        ),
      ));

      expect(find.text('1'), findsOneWidget);
    });

    testWidgets('hides badge when no notifications', (tester) async {
      await setLargeScreenSize(tester);
      await tester.pumpWidget(buildTestApp(
        notifState: const NotificationState(
          notifications: [],
          connectionState: ZConnectionState.connected,
        ),
      ));

      expect(find.text('0'), findsNothing);
      expect(find.text('1'), findsNothing);
    });
  });
}
