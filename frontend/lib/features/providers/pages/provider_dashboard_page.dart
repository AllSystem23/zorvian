import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fl_chart/fl_chart.dart';
import '../../../auth/auth_provider.dart';
import '../../../core/widgets/responsive_layout.dart';
import '../../../shared/ds/ds.dart';

class _CountryNotifier extends Notifier<String?> {
  @override
  String? build() => null;
  void setCountry(String? code) => state = code;
}

final selectedCountryProvider = NotifierProvider<_CountryNotifier, String?>(_CountryNotifier.new);

final providerDashboardProvider = FutureProvider<Map<String, dynamic>?>((ref) async {
  final dio = ref.read(dioClientProvider);
  final country = ref.watch(selectedCountryProvider);
  try {
    final r = await dio.get('providers/dashboard', params: { if (country != null) 'countryCode': country });
    return r.data as Map<String, dynamic>;
  } catch (_) {
    return null;
  }
});

final providerNotificationsProvider = FutureProvider<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final country = ref.watch(selectedCountryProvider);
  try {
    final r = await dio.get('providers/notifications', params: { if (country != null) 'countryCode': country });
    return (r.data as List).map((e) => Map<String, dynamic>.from(e as Map)).toList();
  } catch (_) {
    return [];
  }
});

final providerRankingsProvider = FutureProvider<Map<String, dynamic>?>((ref) async {
  final dio = ref.read(dioClientProvider);
  final country = ref.watch(selectedCountryProvider);
  try {
    final r = await dio.get('providers/rankings', params: { if (country != null) 'countryCode': country });
    return r.data as Map<String, dynamic>;
  } catch (_) {
    return null;
  }
});

final providerRankingHistoryProvider = FutureProvider<Map<String, dynamic>?>((ref) async {
  final dio = ref.read(dioClientProvider);
  final country = ref.watch(selectedCountryProvider);
  try {
    final r = await dio.get('providers/rankings/history', params: { if (country != null) 'countryCode': country });
    return r.data as Map<String, dynamic>;
  } catch (_) {
    return null;
  }
});

class ProviderDashboardPage extends ConsumerWidget {
  const ProviderDashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboardAsync = ref.watch(providerDashboardProvider);
    final notificationsAsync = ref.watch(providerNotificationsProvider);
    final rankingsAsync = ref.watch(providerRankingsProvider);
    final historyAsync = ref.watch(providerRankingHistoryProvider);

