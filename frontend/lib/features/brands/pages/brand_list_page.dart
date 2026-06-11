import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../shared/ds/components/z_async_renderer.dart';
import '../providers/brand_provider.dart';

final class BrandListPage extends ConsumerWidget {
  const BrandListPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final brandAsync = ref.watch(brandProvider);
    return Scaffold(
      appBar: AppBar(title: const Text('Marcas')),
      body: ZAsyncRenderer<List<BrandItem>>(
        value: brandAsync,
        builder: (items) => RefreshIndicator(
          onRefresh: () => ref.refresh(brandProvider.future),
          child: ListView.separated(
            itemCount: items.length,
            separatorBuilder: (_, _) => const Divider(height: 1),
            itemBuilder: (_, i) {
              final b = items[i];
              return ListTile(
                leading: CircleAvatar(
                  backgroundColor: Theme.of(context).colorScheme.primaryContainer,
                  child: Icon(Icons.branding_watermark, color: Theme.of(context).colorScheme.onPrimaryContainer),
                ),
                title: Text(b.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                subtitle: Text(b.description ?? ''),
              );
            },
          ),
        ),
      ),
    );
  }
}
