import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'z_empty_state.dart';
import 'z_error_boundary.dart';
import 'z_skeleton.dart';

class ZAsyncRenderer<T> extends StatelessWidget {
  final AsyncValue<T> value;
  final Widget Function(T data) builder;
  final Widget? loadingWidget;
  final Widget? emptyWidget;
  final VoidCallback? onRetry;

  const ZAsyncRenderer({
    super.key,
    required this.value,
    required this.builder,
    this.loadingWidget,
    this.emptyWidget,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return value.when(
      data: (data) {
        if (data == null || (data is List && data.isEmpty)) {
          return emptyWidget ?? const ZEmptyState(icon: Icons.inbox, title: 'No hay datos disponibles.');
        }
        return builder(data);
      },
      error: (error, stack) => ZErrorDisplay(
        message: 'No pudimos cargar los datos.',
        onRetry: onRetry,
      ),
      loading: () => loadingWidget ?? const ZSkeleton(height: 100),
    );
  }
}
