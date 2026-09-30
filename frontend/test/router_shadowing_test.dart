import 'package:flutter/material.dart' show GlobalKey, NavigatorState, ValueNotifier;
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:zorvian/app/router.dart';
import 'package:zorvian/core/navigation/nav_config.dart';

/// Collects every GoRoute in the tree with its full path pattern
/// (parent segments + own segments).
void collect(
  List<RouteBase> routes,
  String parentPrefix,
  void Function(GoRoute route, String pattern) onRoute,
) {
  for (final r in routes) {
    if (r is GoRoute) {
      final own = r.path.split('/').where((s) => s.isNotEmpty).toList();
      final pattern = r.path.startsWith('/')
          ? own
          : [...parentPrefix.split('/').where((s) => s.isNotEmpty), ...own];
      onRoute(r, pattern.join('/'));
      collect(r.routes, pattern.join('/'), onRoute);
    } else if (r is ShellRoute) {
      collect(r.routes, parentPrefix, onRoute);
    } else if (r is StatefulShellRoute) {
      for (final b in r.branches) {
        collect(b.routes, parentPrefix, onRoute);
      }
    }
  }
}

/// Real go_router matching: builds a RouteConfiguration over [appRoutes]
/// and asks findMatch() which pattern consumed the location.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();

  final routes = appRoutes;
  test('appRoutes was captured from routerProvider', () {
    expect(routes, isNotEmpty, reason: 'routerProvider must be read once');
  });

  final all = <(GoRoute, String)>[];
  collect(routes, '', (r, p) => all.add((r, p)));

  test('route tree is complete', () {
    expect(all.length, greaterThan(150));
  });

  final staticRoutes = all.where((e) => !e.$2.contains(':')).toList();

  final config = RouteConfiguration(
    ValueNotifier(RoutingConfig(routes: routes)),
    navigatorKey: GlobalKey<NavigatorState>(debugLabel: 'test-root'),
  );

  group('static routes resolve to themselves (no shadowing)', () {

    for (final (route, pattern) in staticRoutes) {
      test("'$pattern' -> ${route.name}", () {
        final uri = Uri.parse('/$pattern');
        final match = config.findMatch(uri);
        expect(match.error, isNull, reason: 'location /$pattern must match');
        // .last unwraps ShellRouteMatch nesting down to the leaf RouteMatch.
        expect(
          match.last.route,
          same(route),
          reason:
              'location /$pattern resolved to a different route '
              '(shadowed by a sibling/parent dynamic route?)',
        );
      });
    }
  });

  group('nav_config sidebar routes are reachable', () {
    final navRoutes = <String>[];
    for (final module in NavConfig.allModules) {
      for (final item in module.children) {
        if (item.route.isNotEmpty) navRoutes.add(item.route);
      }
    }

    for (final nav in navRoutes) {
      test("'$nav' matches exactly one route", () {
        final match = config.findMatch(Uri.parse(nav));
        expect(match.error, isNull, reason: 'sidebar route $nav must resolve');
        final matchedPattern = match.last.matchedLocation;
        expect(
          matchedPattern,
          nav,
          reason:
              'sidebar route $nav was captured by pattern $matchedPattern',
        );
      });
    }
  });

  group('role guard resolves the most specific pattern', () {
    test('/admin/companies requires SuperAdmin (not /admin roles)', () {
      expect(hasAccessForRole('SuperAdmin', '/admin/companies'), isTrue);
      expect(hasAccessForRole('CompanyAdmin', '/admin/companies'), isFalse);
      expect(hasAccessForRole('CompanyAdmin', '/admin'), isTrue);
    });

    test('/admin/subscription-plans requires SuperAdmin', () {
      expect(hasAccessForRole('CompanyAdmin', '/admin/subscription-plans'),
          isFalse);
      expect(hasAccessForRole('SuperAdmin', '/admin/subscription-plans'),
          isTrue);
    });

    test('/credits/overdue-dashboard excludes Employee (not /credits roles)',
        () {
      expect(hasAccessForRole('Employee', '/credits/overdue-dashboard'),
          isFalse);
      expect(hasAccessForRole('Supervisor', '/credits/overdue-dashboard'),
          isTrue);
      // /credits itself still allows Employee
      expect(hasAccessForRole('Employee', '/credits'), isTrue);
    });

    test('prefix must not match segments partially (e.g. /admin2)', () {
      // Old guard: '/admin2'.startsWith('/admin') → inherited /admin's roles
      // (SuperAdmin+CompanyAdmin only). New guard: falls to the default set,
      // which includes Employee — proving no partial-prefix inheritance.
      expect(hasAccessForRole('Employee', '/admin2'), isTrue);
      // but a real prefix route keeps its restriction
      expect(hasAccessForRole('Employee', '/admin'), isFalse);
    });

    test('unknown location falls back to default roles', () {
      expect(hasAccessForRole('Employee', '/nonexistent/route'), isTrue);
      expect(hasAccessForRole('Employee', '/unauthorized'), isTrue);
    });
  });
}