    final currentCountry = ref.watch(selectedCountryProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard de Prestadores'),
        actions: [
          SizedBox(
            width: 140,
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 8),
              child: DropdownButtonFormField<String?>(
                value: currentCountry,
                isDense: true,
                decoration: InputDecoration(
                  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(8), borderSide: const BorderSide(color: ZColors.neutral300)),
                  filled: true,
                  fillColor: Colors.white,
                ),
                items: const [
                  DropdownMenuItem<String?>(value: null, child: Text('Todos los países')),
                  DropdownMenuItem<String?>(value: 'NI', child: Text('Nicaragua')),
                  DropdownMenuItem<String?>(value: 'HN', child: Text('Honduras')),
                  DropdownMenuItem<String?>(value: 'SV', child: Text('El Salvador')),
                  DropdownMenuItem<String?>(value: 'CR', child: Text('Costa Rica')),
                  DropdownMenuItem<String?>(value: 'GT', child: Text('Guatemala')),
                  DropdownMenuItem<String?>(value: 'PA', child: Text('Panamá')),
                ],
                onChanged: (val) {
                  ref.read(selectedCountryProvider.notifier).setCountry(val);
                },
              ),
            ),
          ),
          const SizedBox(width: 8),
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Actualizar',
            onPressed: () {
              ref.invalidate(providerDashboardProvider);
              ref.invalidate(providerNotificationsProvider);
              ref.invalidate(providerRankingsProvider);
              ref.invalidate(providerRankingHistoryProvider);
            },
          ),
        ],
      ),
      body: dashboardAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error: $e')),
        data: (data) {
          if (data == null) {
            return const Center(child: Text('No hay datos disponibles'));
          }
          return SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _buildKPIRow(data),
                const SizedBox(height: 24),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(flex: 2, child: _buildMilestonesChart(data)),
                    const SizedBox(width: 16),
                    Expanded(flex: 1, child: _buildTopProviders(data)),
                  ],
                ),
                const SizedBox(height: 24),
                _buildRecentContracts(data),
                const SizedBox(height: 24),
                _buildRankingsSection(rankingsAsync),
                const SizedBox(height: 24),
                _buildRankingHistoryChart(historyAsync),
                const SizedBox(height: 24),
                _buildNotifications(notificationsAsync),
              ],
            ),
          );
        },
      ),
    );
  }

  Widget _buildKPIRow(Map<String, dynamic> data) {
    return ResponsiveGrid(
      mobileColumns: 2,
      tabletColumns: 4,
      desktopColumns: 6,
      children: [
        _KpiTile(
          icon: Icons.business_outlined,
          label: 'Prestadores',
          value: '${data['totalProviders'] ?? 0}',
          subtitle: '${data['activeProviders'] ?? 0} activos',
          color: ZColors.moduleTreasury,
        ),
        _KpiTile(
          icon: Icons.description_outlined,
          label: 'Contratos',
          value: '${data['totalContracts'] ?? 0}',
          subtitle: '${data['activeContracts'] ?? 0} activos',
          color: ZColors.modulePurchases,
        ),
        _KpiTile(
          icon: Icons.attach_money,
          label: 'Valor Total',
          value: _formatCurrency(data['totalContractValue']),
          color: ZColors.moduleSales,
        ),
        _KpiTile(
          icon: Icons.hourglass_top,
          label: 'Hitos Pendientes',
          value: '${data['pendingMilestones'] ?? 0}',
          color: ZColors.warning,
        ),
        _KpiTile(
          icon: Icons.warning_amber_rounded,
          label: 'Hitos Vencidos',
          value: '${data['overdueMilestones'] ?? 0}',
          color: ZColors.danger,
        ),
        _KpiTile(
          icon: Icons.receipt_long,
          label: 'Facturas Pendientes',
          value: '${data['pendingInvoices'] ?? 0}',
          subtitle: _formatCurrency(data['pendingInvoiceAmount']),
          color: ZColors.moduleFinance,
        ),
      ],
    );
  }

  Widget _buildMilestonesChart(Map<String, dynamic> data) {
    final milestonesByStatus = (data['milestonesByStatus'] as List?)
            ?.map((e) => Map<String, dynamic>.from(e as Map))
            .toList() ??
        [];

    final colors = {
      'pending': ZColors.neutral400,
      'in_progress': ZColors.moduleFinance,
      'pending_approval': ZColors.warning,
      'completed': ZColors.success,
      'approved': ZColors.brandAccent,
      'paid': ZColors.moduleSales,
      'cancelled': ZColors.danger,
    };

    final labels = {
      'pending': 'Pendiente',
      'in_progress': 'En Progreso',
      'pending_approval': 'Esperando Aprobación',
      'completed': 'Completado',
      'approved': 'Aprobado',
      'paid': 'Pagado',
      'cancelled': 'Cancelado',
    };

    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Hitos por Estado', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          if (milestonesByStatus.isEmpty)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(32),
                child: Text('Sin hitos registrados', style: TextStyle(color: Colors.grey)),
              ),
            )
          else
            ...milestonesByStatus.map((m) {
              final status = m['status'] as String;
              final count = m['count'] as int;
              final total = milestonesByStatus.fold<int>(0, (sum, e) => sum + (e['count'] as int));
              final pct = total > 0 ? count / total : 0.0;
              return Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Container(
                          width: 10,
                          height: 10,
                          decoration: BoxDecoration(
                            color: colors[status] ?? Colors.grey,
                            shape: BoxShape.circle,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(child: Text(labels[status] ?? status, style: ZTypography.labelMedium)),
                        Text('$count', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.w600)),
                      ],
                    ),
                    const SizedBox(height: 4),
                    LinearProgressIndicator(
                      value: pct,
                      backgroundColor: ZColors.neutral200,
                      color: colors[status] ?? Colors.grey,
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(3),
                    ),
                  ],
                ),
              );
            }),
        ],
      ),
    );
  }

  Widget _buildTopProviders(Map<String, dynamic> data) {
    final topProviders = (data['topProviders'] as List?)
            ?.map((e) => Map<String, dynamic>.from(e as Map))
            .toList() ??
        [];

    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Top Prestadores', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 16),
          if (topProviders.isEmpty)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(32),
                child: Text('Sin datos', style: TextStyle(color: Colors.grey)),
              ),
            )
          else
            ...topProviders.asMap().entries.map((entry) {
              final i = entry.key;
              final p = entry.value;
              return ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: CircleAvatar(
                  radius: 16,
                  backgroundColor: ZColors.brandPrimary.withValues(alpha: 0.1),
                  child: Text('${i + 1}', style: TextStyle(fontSize: 12, color: ZColors.brandPrimary)),
                ),
                title: Text(p['businessName'] as String? ?? '', style: ZTypography.labelMedium),
                subtitle: Text('${p['contractCount']} contratos', style: ZTypography.labelSmall),
                trailing: Text(_formatCurrency(p['totalValue']), style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.w600)),
              );
            }),
        ],
      ),
    );
  }

  Widget _buildRecentContracts(Map<String, dynamic> data) {
    final recentContracts = (data['recentContracts'] as List?)
            ?.map((e) => Map<String, dynamic>.from(e as Map))
            .toList() ??
        [];

    return ZCard(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('Contratos Recientes', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
              TextButton(
                onPressed: () {},
                child: const Text('Ver todos'),
              ),
            ],
          ),
          const SizedBox(height: 8),
          if (recentContracts.isEmpty)
            const Center(
              child: Padding(
                padding: EdgeInsets.all(32),
                child: Text('Sin contratos', style: TextStyle(color: Colors.grey)),
              ),
            )
          else
            ...recentContracts.map((c) {
              final status = c['status'] as String? ?? '';
              return ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.description_outlined, color: ZColors.modulePurchases),
                title: Text('${c['contractNumber']} - ${c['contractName']}', style: ZTypography.labelMedium),
                subtitle: Text(c['providerName'] as String? ?? '', style: ZTypography.labelSmall),
                trailing: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(_formatCurrency(c['totalAmount']), style: ZTypography.labelSmall.copyWith(fontWeight: FontWeight.w600)),
                    const SizedBox(width: 8),
                    ZBadge(text: status.toUpperCase(), type: ZBadgeType.neutral),
                  ],
                ),
              );
            }),
        ],
      ),
    );
  }

  Widget _buildNotifications(AsyncValue<List<Map<String, dynamic>>> notificationsAsync) {
    return notificationsAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, e) => const SizedBox.shrink(),
      data: (notifications) {
        if (notifications.isEmpty) return const SizedBox.shrink();
        return ZCard(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.notifications_outlined, size: 20, color: ZColors.warning),
                  const SizedBox(width: 8),
                  Text('Alertas (${notifications.length})', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 12),
              ...notifications.take(10).map((n) {
                final severity = n['severity'] as String? ?? 'low';
                final icon = switch (n['type']) {
                  'contract_expired' || 'contract_expiring' => Icons.event_busy,
                  'milestone_overdue' || 'milestone_due_soon' => Icons.timer_off_outlined,
                  'invoice_pending' => Icons.receipt_long,
                  _ => Icons.info_outline,
                };
                final color = switch (severity) {
                  'high' => ZColors.danger,
                  'medium' => ZColors.warning,
                  _ => ZColors.brandPrimary,
                };
                return ListTile(
                  dense: true,
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(icon, color: color, size: 20),
                  title: Text(n['title'] as String? ?? '', style: ZTypography.labelMedium),
                  subtitle: Text(n['message'] as String? ?? '', style: ZTypography.labelSmall),
                );
              }),
            ],
          ),
        );
      },
    );
  }

  Widget _buildRankingsSection(AsyncValue<Map<String, dynamic>?> rankingsAsync) {
    return rankingsAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, e) => const SizedBox.shrink(),
      data: (data) {
        if (data == null) return const SizedBox.shrink();
        final rankings = (data['rankings'] as List?)
                ?.map((e) => Map<String, dynamic>.from(e as Map))
                .toList() ??
            [];
        final avgScore = data['averageOverallScore'] ?? 0;

        return ZCard(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.leaderboard_outlined, size: 20, color: ZColors.brandAccent),
                  const SizedBox(width: 8),
                  Text('Ranking de Prestadores', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
                  const Spacer(),
                  ZBadge(text: 'Promedio: $avgScore', type: ZBadgeType.info),
                ],
              ),
              const SizedBox(height: 16),
              if (rankings.isEmpty)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Text('Sin datos de ranking', style: TextStyle(color: Colors.grey)),
                  ),
                )
              else
                ...rankings.map((r) => _RankingTile(data: r)),
            ],
          ),
        );
      },
    );
  }

  Widget _buildRankingHistoryChart(AsyncValue<Map<String, dynamic>?> historyAsync) {
    return historyAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, e) => const SizedBox.shrink(),
      data: (data) {
        if (data == null) return const SizedBox.shrink();
        final seriesList = (data['series'] as List?)
                ?.map((e) => Map<String, dynamic>.from(e as Map))
                .toList() ?? [];
        if (seriesList.isEmpty) return const SizedBox.shrink();

        final colors = [
          ZColors.brandPrimary, ZColors.moduleSales, ZColors.moduleFinance,
          ZColors.modulePurchases, ZColors.moduleHr, ZColors.moduleCrm,
          ZColors.moduleInventory, ZColors.moduleTreasury, ZColors.brandAccent, ZColors.success,
        ];

        return ZCard(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.show_chart, size: 20, color: ZColors.brandPrimary),
                  const SizedBox(width: 8),
                  Text('Evolución de Scores (6 meses)', style: ZTypography.bodyMedium.copyWith(fontWeight: FontWeight.bold)),
                ],
              ),
              const SizedBox(height: 16),
              SizedBox(
                height: 300,
                child: _RankingLineChart(seriesList: seriesList, colors: colors),
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 16,
                runSpacing: 8,
                children: seriesList.asMap().entries.map((entry) {
                  final idx = entry.key;
                  final s = entry.value;
                  return Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(width: 12, height: 12, decoration: BoxDecoration(color: colors[idx % colors.length], borderRadius: BorderRadius.circular(2))),
                      const SizedBox(width: 4),
                      Text(s['businessName'] as String? ?? '', style: ZTypography.labelSmall),
                    ],
                  );
                }).toList(),
              ),
            ],
          ),
        );
      },
    );
  }

  static String _formatCurrency(dynamic value) {
    if (value == null) return 'N/A';
    final d = (value as num).toDouble();
    if (d >= 1000000) return '\$${(d / 1000000).toStringAsFixed(1)}M';
    if (d >= 1000) return '\$${(d / 1000).toStringAsFixed(1)}K';
    return '\$${d.toStringAsFixed(2)}';
  }
}

