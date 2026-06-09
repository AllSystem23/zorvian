import 'package:flutter/material.dart';
import '../ds.dart';

/// Progress indicator with label and value
class ZProgress extends StatelessWidget {
  final String label;
  final double value; // 0.0 to 1.0
  final Color? color;
  final bool showValue;
  final String? valueLabel;

  const ZProgress({
    super.key,
    required this.label,
    required this.value,
    this.color,
    this.showValue = true,
    this.valueLabel,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final progressColor = color ?? _getColorForValue(value, theme);
    final percent = (value * 100).clamp(0, 100).toInt();
    final displayValue = valueLabel ?? '$percent%';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(label, style: theme.textTheme.bodyMedium?.copyWith(color: ZColors.neutral700)),
            if (showValue)
              Text(displayValue, style: theme.textTheme.bodyMedium?.copyWith(fontWeight: FontWeight.w600, color: progressColor)),
          ],
        ),
        const SizedBox(height: ZSpacing.xs),
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: LinearProgressIndicator(
            value: value.clamp(0.0, 1.0),
            minHeight: 8,
            backgroundColor: ZColors.neutral100,
            valueColor: AlwaysStoppedAnimation<Color>(progressColor),
          ),
        ),
      ],
    );
  }

  Color _getColorForValue(double value, ThemeData theme) {
    if (value < 0.5) return ZColors.success;
    if (value < 0.8) return ZColors.warning;
    return ZColors.danger;
  }
}

/// Circular progress with center label
class ZCircularProgress extends StatelessWidget {
  final double value;
  final String? centerLabel;
  final double size;
  final double strokeWidth;
  final Color? color;

  const ZCircularProgress({
    super.key,
    required this.value,
    this.centerLabel,
    this.size = 80,
    this.strokeWidth = 8,
    this.color,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final percent = (value * 100).clamp(0, 100).toInt();
    final displayText = centerLabel ?? '$percent%';
    return SizedBox(
      width: size,
      height: size,
      child: Stack(
        alignment: Alignment.center,
        children: [
          SizedBox(
            width: size,
            height: size,
            child: CircularProgressIndicator(
              value: value.clamp(0.0, 1.0),
              strokeWidth: strokeWidth,
              backgroundColor: ZColors.neutral100,
              valueColor: AlwaysStoppedAnimation<Color>(color ?? theme.colorScheme.primary),
            ),
          ),
          Text(displayText, style: theme.textTheme.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}

/// Step progress indicator
class ZStepProgress extends StatelessWidget {
  final List<String> steps;
  final int currentStep;

  const ZStepProgress({
    super.key,
    required this.steps,
    required this.currentStep,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: List.generate(steps.length, (index) {
        final isComplete = index < currentStep;
        final isCurrent = index == currentStep;
        final color = isComplete || isCurrent ? theme.colorScheme.primary : ZColors.neutral300;
        return Expanded(
          child: Column(
            children: [
              Row(
                children: [
                  if (index > 0)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: index <= currentStep ? theme.colorScheme.primary : ZColors.neutral200,
                      ),
                    ),
                  Container(
                    width: 28, height: 28,
                    decoration: BoxDecoration(
                      color: color,
                      shape: BoxShape.circle,
                    ),
                    child: Center(
                      child: isComplete
                          ? const Icon(Icons.check, size: 16, color: Colors.white)
                          : Text('${index + 1}', style: TextStyle(fontSize: 12, color: Colors.white, fontWeight: FontWeight.bold)),
                    ),
                  ),
                  if (index < steps.length - 1)
                    Expanded(
                      child: Container(
                        height: 2,
                        color: index < currentStep ? theme.colorScheme.primary : ZColors.neutral200,
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 4),
              Text(
                steps[index],
                style: theme.textTheme.bodySmall?.copyWith(
                  color: isCurrent ? theme.colorScheme.primary : ZColors.neutral500,
                  fontWeight: isCurrent ? FontWeight.w600 : FontWeight.normal,
                ),
                textAlign: TextAlign.center,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        );
      }),
    );
  }
}
