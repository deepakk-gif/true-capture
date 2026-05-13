import 'package:flutter/material.dart';

import '../core/constants/color_helper.dart';

class ToastHelper {
  ToastHelper._();

  static void show(BuildContext context, String message,
      {Color? backgroundColor, Duration? duration}) {
    final messenger = ScaffoldMessenger.maybeOf(context);
    if (messenger == null) return;
    messenger
      ..clearSnackBars()
      ..showSnackBar(
        SnackBar(
          content: Text(message),
          backgroundColor: backgroundColor,
          duration: duration ?? const Duration(seconds: 3),
          behavior: SnackBarBehavior.floating,
        ),
      );
  }

  static void showError(BuildContext context, String message) =>
      show(context, message, backgroundColor: ColorHelper.error);

  static void showSuccess(BuildContext context, String message) =>
      show(context, message, backgroundColor: ColorHelper.success);
}
