import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../extension/keyboard_hide_extension.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_button.dart';
import '../../../common_widgets/custom_input_field.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state.dart';
import '../../base/screen_state_aware.dart';
import 'reset_password_view_model.dart';

class ResetPasswordScreen extends ConsumerStatefulWidget {
  const ResetPasswordScreen({
    super.key,
    required this.email,
    required this.code,
  });

  final String email;
  final String code;

  @override
  ConsumerState<ResetPasswordScreen> createState() =>
      _ResetPasswordScreenState();
}

class _ResetPasswordScreenState
    extends BaseConsumerState<ResetPasswordScreen, ResetPasswordViewModel> {
  final _passwordController = TextEditingController();
  final _confirmController  = TextEditingController();
  final _formKey = GlobalKey<FormState>();
  bool _obscure = true;

  @override
  void dispose() {
    _passwordController.dispose();
    _confirmController.dispose();
    super.dispose();
  }

  void _submit() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    context.hideKeyboard();
    viewModel.submit(
      context,
      email:       widget.email,
      code:        widget.code,
      newPassword: _passwordController.text,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Reset password'),
      body: KeyboardDismisser(
        child: SafeArea(
          child: ScreenStateAware(
            state: viewModel.screenState,
            builder: (context) => SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 480),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        'Choose a new password',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: 8),
                      Text('For ${widget.email}'),
                      const SizedBox(height: 24),
                      CustomInputField(
                        controller: _passwordController,
                        label: 'New password',
                        obscureText: _obscure,
                        textInputAction: TextInputAction.next,
                        suffixIcon: IconButton(
                          onPressed: () =>
                              setState(() => _obscure = !_obscure),
                          icon: Icon(_obscure
                              ? Icons.visibility_off_outlined
                              : Icons.visibility_outlined),
                        ),
                        validator: (v) {
                          if (v == null || v.isEmpty) return 'Required';
                          if (v.length < 8) return 'At least 8 characters';
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),
                      CustomInputField(
                        controller: _confirmController,
                        label: 'Confirm password',
                        obscureText: _obscure,
                        textInputAction: TextInputAction.done,
                        validator: (v) {
                          if (v == null || v.isEmpty) return 'Required';
                          if (v != _passwordController.text) {
                            return 'Passwords do not match';
                          }
                          return null;
                        },
                        onSubmitted: (_) => _submit(),
                      ),
                      const SizedBox(height: 24),
                      ListenableBuilder(
                        listenable: viewModel.screenState,
                        builder: (context, _) => CustomButton(
                          label: 'Reset password',
                          isLoading: viewModel.screenState.value ==
                              ScreenState.apiProgress,
                          onPressed: _submit,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  ResetPasswordViewModel createViewModel() =>
      ref.read(resetPasswordViewModelProvider);

  @override
  String screenName() => 'RESET PASSWORD';
}
