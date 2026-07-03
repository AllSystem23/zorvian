import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../auth/auth_provider.dart';
import '../../../auth/tenants_provider.dart';
import '../../navigation/nav_provider.dart';
import '../../providers/company_branch_provider.dart';
import '../../services/signalr_service.dart';
import '../../theme/theme_provider.dart';
import '../../../core/providers/company_currency_provider.dart';
import '../../../shared/ds/ds.dart';

/// GlobalHeader — Persistent top bar across all pages.
/// Contains: Logo, Company/Branch selectors, Search, Notifications, Chat, Theme, User.
final class GlobalHeader extends ConsumerWidget {
  const GlobalHeader({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final isDark = theme.brightness == Brightness.dark;
    final collapsed = ref.watch(sidebarCollapsedProvider);

    return Container(
      height: 56,
      decoration: BoxDecoration(
        color: isDark ? ZColors.darkSurface : ZColors.surface,
        border: Border(
          bottom: BorderSide(
            color: isDark
                ? ZColors.darkBorder.withValues(alpha: 0.5)
                : ZColors.border,
            width: 1,
          ),
        ),
      ),
      padding: EdgeInsets.symmetric(horizontal: collapsed ? ZSpacing.md : ZSpacing.lg),
      child: Row(
        children: [
          // ── Logo (compact) ──
          _LogoCompact(),
          SizedBox(width: ZSpacing.md),

          // ── Tenant Switcher ──
          _TenantSwitcher(),
          SizedBox(width: ZSpacing.sm),

          // ── Company Selector ──
          _CompanySelector(),
          SizedBox(width: ZSpacing.sm),

          // ── Branch Selector ──
          _BranchSelector(),
          SizedBox(width: ZSpacing.sm),

          // ── Search Bar (Center) ──
          Expanded(
            child: _SearchBar(),
          ),

          SizedBox(width: ZSpacing.md),

          // ── Quick Actions (Right) ──
          _QuickActions(),
        ],
      ),
    );
  }
}

/// Compact logo shown in header
class _LogoCompact extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(ZRadii.sm),
          child: Image.asset(
            ZAssets.logoErp,
            width: 24,
            height: 24,
            fit: BoxFit.contain,
            excludeFromSemantics: true,
          ),
        ),
      ],
    );
  }
}

/// Company selector — real dropdown connected to backend
/// For SuperAdmin, selecting a company triggers switch-tenant to set backend context.
class _CompanySelector extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final companyBranch = ref.watch(companyBranchProvider);
    final companiesAsync = ref.watch(companyListProvider);
    final auth = ref.watch(authProvider);
    final isSuperAdmin = auth.role == 'SuperAdmin';

    return companiesAsync.when(
      loading: () => _SelectorChip(
        icon: Icons.business_outlined,
        label: companyBranch.companyName ?? 'Empresa',
        isDark: isDark,
        isLoading: true,
      ),
      error: (_, _) => _SelectorChip(
        icon: Icons.business_outlined,
        label: companyBranch.companyName ?? 'Empresa',
        isDark: isDark,
      ),
      data: (companies) {
        if (companies.isEmpty) {
          return _SelectorChip(
            icon: Icons.business_outlined,
            label: isSuperAdmin ? 'Gestionar empresas' : 'Sin empresas',
            isDark: isDark,
          );
        }
        // Auto-select first company if none selected yet
        if (companyBranch.companyId == null && companies.isNotEmpty) {
          final first = companies.first;
          final firstName = first['name'] ?? first['legalName'] ?? 'Empresa';
          final firstId = first['id'] as String? ?? '';
          final tenantId = first['tenantId'] as String? ?? '';
          WidgetsBinding.instance.addPostFrameCallback((_) {
            ref.read(companyBranchProvider.notifier).selectCompany(firstId, firstName);
            // For SuperAdmin, switch to this tenant on first load
            if (isSuperAdmin && tenantId.isNotEmpty) {
              _switchToTenant(ref, tenantId);
            }
          });
        }
        final displayName = companyBranch.companyName ??
            (companies.isNotEmpty ? (companies.first['name'] ?? companies.first['legalName'] ?? 'Empresa') : 'Empresa');
        return MenuAnchor(
          menuChildren: companies.map((c) {
            final name = c['name'] ?? c['legalName'] ?? 'Sin nombre';
            final id = c['id'] ?? '';
            final tenantId = c['tenantId'] as String? ?? '';
            final isActive = c['isActive'] as bool? ?? true;
            final isSelected = companyBranch.companyId == id;
            return MenuItemButton(
              leadingIcon: isSelected
                  ? const Icon(Icons.check, size: 16, color: ZColors.brandPrimary)
                  : const Icon(Icons.business_outlined, size: 16),
              trailingIcon: !isActive
                  ? const Icon(Icons.block, size: 14, color: ZColors.danger)
                  : null,
              child: Text(name, style: TextStyle(
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
                color: !isActive ? ZColors.neutral400 : null,
              )),
              onPressed: () {
                ref.read(companyBranchProvider.notifier).selectCompany(id, name);
                // For SuperAdmin, switch tenant to set backend context
                if (isSuperAdmin && tenantId.isNotEmpty) {
                  _switchToTenant(ref, tenantId);
                }
              },
            );
          }).toList(),
          builder: (context, controller, child) {
            return GestureDetector(
              onTap: controller.open,
              child: _SelectorChip(
                icon: Icons.business_outlined,
                label: displayName,
                isDark: isDark,
              ),
            );
          },
        );
      },
    );
  }

  Future<void> _switchToTenant(WidgetRef ref, String tenantId) async {
    // switchTenant updates authProvider state, which companyListProvider watches,
    // triggering an automatic re-fetch with the new tenant context.
    await ref.read(authProvider.notifier).switchTenant(tenantId);
    // Also invalidate branches since the tenant context changed
    ref.invalidate(headerBranchListProvider);
  }
}

