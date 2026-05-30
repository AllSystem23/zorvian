import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../auth/auth_provider.dart';

final themeModeProvider = NotifierProvider<ThemeModeNotifier, ThemeMode>(
  ThemeModeNotifier.new,
);

class ThemeModeNotifier extends Notifier<ThemeMode> {
  @override
  ThemeMode build() => ThemeMode.light;

  Future<void> loadPreference() async {
    final storage = ref.read(secureStorageProvider);
    final saved = await storage.getThemeMode();
    state = saved == 'dark' ? ThemeMode.dark : saved == 'system' ? ThemeMode.system : ThemeMode.light;
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    state = mode;
    final storage = ref.read(secureStorageProvider);
    await storage.saveThemeMode(mode == ThemeMode.dark ? 'dark' : mode == ThemeMode.system ? 'system' : 'light');
  }

  void toggle() {
    final next = state == ThemeMode.light ? ThemeMode.dark : ThemeMode.light;
    setThemeMode(next);
  }
}
