import 'dart:async';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

enum ConnectivityStatus { online, offline }

class ConnectivityMonitor extends Notifier<ConnectivityStatus> {
  StreamSubscription<List<ConnectivityResult>>? _sub;
  ConnectivityStatus? _previous;

  @override
  ConnectivityStatus build() {
    _checkConnectivity();
    _sub = Connectivity().onConnectivityChanged.listen((results) {
      final online = results.any((r) =>
          r == ConnectivityResult.mobile ||
          r == ConnectivityResult.wifi ||
          r == ConnectivityResult.ethernet);
      final newState = online ? ConnectivityStatus.online : ConnectivityStatus.offline;
      _previous = state;
      state = newState;
    });
    ref.onDispose(() => _sub?.cancel());
    return ConnectivityStatus.online;
  }

  bool get justWentOnline => _previous == ConnectivityStatus.offline && state == ConnectivityStatus.online;

  Future<void> _checkConnectivity() async {
    final results = await Connectivity().checkConnectivity();
    final online = results.any((r) =>
        r == ConnectivityResult.mobile ||
        r == ConnectivityResult.wifi ||
        r == ConnectivityResult.ethernet);
    _previous = state;
    state = online ? ConnectivityStatus.online : ConnectivityStatus.offline;
  }
}

final connectivityProvider =
    NotifierProvider<ConnectivityMonitor, ConnectivityStatus>(
  ConnectivityMonitor.new,
);
