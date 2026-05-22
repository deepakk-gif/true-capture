// Relative time formatting shared by the conversation list and chat bubbles.

/// Compact form for the conversation list — `now`, `5m`, `3h`, `2d`, `3w`, `1y`.
String chatTimeShort(DateTime? utc) {
  if (utc == null) return '';
  final diff = DateTime.now().toUtc().difference(utc.toUtc());
  if (diff.inSeconds < 60) return 'now';
  if (diff.inMinutes < 60) return '${diff.inMinutes}m';
  if (diff.inHours < 24) return '${diff.inHours}h';
  if (diff.inDays < 7) return '${diff.inDays}d';
  if (diff.inDays < 365) return '${(diff.inDays / 7).floor()}w';
  return '${(diff.inDays / 365).floor()}y';
}

/// Verbose form for a message timestamp — "Just now", "a minute ago",
/// "3 hours ago", "1 week ago", "2 years ago".
String chatTimeRelative(DateTime? utc) {
  if (utc == null) return '';
  final diff = DateTime.now().toUtc().difference(utc.toUtc());
  if (diff.inSeconds < 45) return 'Just now';
  if (diff.inMinutes < 2) return 'a minute ago';
  if (diff.inMinutes < 60) return '${diff.inMinutes} minutes ago';
  if (diff.inHours < 2) return 'an hour ago';
  if (diff.inHours < 24) return '${diff.inHours} hours ago';
  if (diff.inDays < 2) return 'yesterday';
  if (diff.inDays < 7) return '${diff.inDays} days ago';
  if (diff.inDays < 14) return 'a week ago';
  if (diff.inDays < 365) return '${(diff.inDays / 7).floor()} weeks ago';
  if (diff.inDays < 730) return 'a year ago';
  return '${(diff.inDays / 365).floor()} years ago';
}
