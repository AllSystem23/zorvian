import 'package:flutter/material.dart';
import '../ds.dart';

enum ZStepperOrientation { horizontal, vertical }

class ZStepper extends StatelessWidget {
  final int currentStep;
  final List<String> steps;
  final ZStepperOrientation orientation;
  final double? height;

  const ZStepper({
    super.key,
    required this.currentStep,
    required this.steps,
    this.orientation = ZStepperOrientation.horizontal,
    this.height,
  });

  @override
  Widget build(BuildContext context) {
    return orientation == ZStepperOrientation.horizontal
        ? SizedBox(
            height: height ?? 72,
            child: Row(children: _buildSteps(context)),
          )
        : Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: _buildVerticalSteps(context),
          );
  }

  List<Widget> _buildSteps(BuildContext context) {
    final list = <Widget>[];
    for (var i = 0; i < steps.length; i++) {
      list.add(_StepCircle(index: i, currentStep: currentStep, label: steps[i]));
      if (i < steps.length - 1) {
        list.add(Expanded(
          child: Container(
            height: 2,
            color: i < currentStep ? ZColors.brandAccent : ZColors.neutral200,
          ),
        ));
      }
    }
    return list;
  }

  List<Widget> _buildVerticalSteps(BuildContext context) {
    final list = <Widget>[];
    for (var i = 0; i < steps.length; i++) {
      list.add(_VerticalStep(index: i, currentStep: currentStep, label: steps[i], isLast: i == steps.length - 1));
    }
    return list;
  }
}

final class _StepCircle extends StatelessWidget {
  final int index;
  final int currentStep;
  final String label;

  const _StepCircle({required this.index, required this.currentStep, required this.label});

  @override
  Widget build(BuildContext context) {
    final isActive = index <= currentStep;
    final isCurrent = index == currentStep;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: isCurrent ? 36 : 28,
          height: isCurrent ? 36 : 28,
          decoration: BoxDecoration(
            color: isActive ? ZColors.brandAccent : ZColors.neutral100,
            shape: BoxShape.circle,
            border: isCurrent ? Border.all(color: ZColors.brandAccent, width: 3) : null,
          ),
          child: Center(
            child: isCurrent
                ? const SizedBox(width: 14, height: 14, child: CircularProgressIndicator(strokeWidth: 2, color: ZColors.brandPrimary))
                : Text('${index + 1}', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600, color: isActive ? ZColors.brandPrimary : ZColors.neutral400)),
          ),
        ),
        const SizedBox(height: 4),
        Text(label, style: TextStyle(fontSize: 11, color: isCurrent ? ZColors.brandPrimary : ZColors.neutral400, fontWeight: isCurrent ? FontWeight.w600 : FontWeight.w400)),
      ],
    );
  }
}

final class _VerticalStep extends StatelessWidget {
  final int index;
  final int currentStep;
  final String label;
  final bool isLast;

  const _VerticalStep({required this.index, required this.currentStep, required this.label, required this.isLast});

  @override
  Widget build(BuildContext context) {
    final isActive = index <= currentStep;
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 32,
            child: Column(
              children: [
                Container(
                  width: 24, height: 24,
                  decoration: BoxDecoration(
                    color: isActive ? ZColors.brandAccent : ZColors.neutral100,
                    shape: BoxShape.circle,
                  ),
                  child: Center(child: Text('${index + 1}', style: TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: isActive ? ZColors.brandPrimary : ZColors.neutral400))),
                ),
                if (!isLast)
                  Expanded(
                    child: Container(width: 2, color: ZColors.neutral200),
                  ),
              ],
            ),
          ),
          const SizedBox(width: ZSpacing.sm),
          Padding(
            padding: const EdgeInsets.only(top: 2, bottom: ZSpacing.lg),
            child: Text(label, style: TextStyle(fontSize: 13, color: isActive ? ZColors.neutral900 : ZColors.neutral400, fontWeight: isActive ? FontWeight.w500 : FontWeight.w400)),
          ),
        ],
      ),
    );
  }
}
