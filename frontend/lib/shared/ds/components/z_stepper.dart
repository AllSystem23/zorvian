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
    final isCompleted = index < currentStep;
    
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        AnimatedContainer(
          duration: const Duration(milliseconds: 300),
          width: isCurrent ? 36 : 32,
          height: isCurrent ? 36 : 32,
          decoration: BoxDecoration(
            color: isCompleted ? ZColors.success : (isActive ? ZColors.brandAccent : ZColors.neutral100),
            shape: BoxShape.circle,
            boxShadow: isCurrent ? [BoxShadow(color: ZColors.brandAccent.withAlpha(50), blurRadius: 8, spreadRadius: 2)] : null,
            border: isCurrent ? Border.all(color: Colors.white, width: 2) : null,
          ),
          child: Center(
            child: isCompleted
                ? const Icon(Icons.check, size: 18, color: Colors.white)
                : Text(
                    '${index + 1}',
                    style: TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      color: isActive ? ZColors.brandPrimary : ZColors.neutral400,
                    ),
                  ),
          ),
        ),
        const SizedBox(height: 8),
        Text(
          label,
          style: TextStyle(
            fontSize: 11,
            color: isCurrent ? ZColors.brandPrimary : ZColors.neutral400,
            fontWeight: isCurrent ? FontWeight.bold : FontWeight.w500,
          ),
        ),
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
