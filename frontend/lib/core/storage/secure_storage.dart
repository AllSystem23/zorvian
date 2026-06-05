import 'package:flutter/foundation.dart' show kIsWeb;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class SecureStorage {
  static final _storage = FlutterSecureStorage(
    webOptions: WebOptions(dbName: 'ZorvianStorage'),
  );
  static final _memory = <String, String>{};

  static const _accessTokenKey = 'access_token';
  static const _refreshTokenKey = 'refresh_token';
  static const _themeModeKey = 'theme_mode';
  static const _biometricEnabledKey = 'biometric_enabled';

  Future<void> saveTokens(String access, String refresh) async {
    await Future.wait([
      _write(_accessTokenKey, access),
      _write(_refreshTokenKey, refresh),
    ]);
  }

  Future<String?> getAccessToken() => _read(_accessTokenKey);

  Future<String?> getRefreshToken() => _read(_refreshTokenKey);

  Future<void> clearTokens() async {
    await Future.wait([
      _delete(_accessTokenKey),
      _delete(_refreshTokenKey),
    ]);
  }

  Future<void> saveThemeMode(String mode) => _write(_themeModeKey, mode);

  Future<String?> getThemeMode() => _read(_themeModeKey);

  Future<void> setBiometricEnabled(bool enabled) =>
      _write(_biometricEnabledKey, enabled.toString());

  Future<bool> isBiometricEnabled() async {
    final val = await _read(_biometricEnabledKey);
    return val == 'true';
  }

  Future<void> _write(String key, String value) async {
    _memory[key] = value;
    try {
      await _storage.write(key: key, value: value);
    } catch (_) {}
  }

  Future<String?> _read(String key) async {
    if (kIsWeb && _memory.containsKey(key)) return _memory[key];
    try {
      final val = await _storage.read(key: key);
      if (val != null) _memory[key] = val;
      return val;
    } catch (_) {
      return _memory[key];
    }
  }

  Future<void> _delete(String key) async {
    _memory.remove(key);
    try {
      await _storage.delete(key: key);
    } catch (_) {}
  }
}
