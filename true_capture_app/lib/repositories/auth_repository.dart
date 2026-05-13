import '../core/constants/api_endpoints.dart';
import '../network/dto/request/auth/otp_request.dart';
import '../network/dto/request/auth/sign_in_request.dart';
import '../network/dto/request/auth/sign_up_request.dart';
import '../network/dto/request/auth/social_login_request.dart';
import '../network/dto/response/auth/auth_response.dart';
import '../network/dto/response/auth/user_response.dart';
import '../services/api_service.dart';

class AuthRepository {
  AuthRepository(this._apiService);

  final ApiService _apiService;

  Future<AuthResponse> signIn(SignInRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.signIn,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<AuthResponse> signUp(SignUpRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.signUp,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<AuthResponse> socialLogin(SocialLoginRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.socialLogin,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<void> sendOtp(OtpSendRequest request) async {
    await _apiService.post(ApiEndpoints.sendOtp, data: request.toJson());
  }

  Future<AuthResponse> verifyOtp(OtpVerifyRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.verifyOtp,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<void> forgotPassword(String email) async {
    await _apiService
        .post(ApiEndpoints.forgotPassword, data: {'email': email});
  }

  Future<void> signOut() async {
    await _apiService.post(ApiEndpoints.signOut);
  }

  Future<UserResponse> getProfile() async {
    final response =
        await _apiService.get<Map<String, dynamic>>(ApiEndpoints.userProfile);
    return UserResponse.fromJson(response.data!);
  }
}
