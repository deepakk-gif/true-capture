import 'package:flutter/material.dart';

import 'screen_state.dart';

class ScreenStateAware extends StatelessWidget {
  const ScreenStateAware({
    super.key,
    required this.state,
    required this.builder,
    this.progress,
    this.apiProgress,
    this.error,
    this.empty,
    this.noInternet,
    this.onRefresh,
  });

  final ValueNotifier<ScreenState> state;
  final WidgetBuilder builder;
  final Widget? progress;
  final Widget? apiProgress;
  final WidgetBuilder? error;
  final Widget? empty;
  final Widget? noInternet;
  final Future<void> Function()? onRefresh;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<ScreenState>(
      valueListenable: state,
      builder: (context, value, _) {
        switch (value) {
          case ScreenState.progress:
            return progress ??
                const Center(child: CircularProgressIndicator());
          case ScreenState.error:
            return error?.call(context) ??
                const Center(child: Text('Something went wrong'));
          case ScreenState.empty:
            return empty ?? const Center(child: Text('No data'));
          case ScreenState.noInternet:
            return noInternet ??
                const Center(child: Text('No internet connection'));
          case ScreenState.apiProgress:
            return Stack(
              children: [
                builder(context),
                Positioned.fill(
                  child: ColoredBox(
                    color: Colors.black.withValues(alpha: 0.05),
                    child: apiProgress ??
                        const Center(child: CircularProgressIndicator()),
                  ),
                ),
              ],
            );
          case ScreenState.refresh:
          case ScreenState.content:
          case ScreenState.action:
          case ScreenState.none:
            return builder(context);
        }
      },
    );
  }
}
