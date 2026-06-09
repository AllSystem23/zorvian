import 'dart:async';
import 'package:flutter/foundation.dart';

/// Debouncer utility for search inputs and frequent operations
class ZDebouncer {
  final int milliseconds;
  Timer? _timer;

  ZDebouncer({this.milliseconds = 500});

  /// Execute callback after debounce delay
  void run(VoidCallback action) {
    _timer?.cancel();
    _timer = Timer(Duration(milliseconds: milliseconds), action);
  }

  /// Cancel pending execution
  void cancel() {
    _timer?.cancel();
  }

  /// Check if debouncer is active
  bool get isActive => _timer?.isActive ?? false;

  void dispose() {
    _timer?.cancel();
    _timer = null;
  }
}

/// Throttle utility for scroll and resize events
class ZThrottle {
  final int milliseconds;
  DateTime _lastTime = DateTime.fromMillisecondsSinceEpoch(0);

  ZThrottle({this.milliseconds = 300});

  /// Execute callback only if enough time has passed
  bool run(VoidCallback action) {
    final now = DateTime.now();
    if (now.difference(_lastTime).inMilliseconds >= milliseconds) {
      _lastTime = now;
      action();
      return true;
    }
    return false;
  }
}