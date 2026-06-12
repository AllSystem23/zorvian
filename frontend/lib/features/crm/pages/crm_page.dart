import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/leads_provider.dart';
import '../providers/opportunities_provider.dart';
import '../widgets/crm_forms.dart';
import '../widgets/kanban_board.dart';
import '../widgets/leads_view.dart';

class CRMPage extends ConsumerStatefulWidget {
  const CRMPage({super.key});

  @override
  ConsumerState<CRMPage> createState() => _CRMPageState();
}

class _CRMPageState extends ConsumerState<CRMPage> with SingleTickerProviderStateMixin {
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
      appBar: AppBar(
        title: const Text('Zorvian CRM'),
        bottom: TabBar(
          controller: _tabController,
          tabs: const [
            Tab(text: 'Pipeline (Kanban)', icon: Icon(Icons.view_column_outlined)),
            Tab(text: 'Prospectos (Leads)', icon: Icon(Icons.people_alt_outlined)),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              ref.read(leadsProvider.notifier).loadLeads();
              ref.read(opportunitiesProvider.notifier).loadPipeline();
            },
          ),
        ],
      ),
      body: TabBarView(
        controller: _tabController,
        children: const [
          KanbanBoard(),
          LeadsView(),
        ],
      ),
      floatingActionButton: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.end,
        children: [
          FloatingActionButton.small(
            heroTag: 'fab_lead',
            onPressed: () => _showAddLeadSheet(context),
            tooltip: 'Nuevo Lead',
            child: const Icon(Icons.person_add_alt_1_outlined),
          ),
          const SizedBox(height: 12),
          FloatingActionButton.small(
            heroTag: 'fab_opportunity',
            onPressed: () => _showAddOpportunitySheet(context),
            tooltip: 'Nueva Oportunidad',
            child: const Icon(Icons.add_chart_outlined),
          ),
        ],
      ),
    );
  }

  void _showAddLeadSheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => const AddLeadSheet(),
    );
  }

  void _showAddOpportunitySheet(BuildContext context) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      builder: (context) => const AddOpportunitySheet(),
    );
  }
}
