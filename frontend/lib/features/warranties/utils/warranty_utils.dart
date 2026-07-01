import 'package:flutter/material.dart';
import 'package:zorvian/shared/ds/ds.dart';

Color warrantyStatusColor(String status) => switch (status) {
  'Registered' => ZColors.brandPrimary,
  'PendingReview' => ZColors.warning,
  'InDiagnosis' => ZColors.brandSecondary,
  'SentToWorkshop' => ZColors.brandSecondary,
  'InRepair' => ZColors.brandAccent,
  'PendingParts' => ZColors.warning,
  'Repaired' => ZColors.success,
  'ReplacementApproved' => ZColors.success,
  'ReadyForDelivery' => ZColors.brandTeal,
  'Delivered' => ZColors.success,
  'Closed' => ZColors.neutral900,
  'Rejected' => ZColors.danger,
  'Cancelled' => ZColors.danger,
  _ => ZColors.neutral500,
};

String warrantyStatusLabel(String status) => switch (status) {
  'Registered' => 'Registrada',
  'PendingReview' => 'Revisión',
  'InDiagnosis' => 'Diagnóstico',
  'SentToWorkshop' => 'En taller',
  'InRepair' => 'Reparando',
  'PendingParts' => 'Repuestos',
  'Repaired' => 'Reparada',
  'ReplacementApproved' => 'Reemplazo',
  'ReadyForDelivery' => 'Lista para entregar',
  'Delivered' => 'Entregada',
  'Closed' => 'Cerrada',
  'Rejected' => 'Rechazada',
  'Cancelled' => 'Cancelada',
  _ => status,
};

ZBadgeType warrantyBadgeType(String status) => switch (status) {
  'Registered' => ZBadgeType.neutral,
  'PendingReview' => ZBadgeType.warning,
  'InDiagnosis' => ZBadgeType.info,
  'SentToWorkshop' => ZBadgeType.info,
  'InRepair' => ZBadgeType.accent,
  'PendingParts' => ZBadgeType.warning,
  'Repaired' => ZBadgeType.success,
  'ReplacementApproved' => ZBadgeType.success,
  'ReadyForDelivery' => ZBadgeType.success,
  'Delivered' => ZBadgeType.success,
  'Closed' => ZBadgeType.neutral,
  'Rejected' => ZBadgeType.danger,
  'Cancelled' => ZBadgeType.danger,
  _ => ZBadgeType.neutral,
};

const List<String> warrantyWorkflowSteps = [
  'Registrada',
  'Revisión',
  'Diagnóstico',
  'En taller',
  'Reparando',
  'Reparada',
  'Entregar',
  'Cerrada',
];

int warrantyWorkflowStep(String status) => switch (status) {
  'Registered' => 0,
  'PendingReview' => 1,
  'InDiagnosis' => 2,
  'SentToWorkshop' => 3,
  'InRepair' => 4,
  'PendingParts' => 4,
  'Repaired' => 5,
  'ReplacementApproved' => 5,
  'ReadyForDelivery' => 6,
  'Delivered' => 6,
  'Closed' => 7,
  _ => 0,
};
