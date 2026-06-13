class ApiConfig {
  ApiConfig._();

  static const _defaultUrl = String.fromEnvironment(
    'API_URL',
    defaultValue: 'https://nexora-9yal.onrender.com/zorvian/v1',
  );

  static String get baseUrl {
    var url = _defaultUrl.trim();
    while (url.endsWith('/')) {
      url = url.substring(0, url.length - 1);
    }
    return url;
  }

  static String get originUrl {
    final uri = Uri.parse(baseUrl);
    return '${uri.scheme}://${uri.host}${uri.hasPort ? ':${uri.port}' : ''}';
  }

  static String resolve(String path) {
    final p = path.startsWith('/') ? path.substring(1) : path;
    return '$baseUrl/$p';
  }
}
