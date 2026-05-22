import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/router/app_router.dart';
import '../../../../core/utils/image_picker_utils.dart';
import '../../../../extension/keyboard_hide_extension.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_button.dart';
import '../../../common_widgets/custom_input_field.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state.dart';
import '../../base/screen_state_aware.dart';
import 'create_story_view_model.dart';

class CreateStoryScreen extends ConsumerStatefulWidget {
  const CreateStoryScreen({super.key});

  @override
  ConsumerState<CreateStoryScreen> createState() => _CreateStoryScreenState();
}

class _CreateStoryScreenState
    extends BaseConsumerState<CreateStoryScreen, CreateStoryViewModel> {
  final _captionController = TextEditingController();

  @override
  void dispose() {
    _captionController.dispose();
    super.dispose();
  }

  Future<void> _pick({required bool fromCamera}) async {
    final file = fromCamera
        ? await ImagePickerUtils.pickFromCamera()
        : await ImagePickerUtils.pickFromGallery();
    if (file != null) viewModel.setImage(file);
  }

  void _pickOptions() {
    showModalBottomSheet<void>(
      context: context,
      builder: (sheet) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: const Text('Choose from gallery'),
              onTap: () {
                Navigator.pop(sheet);
                _pick(fromCamera: false);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: const Text('Take a photo'),
              onTap: () {
                Navigator.pop(sheet);
                _pick(fromCamera: true);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    context.hideKeyboard();
    final ok = await viewModel.submit(_captionController.text);
    if (ok && mounted) AppRouter.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: const CustomAppBar(title: 'New story'),
      body: KeyboardDismisser(
        child: SafeArea(
          child: ScreenStateAware(
            state: viewModel.screenState,
            builder: (context) => ListenableBuilder(
              listenable: viewModel,
              builder: (context, _) => SingleChildScrollView(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    GestureDetector(
                      onTap: _pickOptions,
                      child: AspectRatio(
                        aspectRatio: 9 / 16,
                        child: Container(
                          decoration: BoxDecoration(
                            color: Theme.of(context)
                                .colorScheme
                                .surfaceContainerHighest,
                            borderRadius: BorderRadius.circular(12),
                            image: viewModel.image != null
                                ? DecorationImage(
                                    image: FileImage(viewModel.image!),
                                    fit: BoxFit.cover)
                                : null,
                          ),
                          child: viewModel.image == null
                              ? const Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(Icons.add_a_photo_outlined, size: 40),
                                    SizedBox(height: 8),
                                    Text('Tap to choose an image'),
                                  ],
                                )
                              : null,
                        ),
                      ),
                    ),
                    const SizedBox(height: 12),
                    CustomInputField(
                      controller: _captionController,
                      label: 'Caption',
                      hint: 'Optional — use @username to mention',
                      maxLines: 3,
                    ),
                    const SizedBox(height: 24),
                    CustomButton(
                      label: 'Share story',
                      isLoading:
                          viewModel.screenState.value == ScreenState.apiProgress,
                      onPressed: _submit,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }

  @override
  CreateStoryViewModel createViewModel() => ref.read(createStoryViewModelProvider);

  @override
  String screenName() => 'CREATE STORY';
}
