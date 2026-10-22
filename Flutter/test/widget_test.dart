import 'package:flutter_test/flutter_test.dart';
import 'package:smart_incident_alert/main.dart';

void main() {
  testWidgets('Smart Incident Alert app starts', (WidgetTester tester) async {
    await tester.pumpWidget(const SmartIncidentApp());

    expect(find.byType(SmartIncidentApp), findsOneWidget);
    expect(find.text('Smart AI Incident Alert'), findsOneWidget);
  });
}
