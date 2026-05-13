import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../config/app_config.dart';
import '../../../core/constants/color_helper.dart';
import '../../providers/vm_provider.dart';
import '../base/base_consumer_state.dart';
import '../base/screen_state_aware.dart';
import 'splash_viewmodel.dart';

class SplashScreen extends ConsumerStatefulWidget {
  const SplashScreen({super.key});

  @override
  ConsumerState<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState
    extends BaseConsumerState<SplashScreen, SplashViewmodel> {
  @override
  void onModelReady(SplashViewmodel model) {
    model.setupBeforeStart(context);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: ColorHelper.primary,
      body: ScreenStateAware(
        state: viewModel.screenState,
        progress: const Center(
          child: CircularProgressIndicator(color: Colors.white),
        ),
        builder: (context) => const Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              FlutterLogo(size: 96),
              SizedBox(height: 16),
              Text(
                AppConfig.appName,
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 28,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  @override
  SplashViewmodel createViewModel() => ref.read(splashVm);

  @override
  String screenName() => 'SPLASH SCREEN';
}
