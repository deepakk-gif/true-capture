import 'package:flutter/material.dart';

import 'user_avatar.dart';

/// A compact user row reused by search results, follower / following lists,
/// and follow-request lists. [trailing] hosts the contextual action (follow
/// button, accept / reject, ...).
class UserListRow extends StatelessWidget {
  const UserListRow({
    super.key,
    required this.username,
    this.displayName,
    this.avatarUrl,
    this.isBlueTick = false,
    this.subtitle,
    this.trailing,
    this.onTap,
  });

  final String username;
  final String? displayName;
  final String? avatarUrl;
  final bool isBlueTick;
  final String? subtitle;
  final Widget? trailing;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      onTap: onTap,
      leading: UserAvatar(avatarUrl: avatarUrl, name: displayName ?? username, radius: 24),
      title: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Flexible(
            child: Text(
              displayName ?? username,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(fontWeight: FontWeight.w600),
            ),
          ),
          if (isBlueTick) ...[
            const SizedBox(width: 4),
            const Icon(Icons.verified, size: 15, color: Colors.blue),
          ],
        ],
      ),
      subtitle: Text(
        subtitle ?? '@$username',
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      trailing: trailing,
    );
  }
}
