import 'package:flutter/material.dart';
import 'package:zorvian/shared/ds/ds.dart';

class ZAlertCard extends StatelessWidget {
  final String message;
  final String severity; // 'high' | 'medium' | 'low'

  const ZAlertCard({
    super.key,
    required this.message,
    this.severity = 'low',
  });

  @override
  Widget build(BuildContext context) {
    final color = severity == 'high' 
        ? Colors.red 
        : (severity == 'medium' ? Colors.orange : Colors.blue);
        
    return ZCard(
      padding: const EdgeInsets.all(12),
      child: Row(
        children: [
          Icon(Icons.warning_amber_rounded, color: color),
          const SizedBox(width: 12),
          Expanded(child: Text(message)),
        ],
      ),
    );
  }
}
