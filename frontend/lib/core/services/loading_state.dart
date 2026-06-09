import 'package:flutter/material.dart';

/// A generic loading state wrapper for async operations.
/// Usage with Riverpod: combine with StateNotifier to manage loading/error/data states.
class LoadingState<T> {
  final bool isLoading;
  final String? error;
  final T? data;

  const LoadingState._({this.isLoading = false, this.error, this.data});

  const LoadingState.initial() : this._();
  const LoadingState.loading() : this._(isLoading: true);
  LoadingState.success(T data) : this._(data: data);
  LoadingState.failure(String error) : this._(error: error);

  bool get hasError => error != null;
  bool get hasData => data != null;
  bool get isIdle => !isLoading && !hasError && !hasData;

  LoadingState<T> copyWith({bool? isLoading, String? error, T? data, bool clearError = false, bool clearData = false}) {
    return LoadingState._(
      isLoading: isLoading ?? this.isLoading,
      error: clearError ? null : (error ?? this.error),
      data: clearData ? null : (data ?? this.data),
    );
  }
}

/// A state notifier that manages loading states for async operations.
class AsyncOperationNotifier<T> extends ValueNotifier<LoadingState<T>> {
  AsyncOperationNotifier() : super(const LoadingState.initial());

  /// Execute an async operation with automatic loading/error handling.
  Future<T?> execute(Future<T> Function() operation) async {
    value = const LoadingState.loading();
    try {
      final result = await operation();
      value = LoadingState.success(result);
      return result;
    } catch (e) {
      value = LoadingState.failure(_friendlyMessage(e));
      return null;
    }
  }

  void reset() => value = const LoadingState.initial();

  String _friendlyMessage(Object error) {
    final msg = error.toString();
    if (msg.contains('SocketException') || msg.contains('Connection')) {
      return 'Sin conexión a internet. Verifica tu red.';
    }
    if (msg.contains('401') || msg.contains('Unauthorized')) {
      return 'Sesión expirada. Inicia sesión nuevamente.';
    }
    if (msg.contains('403') || msg.contains('Forbidden')) {
      return 'No tienes permiso para realizar esta acción.';
    }
    if (msg.contains('404') || msg.contains('Not Found')) {
      return 'Recurso no encontrado.';
    }
    if (msg.contains('429') || msg.contains('Too Many')) {
      return 'Demasiadas solicitudes. Espera un momento.';
    }
    if (msg.contains('500') || msg.contains('Internal Server')) {
      return 'Error del servidor. Intenta más tarde.';
    }
    if (msg.contains('timeout') || msg.contains('Timeout')) {
      return 'La operación tardó demasiado. Intenta de nuevo.';
    }
    return 'Ocurrió un error inesperado.';
  }
}

/// Convenience wrapper widget for loading states.
class LoadingStateBuilder<T> extends StatelessWidget {
  final LoadingState<T> state;
  final Widget Function(T data) onSuccess;
  final Widget Function()? onLoading;
  final Widget Function(String error)? onError;
  final Widget Function()? onIdle;

  const LoadingStateBuilder({
    super.key,
    required this.state,
    required this.onSuccess,
    this.onLoading,
    this.onError,
    this.onIdle,
  });

  @override
  Widget build(BuildContext context) {
    if (state.isLoading) {
      return onLoading?.call() ?? const Center(child: CircularProgressIndicator());
    }
    if (state.hasError) {
      return onError?.call(state.error!) ?? Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: Colors.red),
            const SizedBox(height: 16),
            Text(state.error!, textAlign: TextAlign.center),
          ],
        ),
      );
    }
    if (state.hasData) {
      return onSuccess(state.data as T);
    }
    return onIdle?.call() ?? const SizedBox.shrink();
  }
}