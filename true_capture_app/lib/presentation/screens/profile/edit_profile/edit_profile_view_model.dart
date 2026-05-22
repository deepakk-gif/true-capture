import 'dart:io';

import 'package:flutter/widgets.dart';

import '../../../../core/router/app_router.dart';
import '../../../../network/dto/request/user/update_profile_request.dart';
import '../../../../repositories/user_repository.dart';
import '../../../providers/user_data_provider.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

class EditProfileViewModel extends BaseViewModel {
  EditProfileViewModel(this._userRepository, this._authStateNotifier);

  final UserRepository _userRepository;
  final AuthStateNotifier _authStateNotifier;

  /// Initial fetch — pulls the freshest profile from the server so the form
  /// reflects server state even on a cold start (auth state may be empty).
  Future<void> load() async {
    await executeWithLoading(
      operation: () async {
        final user = await _userRepository.getMyProfile();
        await _authStateNotifier.setUser(user);
      },
    );
  }

  /// Persists display name, bio, gender + account type, then pops back to the
  /// profile tab. [gender] is null when "Not specified" is chosen.
  Future<void> save(
    BuildContext context, {
    required String displayName,
    required String bio,
    required String? gender,
    required String accountType,
  }) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        final updated = await _userRepository.updateProfile(
          UpdateProfileRequest(
            displayName: displayName.trim().isEmpty ? null : displayName.trim(),
            bio: bio.trim().isEmpty ? null : bio.trim(),
            gender: gender,
            accountType: accountType,
          ),
        );
        await _authStateNotifier.setUser(updated);
        if (!context.mounted) return;
        AppRouter.pop(context);
      },
    );
  }

  /// Uploads [image] as the new avatar; the auth state user is refreshed so
  /// every avatar across the app updates.
  Future<void> uploadAvatar(File image) async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        final updated = await _userRepository.uploadAvatar(image);
        await _authStateNotifier.setUser(updated);
      },
    );
  }

  Future<void> removeAvatar() async {
    await executeWithLoading(
      errorState: ScreenState.content,
      operation: () async {
        final updated = await _userRepository.removeAvatar();
        await _authStateNotifier.setUser(updated);
      },
    );
  }
}
