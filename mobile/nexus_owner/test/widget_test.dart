import 'package:flutter_test/flutter_test.dart';
import 'package:nexus_owner/main.dart';

void main() {
  testWidgets('owner app boots', (tester) async {
    await tester.pumpWidget(const NexusOwnerApp());
    await tester.pump();
    expect(find.byType(NexusOwnerApp), findsOneWidget);
  });
}
