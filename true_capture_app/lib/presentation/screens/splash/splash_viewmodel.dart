import 'package:flutter/widgets.dart';

import '../../../core/router/app_router.dart';
import '../../../mixin/auth_mixin.dart';
import '../../../repositories/auth_repository.dart';
import '../../../services/local_service.dart';
import '../../providers/local_storage_provider.dart';
import '../../providers/user_data_provider.dart';
import '../base/base_view_model.dart';

class SplashViewmodel extends BaseViewModel with AuthMixin {
  SplashViewmodel(
    this._authRepository,
    this._localStorageService,
    this._authStateNotifier,
  );

  final AuthRepository _authRepository;
  final LocalStorageService _localStorageService;
  final AuthStateNotifier _authStateNotifier;

  Future<void> setupBeforeStart(BuildContext context) async {
    await Future<void>.delayed(const Duration(milliseconds: 800));
    await executeWithLoading(
      operation: () async {
        final token =
            await _localStorageService.read(StorageKeys.accessTokenKey);
        if (token == null || token.isEmpty) {
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeIntro);
          return;
        }

        try {
          final user = await _authRepository.getProfile();
          await _authStateNotifier.setUser(user);
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeMain);
        } catch (_) {
          if (!context.mounted) return;
          AppRouter.go(context, ScreenPath.routeSignIn);
        }
      },
      errorCallBack: (error, stack, message) {
        if (!context.mounted) return;
        AppRouter.go(context, ScreenPath.routeIntro);
      },
    );
  }
}
