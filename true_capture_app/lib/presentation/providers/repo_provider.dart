import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../repositories/auth_repository.dart';
import '../../repositories/common_repository.dart';
import '../../services/api_service.dart';

final apiServiceProvider = Provider<ApiService>((ref) {
  return ApiService.instance;
});

final authRepo = Provider<AuthRepository>((ref) {
  return AuthRepository(ref.read(apiServiceProvider));
});

final commonRepo = Provider<CommonRepository>((ref) {
  return CommonRepository(ref.read(apiServiceProvider));
});
