import 'package:flutter_riverpod/flutter_riverpod.dart';

final refreshProvider = StateProvider<int>((ref) => 0);

void triggerRefresh(WidgetRef ref) {
  ref.read(refreshProvider.notifier).state++;
}
