import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/leads_provider.dart';
import '../providers/opportunities_provider.dart';
import '../widgets/kanban_board.dart';
import '../widgets/leads_view.dart';

class CRMPage extends ConsumerStatefulWidget {
  const CRMPage({super.key});

  @override
  ConsumerState<CRMPage> createState() => _CRMPageState();
}

class _CRMPageState extends ConsumerState<CRMPage>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);

    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(leadsProvider.notifier).loadLeads();
      ref.read(opportunitiesProvider.notifier).loadPipeline();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Zorvian CRM', style: Theme.of(context).textTheme.titleLarge),
                IconButton(
                  icon: const Icon(Icons.refresh),
                  onPressed: () {
                    ref.read(leadsProvider.notifier).loadLeads();
                    ref.read(opportunitiesProvider.notifier).loadPipeline();
                  },
                ),
              ],
            ),
          ),
          TabBar(
            controller: _tabController,
            tabs: const [
              Tab(
                text: 'Pipeline (Kanban)',
                icon: Icon(Icons.view_column_outlined),
              ),
              Tab(
                text: 'Prospectos (Leads)',
                icon: Icon(Icons.people_alt_outlined),
              ),
            ],
          ),
          Expanded(
            child: TabBarView(
              controller: _tabController,
              children: const [KanbanBoard(), LeadsView()],
            ),
          ),
        ],
      ),
    );
  }
}
