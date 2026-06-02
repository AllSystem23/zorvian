import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/brand_provider.dart';

final class BrandListPage extends ConsumerStatefulWidget {
  const BrandListPage({super.key});
  @override
  ConsumerState<BrandListPage> createState() => _BrandListPageState();
}

final class _BrandListPageState extends ConsumerState<BrandListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(brandProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(brandProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Marcas')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay marcas'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(brandProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final b = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.primaryContainer,
                              child: Icon(Icons.branding_watermark, color: theme.colorScheme.onPrimaryContainer),
                            ),
                            title: Text(b.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(b.description ?? ''),
                          );
                        },
                      ),
                    ),
    );
  }
}