class _RankingTile extends StatelessWidget {
  final Map<String, dynamic> data;
  const _RankingTile({required this.data});

  @override
  Widget build(BuildContext context) {
    final rank = data['rank'] as int? ?? 0;
    final score = (data['overallScore'] as num?)?.toDouble() ?? 0;
    final onTime = (data['onTimeDeliveryScore'] as num?)?.toDouble() ?? 0;
    final completion = (data['contractCompletionScore'] as num?)?.toDouble() ?? 0;
    final accuracy = (data['invoiceAccuracyScore'] as num?)?.toDouble() ?? 0;
    final medal = rank == 1 ? '🥇' : rank == 2 ? '🥈' : rank == 3 ? '🥉' : '';

    final scoreColor = score >= 80 ? ZColors.success
        : score >= 50 ? ZColors.warning
        : ZColors.danger;

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: rank == 1 ? ZColors.brandAccent.withValues(alpha: 0.05) : null,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: rank == 1 ? ZColors.brandAccent.withValues(alpha: 0.3) : ZColors.neutral200,
        ),
      ),
      child: Column(
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: scoreColor.withValues(alpha: 0.15),
                child: Text(medal.isNotEmpty ? medal : '$rank',
                    style: TextStyle(fontSize: medal.isNotEmpty ? 18.0 : 14.0, fontWeight: FontWeight.bold, color: scoreColor)),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(data['businessName'] as String? ?? '',
                        style: ZTypography.labelMedium.copyWith(fontWeight: FontWeight.w600)),
                    Text(data['employeeName'] as String? ?? '',
                        style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Text(score.toStringAsFixed(1),
                      style: ZTypography.headlineSmall.copyWith(fontWeight: FontWeight.bold, color: scoreColor)),
                  Text('SCORE', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, fontSize: 10)),
                ],
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              _ScoreChip(label: 'Entregas', score: onTime, color: ZColors.moduleSales),
              const SizedBox(width: 8),
              _ScoreChip(label: 'Contratos', score: completion, color: ZColors.modulePurchases),
              const SizedBox(width: 8),
              _ScoreChip(label: 'Facturas', score: accuracy, color: ZColors.moduleFinance),
            ],
          ),
        ],
      ),
    );
  }
}

