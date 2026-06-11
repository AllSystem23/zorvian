import 'goal_definition.dart';

class GoalAssignment {
  final String id;
  final String goalDefinitionId;
  final GoalDefinition? goalDefinition;
  final String? employeeId;
  final String? teamId;
  final double targetValue;
  final double? stretchValue;
  final double? baseLine;
  final double? weight;
  final double? minimumGate;
  final DateTime effectiveDate;
  final DateTime? expirationDate;
  final String status;

  GoalAssignment({
    required this.id,
    required this.goalDefinitionId,
    this.goalDefinition,
    this.employeeId,
    this.teamId,
    required this.targetValue,
    this.stretchValue,
    this.baseLine,
    this.weight,
    this.minimumGate,
    required this.effectiveDate,
    this.expirationDate,
    this.status = 'active',
  });

  factory GoalAssignment.fromJson(Map<String, dynamic> json) {
    return GoalAssignment(
      id: json['id'],
      goalDefinitionId: json['goalDefinitionId'],
      goalDefinition: json['goalDefinition'] != null 
          ? GoalDefinition.fromJson(json['goalDefinition']) 
          : null,
      employeeId: json['employeeId'],
      teamId: json['teamId'],
      targetValue: (json['targetValue'] as num).toDouble(),
      stretchValue: (json['stretchValue'] as num?)?.toDouble(),
      baseLine: (json['baseLine'] as num?)?.toDouble(),
      weight: (json['weight'] as num?)?.toDouble(),
      minimumGate: (json['minimumGate'] as num?)?.toDouble(),
      effectiveDate: DateTime.parse(json['effectiveDate']),
      expirationDate: json['expirationDate'] != null 
          ? DateTime.parse(json['expirationDate']) 
          : null,
      status: json['status'] ?? 'active',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'goalDefinitionId': goalDefinitionId,
      'employeeId': employeeId,
      'teamId': teamId,
      'targetValue': targetValue,
      'stretchValue': stretchValue,
      'baseLine': baseLine,
      'weight': weight,
      'minimumGate': minimumGate,
      'effectiveDate': effectiveDate.toIso8601String().split('T')[0],
      'expirationDate': expirationDate?.toIso8601String().split('T')[0],
      'status': status,
    };
  }
}
