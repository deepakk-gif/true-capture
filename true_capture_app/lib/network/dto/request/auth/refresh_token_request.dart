/// Body for `POST /api/auth/refresh` and `POST /api/auth/logout`.
/// Matches backend `RefreshDto(RefreshToken, FcmToken?, DeviceType?)`.
///
/// On `/refresh`, `fcmToken` (when set) bumps the matching `UserDevice.LastUsedAtUtc`.
/// On `/logout`, `fcmToken` (when set) deletes that device row + unsubscribes it
/// from topics so the signed-out device stops receiving pushes.
class RefreshTokenRequest {
  const RefreshTokenRequest({
    required this.refreshToken,
    this.fcmToken,
    this.deviceType,
  });

  final String  refreshToken;
  final String? fcmToken;
  final String? deviceType;

  Map<String, dynamic> toJson() => {
        'refreshToken': refreshToken,
        if (fcmToken   != null) 'fcmToken':   fcmToken,
        if (deviceType != null) 'deviceType': deviceType,
      };
}