class _ScoreChip extends StatelessWidget {
  final String label;
  final double score;
  final Color color;
  const _ScoreChip({required this.label, required this.score, required this.color});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.08),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Column(
          children: [
            Text(label, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, fontSize: 10)),
            const SizedBox(height: 2),
            LinearProgressIndicator(
              value: score / 100,
              backgroundColor: ZColors.neutral200,
              color: color,
              minHeight: 4,
              borderRadius: BorderRadius.circular(2),
            ),
            const SizedBox(height: 2),
            Text('${score.toStringAsFixed(0)}%', style: ZTypography.labelSmall.copyWith(fontWeight: FontWeight.w600, fontSize: 11)),
          ],
        ),
      ),
    );
  }
}

class _KpiTile extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final String? subtitle;
  final Color color;

  const _KpiTile({
    required this.icon,
    required this.label,
    required this.value,
    this.subtitle,
    required this.color,
  });

  @override
  Widget build(BuildContext context) {
    return ZCard(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.all(6),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, size: 16, color: color),
              ),
              const Spacer(),
            ],
          ),
          const SizedBox(height: 12),
          Text(value, style: ZTypography.headlineSmall.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 2),
          Text(label, style: ZTypography.labelSmall),
          if (subtitle != null)
            Padding(
              padding: const EdgeInsets.only(top: 2),
              child: Text(subtitle!, style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
            ),
        ],
      ),
    );
  }
}

