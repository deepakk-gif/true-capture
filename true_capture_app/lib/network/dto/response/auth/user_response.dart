class UserResponse {
  const UserResponse({
    required this.id,
    required this.email,
    this.name,
    this.phone,
    this.avatarUrl,
    this.profileStatus,
    this.emailVerified = false,
  });

  final String id;
  final String email;
  final String? name;
  final String? phone;
  final String? avatarUrl;
  final String? profileStatus;
  final bool emailVerified;

  factory UserResponse.fromJson(Map<String, dynamic> json) => UserResponse(
        id: json['id']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        name: (json['name'] ?? json['displayName'] ?? json['display_name'])?.toString(),
        phone: json['phone']?.toString(),
        avatarUrl: (json['avatarUrl'] ?? json['avatar_url'])?.toString(),
        profileStatus: (json['profileStatus'] ?? json['profile_status'])?.toString(),
        emailVerified: (json['emailVerified'] ?? json['email_verified']) == true,
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'name': name,
        'phone': phone,
        'avatarUrl': avatarUrl,
        'profileStatus': profileStatus,
        'emailVerified': emailVerified,
      };
}
