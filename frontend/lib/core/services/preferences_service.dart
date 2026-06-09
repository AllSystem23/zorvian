import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Persistent user preferences service
class PreferencesService {
  static const _keyThemeMode = 'theme_mode';
  static const _keyLocale = 'locale';
  static const _keySidebarCollapsed = 'sidebar_collapsed';
  static const _keyFavorites = 'favorite_routes';
  static const _keyTableView = 'table_view_mode';

  late final SharedPreferences _prefs;

  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
  }

  // ── Theme ──
  ThemeMode get themeMode {
    final value = _prefs.getString(_keyThemeMode);
    switch (value) {
      case 'light':
        return ThemeMode.light;
      case 'dark':
        return ThemeMode.dark;
      default:
        return ThemeMode.system;
    }
  }

  Future<void> setThemeMode(ThemeMode mode) async {
    await _prefs.setString(_keyThemeMode, mode.name);
  }

  // ── Locale ──
  Locale get locale {
    final code = _prefs.getString(_keyLocale);
    if (code != null) return Locale(code);
    return const Locale('es');
  }

  Future<void> setLocale(String languageCode) async {
    await _prefs.setString(_keyLocale, languageCode);
  }

  // ── Sidebar ──
  bool get sidebarCollapsed => _prefs.getBool(_keySidebarCollapsed) ?? false;

  Future<void> setSidebarCollapsed(bool collapsed) async {
    await _prefs.setBool(_keySidebarCollapsed, collapsed);
  }

  // ── Favorites ──
  List<String> get favoriteRoutes =>
      _prefs.getStringList(_keyFavorites) ?? ['/dashboard'];

  Future<void> toggleFavorite(String route) async {
    final favorites = List<String>.from(favoriteRoutes);
    if (favorites.contains(route)) {
      favorites.remove(route);
    } else {
      favorites.add(route);
    }
    await _prefs.setStringList(_keyFavorites, favorites);
  }

  bool isFavorite(String route) => favoriteRoutes.contains(route);

  // ── Table View ──
  String get tableViewMode => _prefs.getString(_keyTableView) ?? 'list';

  Future<void> setTableViewMode(String mode) async {
    await _prefs.setString(_keyTableView, mode);
  }

  // ── Clear all ──
  Future<void> clearAll() async {
    await _prefs.clear();
  }
}