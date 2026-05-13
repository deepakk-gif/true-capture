class UserResponse {
  const UserResponse({
    required this.id,
    required this.email,
    this.name,
    this.phone,
    this.avatarUrl,
    this.profileStatus,
  });

  final String id;
  final String email;
  final String? name;
  final String? phone;
  final String? avatarUrl;
  final String? profileStatus;

  factory UserResponse.fromJson(Map<String, dynamic> json) => UserResponse(
        id: json['id']?.toString() ?? '',
        email: json['email']?.toString() ?? '',
        name: json['name']?.toString(),
        phone: json['phone']?.toString(),
        avatarUrl: json['avatar_url']?.toString(),
        profileStatus: json['profile_status']?.toString(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'email': email,
        'name': name,
        'phone': phone,
        'avatar_url': avatarUrl,
        'profile_status': profileStatus,
      };
}
