import 'package:flutter/material.dart';
import '../ds.dart';

/// Skeleton loading placeholder component for better perceived performance
class ZSkeleton extends StatefulWidget {
  final double width;
  final double height;
  final double borderRadius;
  final Color? baseColor;
  final Color? highlightColor;

  const ZSkeleton({
    super.key,
    this.width = double.infinity,
    this.height = 16,
    this.borderRadius = 4,
    this.baseColor,
    this.highlightColor,
  });

  /// Pre-built skeleton for a card
  static Widget card({double height = 200}) {
    return Container(
      height: height,
      decoration: BoxDecoration(
        color: ZColors.neutral100,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        border: Border.all(color: ZColors.border),
      ),
    );
  }

  /// Pre-built skeleton for a list tile
  static Widget listTile() {
    return const Padding(
      padding: EdgeInsets.symmetric(vertical: ZSpacing.sm),
      child: Row(
        children: [
          ZSkeleton(width: 40, height: 40, borderRadius: 20),
          SizedBox(width: ZSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                ZSkeleton(width: double.infinity, height: 14),
                SizedBox(height: ZSpacing.xs),
                ZSkeleton(width: 120, height: 10),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// Pre-built skeleton for a data table row
  static Widget tableRow({int columns = 5}) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: ZSpacing.sm),
      child: Row(
        children: [
          for (int i = 0; i < columns; i++) ...[
            if (i > 0) const SizedBox(width: ZSpacing.md),
            Expanded(
              child: ZSkeleton(
                height: 12,
                width: double.infinity,
              ),
            ),
          ],
        ],
      ),
    );
  }

  /// Pre-built skeleton for a dashboard stat card
  static Widget statCard() {
    return Container(
      padding: const EdgeInsets.all(ZSpacing.lg),
      decoration: BoxDecoration(
        color: ZColors.neutral100,
        borderRadius: BorderRadius.circular(ZRadii.lg),
        border: Border.all(color: ZColors.border),
      ),
      child: const Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          ZSkeleton(width: 80, height: 10),
          SizedBox(height: ZSpacing.md),
          ZSkeleton(width: 120, height: 24),
          SizedBox(height: ZSpacing.sm),
          ZSkeleton(width: 60, height: 10),
        ],
      ),
    );
  }

  /// Pre-built skeleton for a page header
  static Widget header() {
    return const Padding(
      padding: EdgeInsets.only(bottom: ZSpacing.lg),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                ZSkeleton(width: 200, height: 24),
                SizedBox(height: ZSpacing.sm),
                ZSkeleton(width: 300, height: 12),
              ],
            ),
          ),
        ],
      ),
    );
  }

  @override
  State<ZSkeleton> createState() => _ZSkeletonState();
}

class _ZSkeletonState extends State<ZSkeleton> with SingleTickerProviderStateMixin {
  late AnimationController _controller;
  late Animation<double> _animation;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 1500),
    )..repeat();
    _animation = Tween<double>(begin: 0.0, end: 1.0).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeInOut),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final baseColor = widget.baseColor ?? ZColors.neutral100;
    final highlightColor = widget.highlightColor ?? ZColors.neutral200;

    return AnimatedBuilder(
      animation: _animation,
      builder: (context, child) {
        final t = _animation.value;
        final color = Color.lerp(baseColor, highlightColor, t < 0.5 ? t * 2 : (1 - t) * 2);

        return Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(widget.borderRadius),
          ),
        );
      },
    );
  }
}