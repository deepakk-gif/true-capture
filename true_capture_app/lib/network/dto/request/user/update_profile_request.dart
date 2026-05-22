/// Body of `PUT /api/users/me`. Maps to the backend `UpdateProfileRequest` DTO.
/// `displayName` / `bio` / `gender` are a full replace (null clears);
/// `accountType` null leaves it unchanged. `gender` is "male" | "female" |
/// "other" | null; `accountType` is "public" | "private".
class UpdateProfileRequest {
  const UpdateProfileRequest({
    this.displayName,
    this.bio,
    this.gender,
    this.accountType,
  });

  final String? displayName;
  final String? bio;
  final String? gender;
  final String? accountType;

  Map<String, dynamic> toJson() => {
    'displayName': displayName,
    'bio': bio,
    'gender': gender,
    'accountType': accountType,
  };
}
