import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:zorvian/features/admin/pages/invitation_form_page.dart';
import 'package:zorvian/shared/ds/ds.dart';

void main() {
  testWidgets('InvitationFormPage renders correctly', (WidgetTester tester) async {
    await tester.pumpWidget(
      const ProviderScope(
        child: MaterialApp(
          home: InvitationFormPage(),
        ),
      ),
    );

    expect(find.byType(ZTextField), findsOneWidget);
    expect(find.byType(ZButton), findsOneWidget);
  });
}
