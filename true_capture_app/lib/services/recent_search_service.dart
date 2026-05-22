import 'dart:convert';

import '../presentation/providers/local_storage_provider.dart';
import 'local_service.dart';

/// A user remembered in the local recent-search history.
class RecentSearchUser {
  const RecentSearchUser({
    required this.id,
    required this.username,
    this.displayName,
    this.avatarUrl,
  });

  final int id;
  final String username;
  final String? displayName;
  final String? avatarUrl;

  Map<String, dynamic> toJson() => {
        'id': id,
        'username': username,
        'displayName': displayName,
        'avatarUrl': avatarUrl,
      };

  factory RecentSearchUser.fromJson(Map<String, dynamic> j) => RecentSearchUser(
        id: (j['id'] as num?)?.toInt() ?? 0,
        username: j['username']?.toString() ?? '',
        displayName: j['displayName']?.toString(),
        avatarUrl: j['avatarUrl']?.toString(),
      );
}

/// Recent-search history, stored **locally only** (no API). Holds the most
/// recently opened users, newest first, capped at [_max].
class RecentSearchService {
  RecentSearchService(this._storage);

  final LocalStorageService _storage;
  static const int _max = 12;

  Future<List<RecentSearchUser>> getAll() async {
    final raw = await _storage.read(StorageKeys.recentSearchesKey);
    if (raw == null || raw.isEmpty) return [];
    try {
      return (jsonDecode(raw) as List)
          .map((e) => RecentSearchUser.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (_) {
      return [];
    }
  }

  /// Adds [user] to the front, de-duplicating by id and trimming to [_max].
  Future<void> add(RecentSearchUser user) async {
    final list = await getAll()
      ..removeWhere((u) => u.id == user.id);
    list.insert(0, user);
    await _save(list.take(_max).toList());
  }

  Future<void> remove(int id) async {
    final list = await getAll()
      ..removeWhere((u) => u.id == id);
    await _save(list);
  }

  Future<void> clear() => _storage.delete(StorageKeys.recentSearchesKey);

  Future<void> _save(List<RecentSearchUser> list) => _storage.write(
        StorageKeys.recentSearchesKey,
        jsonEncode(list.map((u) => u.toJson()).toList()),
      );
}
