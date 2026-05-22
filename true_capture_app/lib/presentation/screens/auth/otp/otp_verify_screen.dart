import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../config/app_config.dart';
import '../../../../core/constants/api_endpoints.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_otp_field.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'otp_view_model.dart';

class OtpVerifyScreen extends ConsumerStatefulWidget {
  const OtpVerifyScreen({
    super.key,
    required this.email,
    required this.purpose,
  });

  final String     email;
  final OtpPurpose purpose;

  @override
  ConsumerState<OtpVerifyScreen> createState() => _OtpVerifyScreenState();
}

class _OtpVerifyScreenState
    extends BaseConsumerState<OtpVerifyScreen, OtpViewModel> {
  @override
  void onModelReady(OtpViewModel model) {
    // Localhost testing: the backend's Development env accepts a fixed OTP, so
    // submit it once automatically. `onModelReady` fires exactly once (post
    // first frame) from this stable screen state — unlike the old field-level
    // auto-submit, it cannot re-fire when the widget tree rebuilds.
    if (AppConfig.isLocal) {
      model.verify(
        context,
        email: widget.email,
        code: AppConfig.localTestOtp,
        purpose: widget.purpose,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final isReset = widget.purpose == OtpPurpose.passwordReset;
    return Scaffold(
      appBar: CustomAppBar(title: isReset ? 'Verify reset code' : 'Verify email'),
      body: SafeArea(
        child: ScreenStateAware(
          state: viewModel.screenState,
          builder: (context) => Padding(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Enter the 6-digit code',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  Text('Sent to ${widget.email}'),
                  const SizedBox(height: 32),
                  CustomOtpField(
                    initialValue:
                        AppConfig.isLocal ? AppConfig.localTestOtp : null,
                    onCompleted: (code) => viewModel.verify(
                      context,
                      email:   widget.email,
                      code:    code,
                      purpose: widget.purpose,
                    ),
                  ),
                  const SizedBox(height: 24),
                  Center(
                    child: TextButton(
                      onPressed: () => viewModel.resend(
                        email:   widget.email,
                        purpose: widget.purpose,
                      ),
                      child: const Text('Resend code'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  OtpViewModel createViewModel() => ref.read(otpViewModelProvider);

  @override
  String screenName() => 'OTP VERIFY';
}
