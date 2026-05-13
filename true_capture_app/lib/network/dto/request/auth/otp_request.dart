class OtpSendRequest {
  const OtpSendRequest({required this.email});

  final String email;

  Map<String, dynamic> toJson() => {'email': email};
}

class OtpVerifyRequest {
  const OtpVerifyRequest({required this.email, required this.otp});

  final String email;
  final String otp;

  Map<String, dynamic> toJson() => {'email': email, 'otp': otp};
}
