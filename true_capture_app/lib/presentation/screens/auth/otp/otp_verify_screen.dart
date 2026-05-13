import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_otp_field.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state_aware.dart';
import 'otp_view_model.dart';

class OtpVerifyScreen extends ConsumerStatefulWidget {
  const OtpVerifyScreen({super.key, required this.email});

  final String email;

  @override
  ConsumerState<OtpVerifyScreen> createState() => _OtpVerifyScreenState();
}

class _OtpVerifyScreenState
    extends BaseConsumerState<OtpVerifyScreen, OtpViewModel> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Verify OTP'),
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
                    onCompleted: (code) =>
                        viewModel.verify(context, email: widget.email, otp: code),
                  ),
                  const SizedBox(height: 24),
                  Center(
                    child: TextButton(
                      onPressed: () => viewModel.resend(widget.email),
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
