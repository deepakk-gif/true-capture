import 'user_response.dart';

/// Matches backend `AuthTokensDto(AccessToken, RefreshToken, AccessExpiresAtUtc)`.
/// ASP.NET Core serializes properties as camelCase under `JsonSerializerDefaults.Web`,
/// so we read `accessToken` / `refreshToken` / `accessExpiresAtUtc` (snake-case
/// fallback is preserved for older builds that may still emit the old shape).
class AuthResponse {
  const AuthResponse({
    required this.accessToken,
    required this.refreshToken,
    this.accessExpiresAtUtc,
    this.user,
  });

  final String       accessToken;
  final String       refreshToken;
  final DateTime?    accessExpiresAtUtc;
  final UserResponse? user;

  factory AuthResponse.fromJson(Map<String, dynamic> json) => AuthResponse(
        accessToken:        (json['accessToken']  ?? json['access_token']  ?? '').toString(),
        refreshToken:       (json['refreshToken'] ?? json['refresh_token'] ?? '').toString(),
        accessExpiresAtUtc: _parseDate(json['accessExpiresAtUtc'] ?? json['access_expires_at_utc']),
        user: json['user'] is Map<String, dynamic>
            ? UserResponse.fromJson(json['user'] as Map<String, dynamic>)
            : null,
      );

  Map<String, dynamic> toJson() => {
        'accessToken':  accessToken,
        'refreshToken': refreshToken,
        if (accessExpiresAtUtc != null)
          'accessExpiresAtUtc': accessExpiresAtUtc!.toIso8601String(),
        if (user != null) 'user': user!.toJson(),
      };

  static DateTime? _parseDate(Object? v) {
    if (v == null) return null;
    if (v is DateTime) return v;
    return DateTime.tryParse(v.toString());
  }
}
