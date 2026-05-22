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

  // Social — search, follow graph, profiles
  static const String userSearch    = '/api/users/search';
  static String userById(int id)        => '/api/users/$id';
  static String followUser(int id)      => '/api/users/$id/follow';
  static String userFollowers(int id)   => '/api/users/$id/followers';
  static String userFollowing(int id)   => '/api/users/$id/following';
  static String userPosts(int id)       => '/api/users/$id/posts';
  static const String followRequests = '/api/follow/requests';
  static String acceptRequest(int id)   => '/api/follow/requests/$id/accept';
  static String rejectRequest(int id)   => '/api/follow/requests/$id/reject';

  // Media — signed-URL upload pipeline
  static const String mediaUploads   = '/api/media/uploads';
  static const String mediaFinalize  = '/api/media/finalize';
  static String mediaBlob(int uploadId) => '/api/media/blob/$uploadId';

  // Feed — Home tab + Fake vs Real tab
  static const String feed           = '/api/feed';

  // Posts + engagement
  static const String posts          = '/api/posts';
  static const String adminPosts     = '/api/admin/posts';
  static const String mySaves        = '/api/users/me/saves';
  static String postById(int id)        => '/api/posts/$id';
  static String postLike(int id)        => '/api/posts/$id/like';
  static String postSave(int id)        => '/api/posts/$id/save';
  static String postShare(int id)       => '/api/posts/$id/share';
  static String postVote(int id)        => '/api/posts/$id/vote';
  static String postReport(int id)      => '/api/posts/$id/report';
  static String postComments(int id)    => '/api/posts/$id/comments';
  static String commentById(int id)     => '/api/comments/$id';
  static String commentReplies(int id)  => '/api/comments/$id/replies';
  static String commentLike(int id)     => '/api/comments/$id/like';

  // Messaging — 1-to-1 chat
  static const String conversations  = '/api/conversations';
  static String conversationMessages(int id) => '/api/conversations/$id/messages';
  static String conversationRead(int id)     => '/api/conversations/$id/read';
  static String conversationPin(int id)      => '/api/conversations/$id/pin';
  static String messageReact(int id)         => '/api/messages/$id/react';
  static String messageById(int id)          => '/api/messages/$id';
  /// SignalR chat hub path (joined to the API base, with `?access_token=`).
  static const String chatHub        = '/hubs/chat';

  // Stories
  static const String stories        = '/api/stories';
  static String storyById(int id)       => '/api/stories/$id';

  // Activity feed (notifications)
  static const String notifications          = '/api/notifications';
  static const String notificationsUnread    = '/api/notifications/unread-count';
  static const String notificationsReadAll   = '/api/notifications/read-all';

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
