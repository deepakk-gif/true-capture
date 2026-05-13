import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/color_helper.dart';
import '../../../log/app_logs.dart';
import 'base_view_model.dart';

abstract class BaseConsumerState<T extends ConsumerStatefulWidget,
    V extends BaseViewModel> extends ConsumerState<T> {
  late final V viewModel;

  String screenName();
  V createViewModel();
  void onModelReady(V model) {}
  bool isBottomSheet() => false;

  @override
  void initState() {
    super.initState();
    viewModel = createViewModel();
    appLog('${screenName()} initState', tag: 'SCREEN');
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) onModelReady(viewModel);
    });
  }

  @override
  void dispose() {
    appLog('${screenName()} dispose', tag: 'SCREEN');
    super.dispose();
  }

  void showMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..clearSnackBars()
      ..showSnackBar(SnackBar(
        content: Text(message),
        behavior: SnackBarBehavior.floating,
      ));
  }

  void showErrorMessage(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..clearSnackBars()
      ..showSnackBar(SnackBar(
        content: Text(message),
        backgroundColor: ColorHelper.error,
        behavior: SnackBarBehavior.floating,
      ));
  }
}
