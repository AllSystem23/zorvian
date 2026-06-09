final class ReportField {
  final String name;
  final String source;
  final String dataType;
  final bool visible;
  final int order;
  final String? aggregate;

  const ReportField({
    required this.name,
    required this.source,
    required this.dataType,
    required this.visible,
    required this.order,
    this.aggregate,
  });

  factory ReportField.fromJson(Map<String, dynamic> j) => ReportField(
        name: j['name'] as String,
        source: j['source'] as String,
        dataType: j['dataType'] as String,
        visible: j['visible'] as bool,
        order: (j['order'] as num).toInt(),
        aggregate: j['aggregate'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'name': name,
        'source': source,
        'dataType': dataType,
        'visible': visible,
        'order': order,
        if (aggregate != null) 'aggregate': aggregate,
      };
}

final class ReportFilter {
  final String field;
  final String operator;
  final String value;
  final String? value2;

  const ReportFilter({
    required this.field,
    required this.operator,
    required this.value,
    this.value2,
  });

  factory ReportFilter.fromJson(Map<String, dynamic> j) => ReportFilter(
        field: j['field'] as String,
        operator: j['operator'] as String,
        value: j['value'] as String,
        value2: j['value2'] as String?,
      );

  Map<String, dynamic> toJson() => {
        'field': field,
        'operator': operator,
        'value': value,
        if (value2 != null) 'value2': value2,
      };
}
