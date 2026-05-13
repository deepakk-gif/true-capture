import '../core/constants/api_endpoints.dart';
import '../services/api_service.dart';

class CommonRepository {
  CommonRepository(this._apiService);

  final ApiService _apiService;

  Future<void> registerFcmToken(String token) async {
    await _apiService.post(
      ApiEndpoints.registerFcmToken,
      data: {'token': token},
    );
  }
}
