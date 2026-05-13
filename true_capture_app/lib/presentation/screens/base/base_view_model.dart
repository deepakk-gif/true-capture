import 'package:flutter/foundation.dart';

import '../../../log/app_logs.dart';
import '../../../network/helper/error_handler.dart';
import 'screen_state.dart';

typedef ErrorCallback = void Function(
  Object error,
  StackTrace stackTrace,
  String errorMessage,
);

abstract class BaseViewModel extends ChangeNotifier {
  String? _error;
  String? get errorMessage => _error;
  bool get hasError => _error != null;

  final ValueNotifier<ScreenState> screenState =
      ValueNotifier(ScreenState.content);

  void changeScreenState(ScreenState value) {
    if (screenState.value == value) return;
    screenState.value = value;
  }

  void setError(String? message) {
    _error = message;
    notifyListeners();
  }

  void clearError() {
    if (_error == null) return;
    _error = null;
    notifyListeners();
  }

  Future<T?> executeWithLoading<T>({
    required Future<T> Function() operation,
    ErrorCallback? errorCallBack,
    ScreenState initialState = ScreenState.apiProgress,
    ScreenState successState = ScreenState.content,
  }) async {
    clearError();
    changeScreenState(initialState);
    try {
      final result = await operation();
      changeScreenState(successState);
      return result;
    } catch (error, stack) {
      final mapped = ErrorHandler.handle(error);
      setError(mapped.message);
      changeScreenState(ScreenState.error);
      appLogError(error, stack, 'VIEW_MODEL');
      errorCallBack?.call(error, stack, mapped.message);
      return null;
    }
  }

  @override
  void dispose() {
    screenState.dispose();
    super.dispose();
  }
}
