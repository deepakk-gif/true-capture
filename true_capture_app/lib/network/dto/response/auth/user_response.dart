/// Current-user profile. Mirrors the backend `UserProfileResponse`
/// (`GET /api/users/me`) and the (slimmer) `user` object some auth responses
/// may carry. Unknown/missing fields fall back to safe defaults.
class UserResponse {
  const UserResponse({
    required this.id,
    required this.email,
    this.username,
    this.name,
    this.phone,
    this.avatarUrl,
    this.bio,
    this.profileStatus,
    this.joinedAt,
    this.followersCount = 0,
    this.followingCount = 0,
    this.postsCount = 0,
    this.emailVerified = false,
    this.isBlueTick = false,
    this.isSuspended = false,
    this.accountType,
    this.gender,
  });

  final String id;
  final String email;
  final String? username;
  final String? name;          // display name
  final String? phone;
  final String? avatarUrl;     // profile photo
  final String? bio;
  final String? profileStatus;
  final DateTime? joinedAt;    // joining date
  final int followersCount;
  final int followingCount;
  final int postsCount;
  final bool emailVerified;
  final bool isBlueTick;       // verified / premium badge
  final bool isSuspended;
  final String? accountType;   // "public" | "private"
  final String? gender;        // "male" | "female" | "other"

  factory UserResponse.fromJson(Map<String, dynamic> json) => UserResponse(
        id: json['id']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        username: json['username']?.toString(),
        name: (json['name'] ?? json['displayName'] ?? json['display_name'])
            ?.toString(),
        phone: json['phone']?.toString(),
        avatarUrl: (json['avatarUrl'] ?? json['avatar_url'])?.toString(),
        bio: json['bio']?.toString(),
        profileStatus:
            (json['profileStatus'] ?? json['profile_status'])?.toString(),
        joinedAt: _parseDate(
            json['joinedAtUtc'] ?? json['createdAtUtc'] ?? json['created_at']),
        followersCount: _parseInt(json['followersCount']),
        followingCount: _parseInt(json['followingCount']),
        postsCount: _parseInt(json['postsCount']),
        emailVerified: (json['emailVerified'] ?? json['email_verified']) == true,
        isBlueTick:
            (json['isBlueTick'] ?? json['isVerified'] ?? json['is_verified']) ==
                true,
        isSuspended: (json['isSuspended'] ?? json['is_suspended']) == true,
        accountType: json['accountType']?.toString(),
        gender: json['gender']?.toString(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'username': username,
        'name': name,
        'phone': phone,
        'avatarUrl': avatarUrl,
        'bio': bio,
        'profileStatus': profileStatus,
        'joinedAtUtc': joinedAt?.toIso8601String(),
        'followersCount': followersCount,
        'followingCount': followingCount,
        'postsCount': postsCount,
        'emailVerified': emailVerified,
        'isBlueTick': isBlueTick,
        'isSuspended': isSuspended,
        'accountType': accountType,
        'gender': gender,
      };

  static int _parseInt(Object? v) {
    if (v is int) return v;
    if (v is num) return v.toInt();
    return int.tryParse(v?.toString() ?? '') ?? 0;
  }

  static DateTime? _parseDate(Object? v) {
    if (v == null) return null;
    if (v is DateTime) return v;
    return DateTime.tryParse(v.toString());
  }
}
