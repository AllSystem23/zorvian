class GoalDefinition {
  final String id;
  final String name;
  final String? description;
  final String goalType;
  final String metricType;
  final String frequency;
  final int evaluationPeriodDays;
  final String dataSource;
  final String? calculationFormula;
  final bool hasGateCondition;
  final String? gateDescription;
  final String? gateFormula;
  final String status;

  GoalDefinition({
    required this.id,
    required this.name,
    this.description,
    required this.goalType,
    this.metricType = 'amount',
    this.frequency = 'monthly',
    this.evaluationPeriodDays = 30,
    required this.dataSource,
    this.calculationFormula,
    this.hasGateCondition = false,
    this.gateDescription,
    this.gateFormula,
    this.status = 'active',
  });

  factory GoalDefinition.fromJson(Map<String, dynamic> json) {
    return GoalDefinition(
      id: json['id'],
      name: json['name'],
      description: json['description'],
      goalType: json['goalType'],
      metricType: json['metricType'] ?? 'amount',
      frequency: json['frequency'] ?? 'monthly',
      evaluationPeriodDays: json['evaluationPeriodDays'] ?? 30,
      dataSource: json['dataSource'],
      calculationFormula: json['calculationFormula'],
      hasGateCondition: json['hasGateCondition'] ?? false,
      gateDescription: json['gateDescription'],
      gateFormula: json['gateFormula'],
      status: json['status'] ?? 'active',
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'name': name,
      'description': description,
      'goalType': goalType,
      'metricType': metricType,
      'frequency': frequency,
      'evaluationPeriodDays': evaluationPeriodDays,
      'dataSource': dataSource,
      'calculationFormula': calculationFormula,
      'hasGateCondition': hasGateCondition,
      'gateDescription': gateDescription,
      'gateFormula': gateFormula,
      'status': status,
    };
  }
}
