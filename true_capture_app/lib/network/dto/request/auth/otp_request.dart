import '../../../../core/constants/api_endpoints.dart' show OtpPurpose;

/// Body for `POST /api/auth/send-otp`. Matches backend `SendOtpRequest(Email, Purpose)`.
class OtpSendRequest {
  const OtpSendRequest({required this.email, required this.purpose});

  final String     email;
  final OtpPurpose purpose;

  Map<String, dynamic> toJson() => {
        'email':   email,
        'purpose': purpose.wireValue,
      };
}

/// Body for `POST /api/auth/verify-otp`. Matches backend `VerifyOtpAndIssueDto(Email, Code, Purpose)`.
class OtpVerifyRequest {
  const OtpVerifyRequest({
    required this.email,
    required this.code,
    required this.purpose,
  });

  final String     email;
  final String     code;
  final OtpPurpose purpose;

  Map<String, dynamic> toJson() => {
        'email':   email,
        'code':    code,
        'purpose': purpose.wireValue,
      };
}
