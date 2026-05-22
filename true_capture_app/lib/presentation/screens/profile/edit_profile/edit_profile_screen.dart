import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/utils/image_picker_utils.dart';
import '../../../../extension/keyboard_hide_extension.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_button.dart';
import '../../../common_widgets/custom_input_field.dart';
import '../../../common_widgets/user_avatar.dart';
import '../../../providers/user_data_provider.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state.dart';
import '../../base/screen_state_aware.dart';
import 'edit_profile_view_model.dart';

class EditProfileScreen extends ConsumerStatefulWidget {
  const EditProfileScreen({super.key});

  @override
  ConsumerState<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState
    extends BaseConsumerState<EditProfileScreen, EditProfileViewModel> {
  final _nameController = TextEditingController();
  final _bioController = TextEditingController();
  final _formKey = GlobalKey<FormState>();

  // Accepted gender wire values; anything else normalizes to null ("not specified").
  static const _genderValues = {'male', 'female', 'other'};

  String? _gender;          // null = not specified
  bool _isPrivate = false;  // false = public account

  @override
  void onModelReady(EditProfileViewModel model) {
    // Fetch the latest profile, then seed the form fields from it.
    model.load().then((_) {
      if (!mounted) return;
      final user = ref.read(authStateNotifierProvider);
      _nameController.text = user?.name ?? '';
      _bioController.text = user?.bio ?? '';
      setState(() {
        final g = user?.gender?.toLowerCase();
        _gender = _genderValues.contains(g) ? g : null;
        _isPrivate = user?.accountType?.toLowerCase() == 'private';
      });
    });
  }

  @override
  void dispose() {
    _nameController.dispose();
    _bioController.dispose();
    super.dispose();
  }

  void _submit() {
    if (!(_formKey.currentState?.validate() ?? false)) return;
    context.hideKeyboard();
    viewModel.save(
      context,
      displayName: _nameController.text,
      bio: _bioController.text,
      gender: _gender,
      accountType: _isPrivate ? 'private' : 'public',
    );
  }

  Future<void> _pickAvatar({required bool fromCamera}) async {
    final file = fromCamera
        ? await ImagePickerUtils.pickFromCamera()
        : await ImagePickerUtils.pickFromGallery();
    if (file != null) {
      await viewModel.uploadAvatar(file);
    }
  }

  void _showAvatarOptions({required bool hasAvatar}) {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choose from gallery'),
              onTap: () {
                Navigator.pop(sheetContext);
                _pickAvatar(fromCamera: false);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Take a photo'),
              onTap: () {
                Navigator.pop(sheetContext);
                _pickAvatar(fromCamera: true);
              },
            ),
            if (hasAvatar)
              ListTile(
                leading: const Icon(Icons.delete_outline),
                title: const Text('Remove photo'),
                onTap: () {
                  Navigator.pop(sheetContext);
                  viewModel.removeAvatar();
                },
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'Edit profile'),
      body: KeyboardDismisser(
        child: SafeArea(
          child: ScreenStateAware(
            state: viewModel.screenState,
            builder: (context) {
              final user = ref.watch(authStateNotifierProvider);
              final hasAvatar =
                  (user?.avatarUrl != null && user!.avatarUrl!.isNotEmpty);
              return SingleChildScrollView(
                padding: const EdgeInsets.all(24),
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxWidth: 480),
                  child: Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        Center(
                          child: Stack(
                            children: [
                              UserAvatar(
                                avatarUrl: user?.avatarUrl,
                                name: user?.name,
                                radius: 52,
                              ),
                              Positioned(
                                right: 0,
                                bottom: 0,
                                child: Material(
                                  color: Theme.of(context).colorScheme.primary,
                                  shape: const CircleBorder(),
                                  child: InkWell(
                                    customBorder: const CircleBorder(),
                                    onTap: () => _showAvatarOptions(
                                        hasAvatar: hasAvatar),
                                    child: Padding(
                                      padding: const EdgeInsets.all(8),
                                      child: Icon(
                                        Icons.camera_alt,
                                        size: 18,
                                        color: Theme.of(context)
                                            .colorScheme
                                            .onPrimary,
                                      ),
                                    ),
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                        const SizedBox(height: 8),
                        Center(
                          child: Text(
                            user?.email ?? '',
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                        ),
                        const SizedBox(height: 28),
                        CustomInputField(
                          controller: _nameController,
                          label: 'Display name',
                          hint: 'Your name',
                          textInputAction: TextInputAction.next,
                          validator: (v) {
                            if ((v ?? '').trim().length > 80) {
                              return 'At most 80 characters';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 16),
                        CustomInputField(
                          controller: _bioController,
                          label: 'Bio',
                          hint: 'A short bio',
                          maxLines: 4,
                          validator: (v) {
                            if ((v ?? '').trim().length > 500) {
                              return 'At most 500 characters';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 16),
                        Text('Gender',
                            style: Theme.of(context).textTheme.bodyMedium),
                        const SizedBox(height: 6),
                        InputDecorator(
                          decoration: const InputDecoration(),
                          child: DropdownButtonHideUnderline(
                            child: DropdownButton<String?>(
                              isExpanded: true,
                              value: _gender,
                              items: const [
                                DropdownMenuItem(
                                    value: null,
                                    child: Text('Not specified')),
                                DropdownMenuItem(
                                    value: 'male', child: Text('Male')),
                                DropdownMenuItem(
                                    value: 'female', child: Text('Female')),
                                DropdownMenuItem(
                                    value: 'other', child: Text('Other')),
                              ],
                              onChanged: (v) => setState(() => _gender = v),
                            ),
                          ),
                        ),
                        const SizedBox(height: 8),
                        SwitchListTile(
                          contentPadding: EdgeInsets.zero,
                          title: const Text('Private account'),
                          subtitle: const Text(
                              'Only approved followers can see your posts'),
                          value: _isPrivate,
                          onChanged: (v) => setState(() => _isPrivate = v),
                        ),
                        const SizedBox(height: 28),
                        ListenableBuilder(
                          listenable: viewModel.screenState,
                          builder: (context, _) => CustomButton(
                            label: 'Save changes',
                            isLoading: viewModel.screenState.value ==
                                ScreenState.apiProgress,
                            onPressed: _submit,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              );
            },
          ),
        ),
      ),
    );
  }

  @override
  EditProfileViewModel createViewModel() =>
      ref.read(editProfileViewModelProvider);

  @override
  String screenName() => 'EDIT PROFILE';
}
