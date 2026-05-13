import '../core/constants/api_endpoints.dart';
import '../network/dto/request/auth/forgot_password_request.dart';
import '../network/dto/request/auth/google_sign_in_request.dart';
import '../network/dto/request/auth/otp_request.dart';
import '../network/dto/request/auth/refresh_token_request.dart';
import '../network/dto/request/auth/reset_password_request.dart';
import '../network/dto/request/auth/sign_in_request.dart';
import '../network/dto/request/auth/sign_up_request.dart';
import '../network/dto/response/auth/auth_response.dart';
import '../network/dto/response/auth/user_response.dart';
import '../services/api_service.dart';

class AuthRepository {
  AuthRepository(this._apiService);

  final ApiService _apiService;

  // ---- Primary credential flows ----------------------------------------

  Future<AuthResponse> signIn(SignInRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.login,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<AuthResponse> signUp(SignUpRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.register,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  // ---- OTP / email verification ----------------------------------------

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

  // ---- Forgot / reset password -----------------------------------------

  Future<void> forgotPassword(ForgotPasswordRequest request) async {
    await _apiService.post(ApiEndpoints.forgotPassword, data: request.toJson());
  }

  Future<void> resetPassword(ResetPasswordRequest request) async {
    await _apiService.post(ApiEndpoints.resetPassword, data: request.toJson());
  }

  // ---- Google OAuth ----------------------------------------------------

  Future<AuthResponse> googleSignIn(GoogleSignInRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.google,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  // ---- Session lifecycle -----------------------------------------------

  /// Calls `/api/auth/refresh` with the stored refresh token; returns new tokens.
  /// Used by the refresh interceptor on 401 retries — kept separate from the
  /// interceptor so it can also be called explicitly (e.g., on app resume).
  Future<AuthResponse> refresh(RefreshTokenRequest request) async {
    final response = await _apiService.post<Map<String, dynamic>>(
      ApiEndpoints.refresh,
      data: request.toJson(),
    );
    return AuthResponse.fromJson(response.data!);
  }

  Future<void> signOut(RefreshTokenRequest request) async {
    await _apiService.post(ApiEndpoints.logout, data: request.toJson());
  }

  // ---- Profile ---------------------------------------------------------

  Future<UserResponse> getProfile() async {
    final response =
        await _apiService.get<Map<String, dynamic>>(ApiEndpoints.userProfile);
    return UserResponse.fromJson(response.data!);
  }
}
