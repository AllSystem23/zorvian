import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../../auth/auth_provider.dart';

final _silentOptions = Options(extra: {'suppressGlobalError': true});

/// Stores the currently selected company and branch IDs.
/// These are used by the header selectors and can be consumed by any module.
class CompanyBranchState {
  final String? companyId;
  final String? companyName;
  final String? branchId;
  final String? branchName;

  const CompanyBranchState({
    this.companyId,
    this.companyName,
    this.branchId,
    this.branchName,
  });

  CompanyBranchState copyWith({
    String? companyId,
    String? companyName,
    String? branchId,
    String? branchName,
  }) {
    return CompanyBranchState(
      companyId: companyId ?? this.companyId,
      companyName: companyName ?? this.companyName,
      branchId: branchId ?? this.branchId,
      branchName: branchName ?? this.branchName,
    );
  }
}

class CompanyBranchNotifier extends Notifier<CompanyBranchState> {
  static const _kCompanyId = 'selected_company_id';
  static const _kCompanyName = 'selected_company_name';
  static const _kBranchId = 'selected_branch_id';
  static const _kBranchName = 'selected_branch_name';

  late final Future<void> _hydrated;

  @override
  CompanyBranchState build() {
    _hydrated = _restore();
    return const CompanyBranchState();
  }

  /// Completes once the persisted selection (if any) has been restored.
  /// Callers that auto-select a company must await this to avoid racing
  /// the restore and clobbering the user's saved selection.
  Future<void> get whenHydrated => _hydrated;

  Future<void> _restore() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final companyId = prefs.getString(_kCompanyId);
      if (companyId == null || companyId.isEmpty) return;
      // Never overwrite a selection the user already made while restoring.
      if (state.companyId != null) return;
      state = CompanyBranchState(
        companyId: companyId,
        companyName: prefs.getString(_kCompanyName),
        branchId: prefs.getString(_kBranchId),
        branchName: prefs.getString(_kBranchName),
      );
    } catch (_) {
      // Sin persistencia disponible: el header rehace la selección.
    }
  }

  Future<void> _persist() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final s = state;
      if (s.companyId == null || s.companyId!.isEmpty) {
        await prefs.remove(_kCompanyId);
        await prefs.remove(_kCompanyName);
        await prefs.remove(_kBranchId);
        await prefs.remove(_kBranchName);
        return;
      }
      await prefs.setString(_kCompanyId, s.companyId!);
      if (s.companyName != null) {
        await prefs.setString(_kCompanyName, s.companyName!);
      } else {
        await prefs.remove(_kCompanyName);
      }
      if (s.branchId != null) {
        await prefs.setString(_kBranchId, s.branchId!);
      } else {
        await prefs.remove(_kBranchId);
      }
      if (s.branchName != null) {
        await prefs.setString(_kBranchName, s.branchName!);
      } else {
        await prefs.remove(_kBranchName);
      }
    } catch (_) {
      // La persistencia es best-effort; el contexto en memoria sigue siendo válido.
    }
  }

  /// Selects a company. When the company actually changes, the selected
  /// branch is reset: the previous branch belongs to the previous company.
  void selectCompany(String id, String name) {
    final companyChanged = state.companyId != id;
    state = CompanyBranchState(
      companyId: id,
      companyName: name,
      branchId: companyChanged ? null : state.branchId,
      branchName: companyChanged ? null : state.branchName,
    );
    _persist();
  }

  void selectBranch(String id, String name) {
    state = state.copyWith(branchId: id, branchName: name);
    _persist();
  }

  /// Clears the branch selection (e.g. the branch no longer exists in the
  /// current company). The header re-auto-selects the first branch.
  void clearBranch() {
    if (state.branchId == null && state.branchName == null) return;
    state = CompanyBranchState(
      companyId: state.companyId,
      companyName: state.companyName,
    );
    _persist();
  }

  /// Clears the whole company/branch scope (logout or tenant switch).
  void clearSelection() {
    state = const CompanyBranchState();
    _persist();
  }
}

final companyBranchProvider =
    NotifierProvider<CompanyBranchNotifier, CompanyBranchState>(() {
  return CompanyBranchNotifier();
});

/// Provider that lists all companies visible to the current user.
/// SuperAdmin gets all companies; regular users get their tenant's company.
final companyListProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  final auth = ref.watch(authProvider);
  final isSuperAdmin = auth.role == 'SuperAdmin';
  try {
    if (isSuperAdmin) {
      final response = await dio.get('companies/all', options: _silentOptions).timeout(const Duration(seconds: 15));
      if (response.data is List) {
        return (response.data as List)
            .map((e) => Map<String, dynamic>.from(e as Map))
            .toList();
      }
      return [];
    } else {
      final response = await dio.get('companies/current', options: _silentOptions).timeout(const Duration(seconds: 15));
      if (response.data is Map<String, dynamic>) {
        final company = response.data as Map<String, dynamic>;
        // Filter out empty/default company responses (SuperAdmin without tenant)
        if (company['id'] == null || company['id'] == '00000000-0000-0000-0000-000000000000') {
          return [];
        }
        return [company];
      }
      return [];
    }
  } catch (e) {
    return [];
  }
});

/// Provider that lists all branches for the current company
final headerBranchListProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('branches', options: _silentOptions).timeout(const Duration(seconds: 15));
    if (response.data is List) {
      return (response.data as List).cast<Map<String, dynamic>>();
    }
    return [];
  } catch (e) {
    return [];
  }
});