import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/accounting/pages/ai_assistant_page.dart';
import 'package:zorvian/features/accounting/providers/assistant_provider.dart';

class FakeAssistantService extends AssistantService {
  FakeAssistantService() : super(null);

  @override
  Future<void> saveFeedback(Map<String, dynamic> feedbackData) async {
    // No-op for testing
  }
}

void main() {
  testWidgets('AiAssistantPage displays', (WidgetTester tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          assistantServiceProvider.overrideWithValue(FakeAssistantService()),
        ],
        child: const MaterialApp(
          home: AiAssistantPage(),
        ),
      ),
    );
    
    expect(find.byType(AiAssistantPage), findsOneWidget);
  });
}
