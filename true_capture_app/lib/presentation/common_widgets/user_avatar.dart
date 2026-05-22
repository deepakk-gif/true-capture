import 'package:flutter/material.dart';

import '../../config/app_config.dart';

/// Circular user avatar. Shows the network image when [avatarUrl] is set
/// (resolving relative backend paths against the API base), otherwise falls
/// back to the user's initials, then a generic person icon.
class UserAvatar extends StatelessWidget {
  const UserAvatar({
    super.key,
    this.avatarUrl,
    this.name,
    this.radius = 40,
  });

  final String? avatarUrl;
  final String? name;
  final double radius;

  @override
  Widget build(BuildContext context) {
    final resolved = AppConfig.resolveUrl(avatarUrl);
    final initials = _initials(name);
    final scheme = Theme.of(context).colorScheme;

    return CircleAvatar(
      radius: radius,
      backgroundColor: scheme.primaryContainer,
      foregroundImage: resolved != null ? NetworkImage(resolved) : null,
      child: initials.isNotEmpty
          ? Text(
              initials,
              style: TextStyle(
                fontSize: radius * 0.7,
                fontWeight: FontWeight.w600,
                color: scheme.onPrimaryContainer,
              ),
            )
          : Icon(Icons.person, size: radius, color: scheme.onPrimaryContainer),
    );
  }

  static String _initials(String? name) {
    final trimmed = name?.trim() ?? '';
    if (trimmed.isEmpty) return '';
    final parts = trimmed.split(RegExp(r'\s+'));
    return parts
        .take(2)
        .map((p) => p.isEmpty ? '' : p[0].toUpperCase())
        .join();
  }
}
