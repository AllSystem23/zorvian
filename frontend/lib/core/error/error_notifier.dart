import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

enum ErrorSeverity { info, warning, error }

class AppError {
  final String message;
  final String? detail;
  final ErrorSeverity severity;
  final VoidCallback? onRetry;

  const AppError({
    required this.message,
    this.detail,
    this.severity = ErrorSeverity.error,
    this.onRetry,
  });
}

class ErrorNotifier extends Notifier<List<AppError>> {
  @override
  List<AppError> build() => [];

  void showError(String message, {String? detail, VoidCallback? onRetry}) {
    _add(AppError(message: message, detail: detail, severity: ErrorSeverity.error, onRetry: onRetry));
  }

  void showWarning(String message, {String? detail}) {
    _add(AppError(message: message, detail: detail, severity: ErrorSeverity.warning));
  }

  void showInfo(String message) {
    _add(AppError(message: message, severity: ErrorSeverity.info));
  }

  void _add(AppError error) {
    state = [...state, error];
  }

  void dismiss(AppError error) {
    state = state.where((e) => e != error).toList();
  }

  void clear() => state = [];

  String friendlyHttpError(int? statusCode) {
    return switch (statusCode) {
      400 => 'Solicitud inválida. Verifique los datos ingresados.',
      401 => 'Sesión expirada. Inicie sesión nuevamente.',
      403 => 'No tiene permiso para realizar esta acción.',
      404 => 'El recurso solicitado no existe.',
      409 => 'Conflicto. El recurso ya existe o hay datos duplicados.',
      422 => 'Datos inválidos. Verifique los campos.',
      429 => 'Demasiadas solicitudes. Intente más tarde.',
      500 => 'Error interno del servidor. Intente más tarde.',
      502 => 'El servicio no está disponible momentáneamente.',
      503 => 'Servicio en mantenimiento. Intente más tarde.',
      _ => 'Ha ocurrido un error inesperado.',
    };
  }
}

final errorNotifierProvider = NotifierProvider<ErrorNotifier, List<AppError>>(
  ErrorNotifier.new,
);
