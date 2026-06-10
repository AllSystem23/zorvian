import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../services/preferences_service.dart';
import '../../auth/auth_provider.dart';
import 'nav_config.dart';

final preferencesServiceProvider = Provider<PreferencesService>((ref) {
  throw UnimplementedError('PreferencesService must be overridden at app startup');
});

final sidebarCollapsedProvider = NotifierProvider<SidebarCollapsedNotifier, bool>(
  SidebarCollapsedNotifier.new,
);

final expandedModulesProvider = NotifierProvider<ExpandedModulesNotifier, Set<String>>(
  ExpandedModulesNotifier.new,
);

final searchQueryProvider = NotifierProvider<SearchQueryNotifier, String>(
  SearchQueryNotifier.new,
);

final favoritesProvider = NotifierProvider<FavoritesNotifier, List<String>>(
  FavoritesNotifier.new,
);

final recentItemsProvider = NotifierProvider<RecentItemsNotifier, List<String>>(
  RecentItemsNotifier.new,
);

final filteredModulesProvider = Provider.autoDispose.family<List<NavModule>, String>((ref, role) {
  final query = ref.watch(searchQueryProvider);
  return NavConfig.getModulesForRole(role, searchQuery: query.isNotEmpty ? query : null);
});

final sidebarBadgesProvider = NotifierProvider<BadgesNotifier, Map<String, int>>(
  BadgesNotifier.new,
);

class BadgesNotifier extends Notifier<Map<String, int>> {
  Timer? _timer;

  @override
  Map<String, int> build() {
    _fetch();
    _timer = Timer.periodic(const Duration(seconds: 60), (_) => _fetch());
    ref.onDispose(() => _timer?.cancel());
    return {};
  }

  Future<void> _fetch() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('badges');
      final data = response.data;
      if (data is Map) {
        state = data.map((k, v) => MapEntry(k.toString(), (v as num?)?.toInt() ?? 0));
      }
    } catch (_) {}
  }
}

class SearchQueryNotifier extends Notifier<String> {
  @override
  String build() => '';

  void update(String query) => state = query;
  void clear() => state = '';
}

class SidebarCollapsedNotifier extends Notifier<bool> {
  @override
  bool build() => false;

  void toggle() {
    state = !state;
    try {
      ref.read(preferencesServiceProvider).setSidebarCollapsed(state);
    } catch (_) {}
  }

  void setCollapsed(bool value) {
    state = value;
    try {
      ref.read(preferencesServiceProvider).setSidebarCollapsed(value);
    } catch (_) {}
  }
}

class ExpandedModulesNotifier extends Notifier<Set<String>> {
  @override
  Set<String> build() => {};

  void toggle(String moduleId) {
    if (state.contains(moduleId)) {
      state = Set.from(state)..remove(moduleId);
    } else {
      state = Set.from(state)..add(moduleId);
    }
  }

  void expand(String moduleId) {
    if (!state.contains(moduleId)) {
      state = Set.from(state)..add(moduleId);
    }
  }

  void collapseAll() => state = {};
}

class FavoritesNotifier extends Notifier<List<String>> {
  @override
  List<String> build() => [];

  void toggle(String route) {
    if (state.contains(route)) {
      state = List.from(state)..remove(route);
    } else {
      state = List.from(state)..add(route);
    }
    try {
      ref.read(preferencesServiceProvider).toggleFavorite(route);
    } catch (_) {}
  }

  bool isFavorite(String route) => state.contains(route);
}

class RecentItemsNotifier extends Notifier<List<String>> {
  static const int _maxItems = 5;

  @override
  List<String> build() => [];

  void add(String route) {
    state = List.from(state)..remove(route)..insert(0, route);
    if (state.length > _maxItems) {
      state = state.sublist(0, _maxItems);
    }
  }
}