/// Branch selector — real dropdown connected to backend
class _BranchSelector extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final companyBranch = ref.watch(companyBranchProvider);
    final branchesAsync = ref.watch(headerBranchListProvider);

    return branchesAsync.when(
      loading: () => _SelectorChip(
        icon: Icons.storefront_outlined,
        label: companyBranch.branchName ?? 'Sucursal',
        isDark: isDark,
        isLoading: true,
      ),
      error: (_, _) => _SelectorChip(
        icon: Icons.storefront_outlined,
        label: companyBranch.branchName ?? 'Sucursal',
        isDark: isDark,
      ),
      data: (branches) {
        if (branches.isEmpty) {
          return _SelectorChip(
            icon: Icons.storefront_outlined,
            label: companyBranch.branchName ?? 'Sin sucursales',
            isDark: isDark,
          );
        }
        // Auto-select first branch if none selected yet
        if (companyBranch.branchId == null && branches.isNotEmpty) {
          final first = branches.first;
          final firstName = first['name'] as String? ?? 'Sucursal';
          final firstId = first['id'] as String? ?? '';
          WidgetsBinding.instance.addPostFrameCallback((_) {
            ref.read(companyBranchProvider.notifier).selectBranch(firstId, firstName);
          });
        }
        final displayName = companyBranch.branchName ??
            (branches.isNotEmpty ? (branches.first['name'] as String? ?? 'Sucursal') : 'Sucursal');
        return MenuAnchor(
          menuChildren: branches.map((b) {
            final name = b['name'] ?? 'Sin nombre';
            final id = b['id'] ?? '';
            final isSelected = companyBranch.branchId == id;
            return MenuItemButton(
              leadingIcon: isSelected
                  ? const Icon(Icons.check, size: 16, color: ZColors.brandPrimary)
                  : const Icon(Icons.storefront_outlined, size: 16),
              child: Text(name, style: TextStyle(
                fontWeight: isSelected ? FontWeight.w600 : FontWeight.normal,
              )),
              onPressed: () {
                ref.read(companyBranchProvider.notifier).selectBranch(id, name);
              },
            );
          }).toList(),
          builder: (context, controller, child) {
            return GestureDetector(
              onTap: controller.open,
              child: _SelectorChip(
                icon: Icons.storefront_outlined,
                label: displayName,
                isDark: isDark,
              ),
            );
          },
        );
      },
    );
  }
}

