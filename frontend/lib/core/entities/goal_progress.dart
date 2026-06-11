class GoalProgress {
  final String id;
  final String goalAssignmentId;
  final double currentValue;
  final double compliancePercentage;
  final DateTime evaluationDate;
  final String periodKey;
  final String? notes;
  final String? sourceData;

  GoalProgress({
    required this.id,
    required this.goalAssignmentId,
    required this.currentValue,
    required this.compliancePercentage,
    required this.evaluationDate,
    required this.periodKey,
    this.notes,
    this.sourceData,
  });

  factory GoalProgress.fromJson(Map<String, dynamic> json) {
    return GoalProgress(
      id: json['id'],
      goalAssignmentId: json['goalAssignmentId'],
      currentValue: (json['currentValue'] as num).toDouble(),
      compliancePercentage: (json['compliancePercentage'] as num).toDouble(),
      evaluationDate: DateTime.parse(json['evaluationDate']),
      periodKey: json['periodKey'],
      notes: json['notes'],
      sourceData: json['sourceData'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'goalAssignmentId': goalAssignmentId,
      'currentValue': currentValue,
      'compliancePercentage': compliancePercentage,
      'evaluationDate': evaluationDate.toIso8601String().split('T')[0],
      'periodKey': periodKey,
      'notes': notes,
      'sourceData': sourceData,
    };
  }
}
