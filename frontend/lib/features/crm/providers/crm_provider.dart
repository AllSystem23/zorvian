import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../auth/auth_provider.dart';

/// CRM Contact — Represents a client or lead with relationship data
class CrmContact {
  final String id;
  final String name;
  final String? email;
  final String? phone;
  final String? company;
  final String status; // lead, prospect, client, inactive
  final String? notes;
  final String createdAt;
  final String? lastContactAt;

  const CrmContact({
    required this.id,
    required this.name,
    this.email,
    this.phone,
    this.company,
    this.status = 'lead',
    this.notes,
    required this.createdAt,
    this.lastContactAt,
  });

  factory CrmContact.fromJson(Map<String, dynamic> json) {
    return CrmContact(
      id: json['id'] ?? '',
      name: json['name'] ?? json['fullName'] ?? '',
      email: json['email'],
      phone: json['phone'],
      company: json['company'],
      status: json['status'] ?? 'lead',
      notes: json['notes'],
      createdAt: json['createdAt'] ?? '',
      lastContactAt: json['lastContactAt'],
    );
  }
}

/// CRM Activity — Interaction log for a contact
class CrmActivity {
  final String id;
  final String contactId;
  final String type; // call, email, meeting, note
  final String subject;
  final String? description;
  final String createdAt;

  const CrmActivity({
    required this.id,
    required this.contactId,
    required this.type,
    required this.subject,
    this.description,
    required this.createdAt,
  });

  factory CrmActivity.fromJson(Map<String, dynamic> json) {
    return CrmActivity(
      id: json['id'] ?? '',
      contactId: json['contactId'] ?? '',
      type: json['type'] ?? 'note',
      subject: json['subject'] ?? '',
      description: json['description'],
      createdAt: json['createdAt'] ?? '',
    );
  }
}

/// CRM State
class CrmState {
  final List<CrmContact> contacts;
  final List<CrmActivity> activities;
  final bool loading;
  final String? error;
  final String filterStatus; // all, lead, prospect, client

  const CrmState({
    this.contacts = const [],
    this.activities = const [],
    this.loading = false,
    this.error,
    this.filterStatus = 'all',
  });

  List<CrmContact> get filteredContacts {
    if (filterStatus == 'all') return contacts;
    return contacts.where((c) => c.status == filterStatus).toList();
  }

  int get totalLeads => contacts.where((c) => c.status == 'lead').length;
  int get totalProspects => contacts.where((c) => c.status == 'prospect').length;
  int get totalClients => contacts.where((c) => c.status == 'client').length;

  CrmState copyWith({
    List<CrmContact>? contacts,
    List<CrmActivity>? activities,
    bool? loading,
    String? error,
    String? filterStatus,
  }) {
    return CrmState(
      contacts: contacts ?? this.contacts,
      activities: activities ?? this.activities,
      loading: loading ?? this.loading,
      error: error,
      filterStatus: filterStatus ?? this.filterStatus,
    );
  }
}

class CrmNotifier extends Notifier<CrmState> {
  @override
  CrmState build() => const CrmState();

  Future<void> loadContacts() async {
    state = state.copyWith(loading: true, error: null);
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('clients');
      final data = (response.data as List)
          .map((e) => CrmContact.fromJson(e as Map<String, dynamic>))
          .toList();
      state = state.copyWith(contacts: data, loading: false);
    } catch (e) {
      state = state.copyWith(error: 'Error cargando contactos: $e', loading: false);
    }
  }

  Future<void> loadActivities(String contactId) async {
    try {
      final dio = ref.read(dioClientProvider);
      final response = await dio.get('clients/$contactId/activities');
      final data = (response.data as List)
          .map((e) => CrmActivity.fromJson(e as Map<String, dynamic>))
          .toList();
      state = state.copyWith(activities: data);
    } catch (e) {
      state = state.copyWith(activities: []);
    }
  }

  void setFilter(String status) {
    state = state.copyWith(filterStatus: status);
  }

  Future<bool> addContact(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('clients', data: data);
      await loadContacts();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error creando contacto: $e');
      return false;
    }
  }

  Future<bool> updateContact(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.put('clients/${data['id']}', data: data);
      await loadContacts();
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error actualizando contacto: $e');
      return false;
    }
  }

  Future<bool> addActivity(Map<String, dynamic> data) async {
    try {
      final dio = ref.read(dioClientProvider);
      await dio.post('clients/${data['contactId']}/activities', data: data);
      await loadActivities(data['contactId']);
      return true;
    } catch (e) {
      state = state.copyWith(error: 'Error registrando actividad: $e');
      return false;
    }
  }
}

final crmProvider = NotifierProvider<CrmNotifier, CrmState>(() {
  return CrmNotifier();
});