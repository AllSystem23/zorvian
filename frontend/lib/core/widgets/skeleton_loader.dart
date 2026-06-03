import 'package:flutter/material.dart';

class ShimmerLoader extends StatefulWidget {
  final double width;
  final double height;
  final double borderRadius;

  const ShimmerLoader({
    super.key,
    this.width = double.infinity,
    required this.height,
    this.borderRadius = 8,
  });

  @override
  State<ShimmerLoader> createState() => _ShimmerLoaderState();
}

class _ShimmerLoaderState extends State<ShimmerLoader> with SingleTickerProviderStateMixin {
  late final AnimationController _controller;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 1500))..repeat();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return AnimatedBuilder(
      animation: _controller,
      builder: (_, _) {
        final isLight = theme.brightness == Brightness.light;
        return Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(widget.borderRadius),
            gradient: LinearGradient(
              begin: Alignment.centerLeft,
              end: Alignment.centerRight,
              colors: [
                isLight ? Colors.grey.shade200 : Colors.grey.shade800,
                isLight ? Colors.grey.shade100 : Colors.grey.shade700,
                isLight ? Colors.grey.shade200 : Colors.grey.shade800,
              ],
              stops: [
                _controller.value - 0.3,
                _controller.value,
                _controller.value + 0.3,
              ],
            ),
          ),
        );
      },
    );
  }
}

class TableShimmer extends StatelessWidget {
  final int rows;
  final int columns;

  const TableShimmer({super.key, this.rows = 5, this.columns = 4});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: List.generate(
        rows,
        (i) => Padding(
          padding: const EdgeInsets.symmetric(vertical: 8),
          child: Row(
            children: List.generate(
              columns,
              (j) => Expanded(
                child: Padding(
                  padding: EdgeInsets.only(right: j < columns - 1 ? 12 : 0),
                  child: ShimmerLoader(height: 16, width: double.infinity, borderRadius: 4),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class CardShimmer extends StatelessWidget {
  final int count;

  const CardShimmer({super.key, this.count = 3});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: List.generate(
        count,
        (_) => Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                const ShimmerLoader(width: 40, height: 40, borderRadius: 20),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ShimmerLoader(height: 14, width: 150, borderRadius: 4),
                      const SizedBox(height: 8),
                      ShimmerLoader(height: 12, width: double.infinity, borderRadius: 4),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
