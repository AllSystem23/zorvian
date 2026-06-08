class UserModel {
  final String id;
  final String displayName;
  final String email;
  final bool isActive;
  final DateTime? lastLoginAt;
  final List<String> roles;
  final String? employeeName;

  UserModel({
    required this.id,
    required this.displayName,
    required this.email,
    required this.isActive,
    this.lastLoginAt,
    required this.roles,
    this.employeeName,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) {
    return UserModel(
      id: json['id'],
      displayName: json['displayName'],
      email: json['email'],
      isActive: json['isActive'],
      lastLoginAt: json['lastLoginAt'] != null ? DateTime.parse(json['lastLoginAt']) : null,
      roles: List<String>.from(json['roles']),
      employeeName: json['employeeName'],
    );
  }
}
