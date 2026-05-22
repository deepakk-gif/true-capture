import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/router/app_router.dart';
import '../../../../extension/keyboard_hide_extension.dart';
import '../../../../network/dto/post_models.dart';
import '../../../../repositories/media_repository.dart';
import '../../../common_widgets/custom_app_bar.dart';
import '../../../common_widgets/custom_button.dart';
import '../../../common_widgets/custom_input_field.dart';
import '../../../providers/user_data_provider.dart';
import '../../../providers/vm_provider.dart';
import '../../base/base_consumer_state.dart';
import '../../base/screen_state.dart';
import '../../base/screen_state_aware.dart';
import 'create_post_view_model.dart';

class CreatePostScreen extends ConsumerStatefulWidget {
  const CreatePostScreen({super.key});

  @override
  ConsumerState<CreatePostScreen> createState() => _CreatePostScreenState();
}

class _CreatePostScreenState
    extends BaseConsumerState<CreatePostScreen, CreatePostViewModel> {
  final _captionController = TextEditingController();
  final _picker = ImagePicker();
  final List<TextEditingController> _referenceControllers = [
    TextEditingController(),
  ];

  @override
  void dispose() {
    _captionController.dispose();
    for (final c in _referenceControllers) {
      c.dispose();
    }
    super.dispose();
  }

  Future<void> _pickImages() async {
    final files = await _picker.pickMultiImage();
    for (final f in files) {
      viewModel.addMedia(File(f.path));
    }
  }

  Future<void> _pickVideo() async {
    final file = await _picker.pickVideo(source: ImageSource.gallery);
    if (file != null) viewModel.addMedia(File(file.path));
  }

  void _addReferenceField() {
    setState(() => _referenceControllers.add(TextEditingController()));
  }

  Future<void> _submit() async {
    context.hideKeyboard();
    final post = await viewModel.submit(
      caption: _captionController.text,
      references: _referenceControllers.map((c) => c.text).toList(),
    );
    if (post != null && mounted) {
      _captionController.clear();
      for (final c in _referenceControllers) {
        c.clear();
      }
      showMessage('Post shared.');
      AppRouter.push(context, ScreenPath.routePostDetail, extra: {'postId': post.id});
    }
  }

  @override
  Widget build(BuildContext context) {
    // Granting Fake-vs-Real access also sets the blue tick — use it as the gate.
    final canPostFvr = ref.watch(authStateNotifierProvider)?.isBlueTick ?? false;

    return Scaffold(
      appBar: const CustomAppBar(title: 'New post'),
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
                    if (canPostFvr) _typeSelector(),
                    if (canPostFvr) const SizedBox(height: 16),
                    _mediaPicker(),
                    const SizedBox(height: 16),
                    CustomInputField(
                      controller: _captionController,
                      label: viewModel.isFakeVsReal ? 'Caption (required)' : 'Caption',
                      hint: 'Write a caption…',
                      maxLines: 4,
                    ),
                    if (viewModel.isFakeVsReal) ...[
                      const SizedBox(height: 16),
                      _referenceLinks(),
                    ],
                    const SizedBox(height: 24),
                    CustomButton(
                      label: viewModel.isFakeVsReal
                          ? 'Share Fake vs Real post'
                          : 'Share',
                      isLoading: viewModel.screenState.value ==
                          ScreenState.apiProgress,
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

  Widget _typeSelector() {
    return SegmentedButton<String>(
      segments: const [
        ButtonSegment(value: PostType.normal, label: Text('Normal')),
        ButtonSegment(value: PostType.fakeVsReal, label: Text('Fake vs Real')),
      ],
      selected: {viewModel.postType},
      onSelectionChanged: (s) => viewModel.setType(s.first),
    );
  }

  Widget _mediaPicker() {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (viewModel.media.isNotEmpty)
          SizedBox(
            height: 96,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: viewModel.media.length,
              separatorBuilder: (_, _) => const SizedBox(width: 8),
              itemBuilder: (_, i) => _mediaThumb(i),
            ),
          ),
        const SizedBox(height: 8),
        Row(
          children: [
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _pickImages,
                icon: const Icon(Icons.photo_library_outlined),
                label: const Text('Add photos'),
              ),
            ),
            const SizedBox(width: 8),
            Expanded(
              child: OutlinedButton.icon(
                onPressed: _pickVideo,
                icon: const Icon(Icons.videocam_outlined),
                label: const Text('Add video'),
              ),
            ),
          ],
        ),
        if (viewModel.media.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Text(
              'Photos and videos only — GIFs and audio are not supported.',
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.hintColor),
            ),
          ),
      ],
    );
  }

  Widget _mediaThumb(int index) {
    final file = viewModel.media[index];
    final isVideo = MediaRepository.isVideoPath(file.path);
    return Stack(
      children: [
        ClipRRect(
          borderRadius: BorderRadius.circular(8),
          child: SizedBox(
            width: 96,
            height: 96,
            child: isVideo
                ? Container(
                    color: Theme.of(context).colorScheme.surfaceContainerHighest,
                    child: const Icon(Icons.videocam, size: 32),
                  )
                : Image.file(file, fit: BoxFit.cover),
          ),
        ),
        Positioned(
          top: 0,
          right: 0,
          child: GestureDetector(
            onTap: () => viewModel.removeMedia(index),
            child: const CircleAvatar(
              radius: 12,
              backgroundColor: Colors.black54,
              child: Icon(Icons.close, size: 14, color: Colors.white),
            ),
          ),
        ),
      ],
    );
  }

  Widget _referenceLinks() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text('Reference links (at least one required)',
            style: Theme.of(context).textTheme.titleSmall),
        const SizedBox(height: 8),
        for (var i = 0; i < _referenceControllers.length; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: 8),
            child: CustomInputField(
              controller: _referenceControllers[i],
              label: 'Link ${i + 1}',
              hint: 'https://…',
            ),
          ),
        Align(
          alignment: Alignment.centerLeft,
          child: TextButton.icon(
            onPressed: _addReferenceField,
            icon: const Icon(Icons.add),
            label: const Text('Add another link'),
          ),
        ),
      ],
    );
  }

  @override
  CreatePostViewModel createViewModel() =>
      ref.read(createPostViewModelProvider);

  @override
  String screenName() => 'CREATE POST';
}
