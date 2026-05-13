import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../common_widgets/custom_app_bar.dart';

class TabHome extends ConsumerWidget {
  const TabHome({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Home'),
      body: const Center(
        child: Text('Home tab'),
      ),
    );
  }
}
