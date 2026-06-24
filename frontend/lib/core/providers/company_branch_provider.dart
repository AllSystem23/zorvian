import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
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
  @override
  CompanyBranchState build() => const CompanyBranchState();

  void selectCompany(String id, String name) {
    state = state.copyWith(companyId: id, companyName: name);
  }

  void selectBranch(String id, String name) {
    state = state.copyWith(branchId: id, branchName: name);
  }
}

final companyBranchProvider =
    NotifierProvider<CompanyBranchNotifier, CompanyBranchState>(() {
  return CompanyBranchNotifier();
});

/// Provider that lists all companies for the current tenant
final companyListProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('companies/current', options: _silentOptions).timeout(const Duration(seconds: 5));
    if (response.data is Map<String, dynamic>) {
      return [response.data as Map<String, dynamic>];
    }
    return [];
  } catch (e) {
    return [];
  }
});

/// Provider that lists all branches for the current company
final headerBranchListProvider =
    FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final dio = ref.read(dioClientProvider);
  try {
    final response = await dio.get('branches', options: _silentOptions).timeout(const Duration(seconds: 5));
    if (response.data is List) {
      return (response.data as List).cast<Map<String, dynamic>>();
    }
    return [];
  } catch (e) {
    return [];
  }
});