/// Tenant switcher — shows available companies/tenants the user can access
class _TenantSwitcher extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final tenantsAsync = ref.watch(tenantsListProvider);

    return tenantsAsync.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (tenants) {
        if (tenants.length <= 1) return const SizedBox.shrink();
        final current = tenants.firstWhere((t) => t.isCurrent, orElse: () => tenants.first);
        return MenuAnchor(
          menuChildren: tenants.where((t) => !t.isCurrent).map((t) {
            return MenuItemButton(
              leadingIcon: const Icon(Icons.swap_horiz, size: 16),
              child: Text(t.companyName),
              onPressed: () async {
                final success = await ref.read(authProvider.notifier).switchTenant(t.tenantId);
                if (success) {
                  ref.invalidate(tenantsListProvider);
                }
              },
            );
          }).toList(),
          builder: (context, controller, child) {
            return GestureDetector(
              onTap: controller.open,
              child: _SelectorChip(
                icon: Icons.apartment_outlined,
                label: current.companyName,
                isDark: isDark,
              ),
            );
          },
        );
      },
    );
  }
}

/// Reusable selector chip used by both company and branch selectors
class _SelectorChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final bool isDark;
  final bool isLoading;

  const _SelectorChip({
    required this.icon,
    required this.label,
    required this.isDark,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: ZSpacing.sm, vertical: 6),
      decoration: BoxDecoration(
        color: isDark
            ? ZColors.neutral700.withValues(alpha: 0.3)
            : ZColors.neutral100,
        borderRadius: BorderRadius.circular(ZRadii.md),
        border: Border.all(
          color: isDark ? ZColors.darkBorder : ZColors.border,
        ),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (isLoading)
            const SizedBox(
              width: 14,
              height: 14,
              child: CircularProgressIndicator(strokeWidth: 1.5),
            )
          else
            Icon(
              icon,
              size: 14,
              color: isDark ? ZColors.neutral400 : ZColors.neutral500,
            ),
          const SizedBox(width: ZSpacing.xs),
          Text(
            label,
            style: ZTypography.labelMedium.copyWith(
              color: isDark ? ZColors.neutral300 : ZColors.neutral600,
              fontSize: 12,
            ),
          ),
          const SizedBox(width: ZSpacing.xs),
          Icon(
            Icons.keyboard_arrow_down,
            size: 14,
            color: isDark ? ZColors.neutral400 : ZColors.neutral500,
          ),
        ],
      ),
    );
  }
}

