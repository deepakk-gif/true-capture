import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/constants/color_helper.dart';
import '../../../log/app_logs.dart';
import 'base_view_model.dart';
import 'screen_state.dart';

abstract class BaseConsumerState<T extends ConsumerStatefulWidget,
    V extends BaseViewModel> extends ConsumerState<T> {
  late final V viewModel;

  String? _lastShownError;

  String screenName();
  V createViewModel();
  void onModelReady(V model) {}
  bool isBottomSheet() => false;

  @override
  void initState() {
    super.initState();
    viewModel = createViewModel();
    viewModel.addListener(_handleViewModelError);
    appLog('${screenName()} initState', tag: 'SCREEN');
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) onModelReady(viewModel);
    });
  }

  @override
  void dispose() {
    viewModel.removeListener(_handleViewModelError);
    appLog('${screenName()} dispose', tag: 'SCREEN');
    super.dispose();
  }

  /// Surfaces a new view-model error as a snackbar. Skipped when the screen has
  /// switched to a full-page [ScreenState.error] view, which renders the
  /// message itself, and de-duplicated so the same error isn't shown twice.
  void _handleViewModelError() {
    final error = viewModel.errorMessage;
    if (error == null) {
      _lastShownError = null;
      return;
    }
    if (error == _lastShownError) return;
    _lastShownError = error;
    if (viewModel.screenState.value == ScreenState.error) return;
    showErrorMessage(error);
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
