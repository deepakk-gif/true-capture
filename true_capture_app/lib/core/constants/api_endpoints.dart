class ApiEndpoints {
  ApiEndpoints._();

  // Auth — aligned with backend AuthController routes under `/api/auth`.
  static const String register       = '/api/auth/register';
  static const String login          = '/api/auth/login';
  static const String refresh        = '/api/auth/refresh';
  static const String logout         = '/api/auth/logout';
  static const String sendOtp        = '/api/auth/send-otp';
  static const String verifyOtp      = '/api/auth/verify-otp';
  static const String forgotPassword = '/api/auth/forgot-password';
  static const String resetPassword  = '/api/auth/reset-password';
  static const String google         = '/api/auth/google';

  // User
  static const String userProfile  = '/api/users/me';
  static const String updateProfile = '/api/users/me';
  static const String uploadAvatar = '/api/users/me/avatar';

  // Common
  static const String registerFcmToken = '/api/notifications/register-token';
}

/// OTP purpose discriminator. Must match the backend `OtpPurpose` enum
/// (1 = VerifyEmail, 2 = PasswordReset).
enum OtpPurpose {
  verifyEmail(1),
  passwordReset(2);

  const OtpPurpose(this.wireValue);
  final int wireValue;
}
