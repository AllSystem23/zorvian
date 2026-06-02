import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/category_provider.dart';

final class CategoryListPage extends ConsumerStatefulWidget {
  const CategoryListPage({super.key});
  @override
  ConsumerState<CategoryListPage> createState() => _CategoryListPageState();
}

final class _CategoryListPageState extends ConsumerState<CategoryListPage> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(categoryProvider.notifier).load());
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(categoryProvider);
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Categorías')),
      body: state.loading
          ? const Center(child: CircularProgressIndicator())
          : state.error != null
              ? Center(child: Text(state.error!, style: TextStyle(color: theme.colorScheme.error)))
              : state.items.isEmpty
                  ? const Center(child: Text('No hay categorías'))
                  : RefreshIndicator(
                      onRefresh: () => ref.read(categoryProvider.notifier).load(),
                      child: ListView.separated(
                        itemCount: state.items.length,
                        separatorBuilder: (_, _) => const Divider(height: 1),
                        itemBuilder: (_, i) {
                          final c = state.items[i];
                          return ListTile(
                            leading: CircleAvatar(
                              backgroundColor: theme.colorScheme.primaryContainer,
                              child: Icon(Icons.category, color: theme.colorScheme.onPrimaryContainer),
                            ),
                            title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w600)),
                            subtitle: Text(c.description ?? ''),
                          );
                        },
                      ),
                    ),
    );
  }
}
