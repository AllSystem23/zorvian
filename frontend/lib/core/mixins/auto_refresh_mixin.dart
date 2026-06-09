import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';

mixin AutoRefreshMixin<T extends ConsumerStatefulWidget> on ConsumerState<T> {
  Timer? _timer;

  void startAutoRefresh({
    required List<dynamic> providers,
    Duration duration = const Duration(seconds: 60),
  }) {
    _timer = Timer.periodic(duration, (timer) {
      for (final provider in providers) {
        ref.invalidate(provider);
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}
