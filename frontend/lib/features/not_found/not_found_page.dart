import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../shared/ds/ds.dart';

class NotFoundPage extends StatelessWidget {
  final GoRouterState? state;
  const NotFoundPage({super.key, this.state});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final path = state?.uri.toString() ?? '';
    final error = state?.error;

    return Scaffold(
      body: Center(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.explore_off_outlined, size: 64, color: theme.colorScheme.error),
              const SizedBox(height: 16),
              Text(
                'Página no encontrada',
                style: theme.textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              Text(
                path.isNotEmpty ? 'La ruta "$path" no existe' : 'La página que buscas no está disponible',
                textAlign: TextAlign.center,
                style: theme.textTheme.bodyLarge?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                ),
              ),
              if (error != null) ...[
                const SizedBox(height: 8),
                Text(
                  error.toString(),
                  textAlign: TextAlign.center,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.error,
                  ),
                  maxLines: 3,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
              const SizedBox(height: 32),
              ZButton(
                text: 'Volver al inicio',
                onPressed: () => context.go('/dashboard'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