class _RankingLineChart extends StatelessWidget {
  final List<Map<String, dynamic>> seriesList;
  final List<Color> colors;
  const _RankingLineChart({required this.seriesList, required this.colors});

  @override
  Widget build(BuildContext context) {
    if (seriesList.isEmpty) return const SizedBox.shrink();

    final allMonths = <String>{};
    for (final s in seriesList) {
      final points = s['points'] as List? ?? [];
      for (final p in points) {
        allMonths.add((p as Map<String, dynamic>)['month'] as String? ?? '');
      }
    }
    final sortedMonths = allMonths.toList()..sort();
    final monthLabels = sortedMonths.map((m) {
      final parts = m.split('-');
      return parts.length == 2 ? '${parts[1]}/${parts[0].substring(2)}' : m;
    }).toList();

    final spots = <int, List<FlSpot>>{};
    for (var si = 0; si < seriesList.length; si++) {
      final points = (seriesList[si]['points'] as List?)
              ?.map((e) => Map<String, dynamic>.from(e as Map))
              .toList() ?? [];
      spots[si] = [];
      for (final p in points) {
        final month = p['month'] as String? ?? '';
        final idx = sortedMonths.indexOf(month);
        if (idx >= 0) {
          final score = (p['overallScore'] as num?)?.toDouble() ?? 0;
          spots[si]!.add(FlSpot(idx.toDouble(), score));
        }
      }
      spots[si]!.sort((a, b) => a.x.compareTo(b.x));
    }

    return LineChart(
      LineChartData(
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          horizontalInterval: 25,
          getDrawingHorizontalLine: (v) => FlLine(color: ZColors.neutral200, strokeWidth: 1),
        ),
        titlesData: FlTitlesData(
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true, reservedSize: 40,
              interval: 25,
              getTitlesWidget: (v, meta) => Text('${v.toInt()}', style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500)),
            ),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true, reservedSize: 30,
              interval: 1,
              getTitlesWidget: (v, meta) {
                final i = v.toInt();
                if (i < 0 || i >= monthLabels.length) return const SizedBox.shrink();
                return Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: Text(monthLabels[i], style: ZTypography.labelSmall.copyWith(color: ZColors.neutral500, fontSize: 10)),
                );
              },
            ),
          ),
          topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
        ),
        borderData: FlBorderData(show: false),
        minY: 0, maxY: 100,
        lineBarsData: spots.entries.map((entry) {
          final idx = entry.key;
          final pts = entry.value;
          final color = colors[idx % colors.length];
          return LineChartBarData(
            spots: pts,
            isCurved: true,
            color: color,
            barWidth: 2.5,
            isStrokeCapRound: true,
            dotData: FlDotData(
              show: true,
              getDotPainter: (spot, pct, bar, i) => FlDotCirclePainter(
                radius: 3, color: color, strokeWidth: 1.5, strokeColor: Colors.white,
              ),
            ),
            belowBarData: BarAreaData(
              show: true,
              color: color.withValues(alpha: 0.08),
            ),
          );
        }).toList(),
        lineTouchData: LineTouchData(
          touchTooltipData: LineTouchTooltipData(
            getTooltipColor: (_) => ZColors.darkSurface,
            getTooltipItems: (touchedSpots) {
              return touchedSpots.map((spot) {
                final seriesIdx = spot.barIndex;
                final name = seriesList.length > seriesIdx
                    ? (seriesList[seriesIdx]['businessName'] as String? ?? '')
                    : '';
                return LineTooltipItem(
                  '$name: ${spot.y.toStringAsFixed(1)}%',
                  TextStyle(color: colors[seriesIdx % colors.length], fontSize: 12, fontWeight: FontWeight.w600),
                );
              }).toList();
            },
          ),
          handleBuiltInTouches: true,
        ),
      ),
    );
  }
}