/// Centered search bar triggering command palette
class _SearchBar extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Material(
      color: isDark
          ? ZColors.neutral800.withValues(alpha: 0.5)
          : ZColors.neutral100,
      borderRadius: BorderRadius.circular(ZRadii.lg),
      child: InkWell(
        borderRadius: BorderRadius.circular(ZRadii.lg),
        onTap: () => ZCommandPalette.show(context),
        child: Padding(
          padding: const EdgeInsets.symmetric(
            horizontal: ZSpacing.md,
            vertical: 8,
          ),
          child: Row(
            children: [
              Icon(
                Icons.search,
                size: 16,
                color: isDark ? ZColors.neutral400 : ZColors.neutral500,
              ),
              const SizedBox(width: ZSpacing.sm),
              Expanded(
                child: Text(
                  'Buscar módulos, clientes, reportes...',
                  style: ZTypography.bodySmall.copyWith(
                    color: isDark
                        ? ZColors.neutral400
                        : ZColors.neutral500,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 4, vertical: 1),
                decoration: BoxDecoration(
                  border: Border.all(
                    color:
                        isDark ? ZColors.neutral600 : ZColors.neutral300,
                  ),
                  borderRadius: BorderRadius.circular(ZRadii.xs),
                ),
                child: Text(
                  '⌘K',
                  style: ZTypography.labelSmall.copyWith(
                    color: isDark
                        ? ZColors.neutral400
                        : ZColors.neutral500,
                    fontSize: 10,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Quick action buttons: notifications, chat, theme, user
class _QuickActions extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final themeMode = ref.watch(themeModeProvider);
    final auth = ref.watch(authProvider);
    final notifState = ref.watch(signalRProvider);
    final unreadCount = notifState.notifications.length;
    final connState = notifState.connectionState;

    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        // ── Currency Indicator ──
        _CurrencyIndicator(),
        const SizedBox(width: ZSpacing.sm),

        // ── Connectivity Indicator ──
        _ConnectivityDot(state: connState),
        const SizedBox(width: ZSpacing.sm),

        // ── Notifications ──
        _HeaderIconButton(
          icon: Icons.notifications_outlined,
          tooltip: '$unreadCount notificaciones',
          badgeCount: unreadCount,
          isDark: isDark,
          onPressed: () => _showNotificationPanel(context, ref),
        ),

        // ── Chat ──
        _HeaderIconButton(
          icon: Icons.chat_bubble_outline,
          tooltip: 'Centro de Comunicación',
          badgeCount: 0,
          isDark: isDark,
          onPressed: () => context.go('/chat'),
        ),

        SizedBox(width: ZSpacing.xs),

        // ── Theme Toggle ──
        IconButton(
          icon: Icon(
            themeMode == ThemeMode.dark
                ? Icons.light_mode_outlined
                : Icons.dark_mode_outlined,
            size: 18,
          ),
          tooltip:
              themeMode == ThemeMode.dark ? 'Modo claro' : 'Modo oscuro',
          onPressed: () =>
              ref.read(themeModeProvider.notifier).toggle(),
          constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
          padding: EdgeInsets.zero,
          splashRadius: 16,
        ),

        SizedBox(width: ZSpacing.xs),

        // ── User Avatar + Dropdown ──
        _UserAvatar(auth: auth, isDark: isDark),
      ],
    );
  }
}

/// Small icon button with optional badge
class _HeaderIconButton extends StatelessWidget {
  final IconData icon;
  final String tooltip;
  final int badgeCount;
  final bool isDark;
  final VoidCallback? onPressed;

  const _HeaderIconButton({
    required this.icon,
    required this.tooltip,
    this.badgeCount = 0,
    required this.isDark,
    this.onPressed,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: [
        IconButton(
          icon: Icon(
            icon,
            size: 18,
            color: isDark ? ZColors.neutral300 : ZColors.neutral600,
          ),
          tooltip: tooltip,
          onPressed: onPressed,
          constraints: const BoxConstraints(minWidth: 36, minHeight: 36),
          padding: EdgeInsets.zero,
          splashRadius: 16,
        ),
        if (badgeCount > 0)
          Positioned(
            right: 4,
            top: 4,
            child: Container(
              padding: const EdgeInsets.all(3),
              decoration: const BoxDecoration(
                color: ZColors.danger,
                shape: BoxShape.circle,
              ),
              child: Text(
                '$badgeCount',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 9,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ),
          ),
      ],
    );
  }
}

/// User avatar with dropdown menu
class _UserAvatar extends ConsumerWidget {
  final dynamic auth;
  final bool isDark;

  const _UserAvatar({required this.auth, required this.isDark});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final initial = (auth.displayName ?? auth.email ?? 'U')[0].toUpperCase();

    return MenuAnchor(
      menuChildren: [
        MenuItemButton(
          leadingIcon: const Icon(Icons.person_outline, size: 18),
          child: const Text('Mi Perfil'),
          onPressed: () => context.push('/profile'),
        ),
        MenuItemButton(
          leadingIcon: const Icon(Icons.settings_outlined, size: 18),
          child: const Text('Configuración'),
          onPressed: () => context.push('/settings'),
        ),
        const Divider(height: 1),
        MenuItemButton(
          leadingIcon: Icon(
            Icons.logout,
            size: 18,
            color: ZColors.danger,
          ),
          child: Text(
            'Cerrar sesión',
            style: TextStyle(color: ZColors.danger),
          ),
          onPressed: () => ref.read(authProvider.notifier).logout(),
        ),
      ],
      builder: (context, controller, child) {
        return GestureDetector(
          onTap: controller.open,
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(ZRadii.md),
            ),
            child: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                CircleAvatar(
                  radius: 14,
                  backgroundColor: isDark
                      ? ZColors.neutral600
                      : ZColors.neutral200,
                  child: Text(
                    initial,
                    style: ZTypography.labelSmall.copyWith(
                      color: isDark
                          ? ZColors.neutral100
                          : ZColors.neutral700,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
                SizedBox(width: ZSpacing.xs),
                Text(
                  auth.displayName ?? auth.email ?? 'Usuario',
                  style: ZTypography.labelMedium.copyWith(
                    color:
                        isDark ? ZColors.neutral300 : ZColors.neutral600,
                    fontSize: 12,
                  ),
                  overflow: TextOverflow.ellipsis,
                ),
                SizedBox(width: ZSpacing.xs),
                Icon(
                  Icons.keyboard_arrow_down,
                  size: 14,
                  color:
                      isDark ? ZColors.neutral400 : ZColors.neutral500,
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}

/// Shows a notification panel overlay from the header
void _showNotificationPanel(BuildContext context, WidgetRef ref) {
  final notifState = ref.read(signalRProvider);
  final notifications = notifState.notifications;

  showModalBottomSheet(
    context: context,
    isScrollControlled: true,
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(top: Radius.circular(16)),
    ),
    builder: (ctx) => DraggableScrollableSheet(
      initialChildSize: 0.5,
      minChildSize: 0.3,
      maxChildSize: 0.8,
      expand: false,
      builder: (ctx, controller) => Column(
        children: [
          // Handle bar
          Container(
            margin: const EdgeInsets.only(top: 8),
            width: 40,
            height: 4,
            decoration: BoxDecoration(
              color: ZColors.neutral300,
              borderRadius: BorderRadius.circular(2),
            ),
          ),
          // Header
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                const Icon(Icons.notifications_outlined, size: 20),
                const SizedBox(width: 8),
                Text(
                  'Notificaciones',
                  style: ZTypography.titleMedium.copyWith(fontWeight: FontWeight.w600),
                ),
                const Spacer(),
                if (notifications.isNotEmpty)
                  TextButton(
                    onPressed: () {
                      ref.read(signalRProvider.notifier).clearNotifications();
                      Navigator.pop(ctx);
                    },
                    child: Text(
                      'Limpiar todo',
                      style: TextStyle(color: ZColors.danger, fontSize: 12),
                    ),
                  ),
              ],
            ),
          ),
          const Divider(height: 1),
          // Notification list
          Expanded(
            child: notifications.isEmpty
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(
                          Icons.notifications_none_outlined,
                          size: 48,
                          color: ZColors.neutral300,
                        ),
                        const SizedBox(height: 12),
                        Text(
                          'Sin notificaciones',
                          style: ZTypography.bodyMedium.copyWith(
                            color: ZColors.neutral500,
                          ),
                        ),
                      ],
                    ),
                  )
                : ListView.separated(
                    controller: controller,
                    itemCount: notifications.length,
                    separatorBuilder: (_, _) => const Divider(height: 1),
                    itemBuilder: (_, i) {
                      final n = notifications[i];
                      return ListTile(
                        dense: true,
                        leading: Icon(
                          n.type == 'approval'
                              ? Icons.approval
                              : Icons.notifications_outlined,
                          size: 20,
                          color: n.type == 'approval'
                              ? ZColors.warning
                              : ZColors.info,
                        ),
                        title: Text(
                          n.title,
                          style: ZTypography.bodyMedium.copyWith(
                            fontWeight: FontWeight.w600,
                            fontSize: 13,
                          ),
                        ),
                        subtitle: Text(
                          n.message,
                          style: ZTypography.bodySmall.copyWith(
                            color: ZColors.neutral500,
                          ),
                        ),
                      );
                    },
                  ),
          ),
        ],
      ),
    ),
  );
}

/// Small dot indicating SignalR / API connectivity status
class _ConnectivityDot extends StatelessWidget {
  final ZConnectionState state;

  const _ConnectivityDot({required this.state});

  @override
  Widget build(BuildContext context) {
    final (color, tooltip) = switch (state) {
      ZConnectionState.connected => (ZColors.success, 'Sistema conectado y en línea'),
      ZConnectionState.connecting => (ZColors.warning, 'Conectando al sistema...'),
      ZConnectionState.reconnecting => (ZColors.warning, 'Reconectando al sistema...'),
      ZConnectionState.disconnected => (ZColors.danger, 'Sistema desconectado. Notificaciones en pausa.'),
    };

    return Tooltip(
      message: tooltip,
      child: Container(
        width: 8,
        height: 8,
        decoration: BoxDecoration(
          color: color,
          shape: BoxShape.circle,
          boxShadow: [
            BoxShadow(
              color: color.withValues(alpha: 0.4),
              blurRadius: 4,
              spreadRadius: 1,
            ),
          ],
        ),
      ),
    );
  }
}

/// Currency indicator — shows the active company currency as a compact chip
/// in the global header so users always know which currency is in use.
/// Tapping opens an exchange rate popup vs USD.
class _CurrencyIndicator extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final currencyCode = ref.watch(companyCurrencyProvider);
    return Tooltip(
      message: 'Ver tipo de cambio',
      child: GestureDetector(
        onTap: () => _showExchangeRateDialog(context, ref, currencyCode),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            gradient: LinearGradient(
              colors: isDark
                  ? [ZColors.brandPrimary.withValues(alpha: 0.3), ZColors.brandAccent.withValues(alpha: 0.2)]
                  : [ZColors.brandPrimary.withValues(alpha: 0.08), ZColors.brandAccent.withValues(alpha: 0.05)],
            ),
            borderRadius: BorderRadius.circular(ZRadii.md),
            border: Border.all(
              color: isDark
                  ? ZColors.brandAccent.withValues(alpha: 0.3)
                  : ZColors.brandPrimary.withValues(alpha: 0.2),
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.attach_money,
                size: 14,
                color: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
              ),
              const SizedBox(width: 4),
              Text(
                currencyCode,
                style: ZTypography.labelSmall.copyWith(
                  color: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
                  fontWeight: FontWeight.w700,
                  fontSize: 12,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Shows a popup with the latest exchange rate for [currencyCode] vs USD.
Future<void> _showExchangeRateDialog(
    BuildContext context, WidgetRef ref, String currencyCode) async {
  showDialog(
    context: context,
    builder: (ctx) => _ExchangeRatePopup(currencyCode: currencyCode),
  );
}

/// Internal popup widget that fetches the exchange rate on init.
class _ExchangeRatePopup extends ConsumerStatefulWidget {
  final String currencyCode;
  const _ExchangeRatePopup({required this.currencyCode});

  @override
  ConsumerState<_ExchangeRatePopup> createState() => _ExchangeRatePopupState();
}

class _ExchangeRatePopupState extends ConsumerState<_ExchangeRatePopup> {
  double? _rate;
  DateTime? _effectiveDate;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _fetchRate();
  }

  Future<void> _fetchRate() async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get(
        'exchange-rates/rate',
        params: {
          'from': widget.currencyCode,
          'to': 'USD',
        },
      );
      final data = response.data as Map<String, dynamic>;
      setState(() {
        _rate = (data['rate'] as num?)?.toDouble();
        _effectiveDate = data['date'] != null
            ? DateTime.tryParse(data['date'] as String)
            : null;
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _loading = false;
        _error = 'No se pudo obtener el tipo de cambio';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final theme = Theme.of(context);
    final code = widget.currencyCode;

    return Dialog(
      backgroundColor: isDark ? ZColors.darkSurface : ZColors.surface,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(ZRadii.lg)),
      child: Padding(
        padding: const EdgeInsets.all(ZSpacing.lg),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Header ──
            Row(
              children: [
                Container(
                  padding: const EdgeInsets.all(8),
                  decoration: BoxDecoration(
                    gradient: LinearGradient(
                      colors: isDark
                          ? [ZColors.brandPrimary.withValues(alpha: 0.3), ZColors.brandAccent.withValues(alpha: 0.2)]
                          : [ZColors.brandPrimary.withValues(alpha: 0.1), ZColors.brandAccent.withValues(alpha: 0.08)],
                    ),
                    borderRadius: BorderRadius.circular(ZRadii.md),
                  ),
                  child: Icon(
                    Icons.currency_exchange,
                    color: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
                    size: 20,
                  ),
                ),
                const SizedBox(width: ZSpacing.sm),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Tipo de Cambio',
                        style: ZTypography.titleSmall.copyWith(
                          fontWeight: FontWeight.w600,
                          color: isDark ? ZColors.neutral100 : ZColors.neutral900,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        'Moneda activa: $code',
                        style: ZTypography.bodySmall.copyWith(
                          color: isDark ? ZColors.neutral400 : ZColors.neutral500,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close, size: 18),
                  onPressed: () => Navigator.of(context).pop(),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(minWidth: 32, minHeight: 32),
                  splashRadius: 16,
                ),
              ],
            ),

            const SizedBox(height: ZSpacing.lg),

            // ── Rate body ──
            if (_loading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 32),
                child: Center(child: CircularProgressIndicator(strokeWidth: 2)),
              )
            else if (_error != null)
              _buildError(isDark)
            else if (code == 'USD')
              _buildUSDPanel(isDark)
            else
              _buildRatePanel(code, isDark, theme),

            // ── Footer link ──
            const SizedBox(height: ZSpacing.md),
            SizedBox(
              width: double.infinity,
              child: TextButton.icon(
                onPressed: () {
                  Navigator.of(context).pop();
                  context.go('/exchange-rates');
                },
                icon: const Icon(Icons.open_in_new, size: 14),
                label: Text(
                  'Gestionar tipos de cambio',
                  style: ZTypography.labelMedium.copyWith(fontSize: 12),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildError(bool isDark) {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.md),
      decoration: BoxDecoration(
        color: (isDark ? ZColors.danger : ZColors.danger).withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      child: Row(
        children: [
          Icon(Icons.warning_amber_rounded, size: 16, color: ZColors.danger),
          const SizedBox(width: ZSpacing.sm),
          Expanded(
            child: Text(
              _error!,
              style: ZTypography.bodySmall.copyWith(
                color: ZColors.danger,
                fontSize: 12,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildUSDPanel(bool isDark) {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.md),
      decoration: BoxDecoration(
        color: (isDark ? ZColors.info : ZColors.info).withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(ZRadii.md),
      ),
      child: Row(
        children: [
          Icon(Icons.info_outline, size: 16, color: ZColors.info),
          const SizedBox(width: ZSpacing.sm),
          Expanded(
            child: Text(
              'La moneda activa es USD. No se requiere conversión.',
              style: ZTypography.bodySmall.copyWith(
                color: isDark ? ZColors.neutral300 : ZColors.neutral700,
                fontSize: 12,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildRatePanel(String code, bool isDark, ThemeData theme) {
    final invertedRate = _rate != null && _rate! > 0 ? (1 / _rate!) : null;

    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        // Rate card
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(ZSpacing.md),
          decoration: BoxDecoration(
            color: isDark ? ZColors.neutral800 : ZColors.neutral50,
            borderRadius: BorderRadius.circular(ZRadii.md),
            border: Border.all(
              color: isDark ? ZColors.darkBorder : ZColors.border,
            ),
          ),
          child: Column(
            children: [
              // Main rate display
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    '1 $code',
                    style: ZTypography.titleMedium.copyWith(
                      fontWeight: FontWeight.w600,
                      color: isDark ? ZColors.neutral200 : ZColors.neutral800,
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 8),
                    child: Icon(
                      Icons.arrow_forward,
                      size: 16,
                      color: isDark ? ZColors.neutral500 : ZColors.neutral400,
                    ),
                  ),
                  Text(
                    _rate != null
                        ? 'USD ${_rate!.toStringAsFixed(4)}'
                        : 'USD (sin datos)',
                    style: ZTypography.titleMedium.copyWith(
                      fontWeight: FontWeight.w700,
                      color: isDark ? ZColors.brandAccent : ZColors.brandPrimary,
                    ),
                  ),
                ],
              ),

              if (invertedRate != null) ...[
                const SizedBox(height: ZSpacing.sm),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.swap_horiz, size: 12, color: isDark ? ZColors.neutral500 : ZColors.neutral400),
                    const SizedBox(width: 4),
                    Text(
                      '1 USD = $code ${invertedRate.toStringAsFixed(4)}',
                      style: ZTypography.bodySmall.copyWith(
                        color: isDark ? ZColors.neutral500 : ZColors.neutral400,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ],

              // Effective date
              if (_effectiveDate != null) ...[
                const SizedBox(height: ZSpacing.sm),
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.calendar_today, size: 11, color: isDark ? ZColors.neutral500 : ZColors.neutral400),
                    const SizedBox(width: 4),
                    Text(
                      'Actualizado: ${_effectiveDate!.day}/${_effectiveDate!.month}/${_effectiveDate!.year}',
                      style: ZTypography.bodySmall.copyWith(
                        color: isDark ? ZColors.neutral500 : ZColors.neutral400,
                        fontSize: 11,
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}
