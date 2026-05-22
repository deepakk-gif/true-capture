import 'dart:async';

import '../../../../log/app_logs.dart';
import '../../../../network/dto/social_models.dart';
import '../../../../repositories/social_repository.dart';
import '../../../../services/recent_search_service.dart';
import '../../base/base_view_model.dart';

class UserSearchViewModel extends BaseViewModel {
  UserSearchViewModel(this._socialRepo, this._recentSearch);

  final SocialRepository _socialRepo;
  final RecentSearchService _recentSearch;

  Timer? _debounce;
  String _query = '';
  bool searching = false;
  List<RecentSearchUser> recents = [];
  List<UserSearchItem> results = [];

  String get query => _query;
  bool get hasQuery => _query.trim().isNotEmpty;

  Future<void> loadRecents() async {
    recents = await _recentSearch.getAll();
    notifyListeners();
  }

  /// Debounces keystrokes — the API is hit 350 ms after typing stops.
  void onQueryChanged(String q) {
    _query = q;
    _debounce?.cancel();
    if (q.trim().isEmpty) {
      results = [];
      notifyListeners();
      return;
    }
    notifyListeners();
    _debounce = Timer(const Duration(milliseconds: 350), () => _runSearch(q.trim()));
  }

  Future<void> _runSearch(String q) async {
    searching = true;
    notifyListeners();
    try {
      results = await _socialRepo.search(q);
    } catch (e, s) {
      appLogError(e, s, 'USER_SEARCH');
      results = [];
    } finally {
      searching = false;
      notifyListeners();
    }
  }

  Future<void> remember(RecentSearchUser user) async {
    await _recentSearch.add(user);
    recents = await _recentSearch.getAll();
    notifyListeners();
  }

  Future<void> removeRecent(int id) async {
    await _recentSearch.remove(id);
    recents = await _recentSearch.getAll();
    notifyListeners();
  }

  Future<void> clearRecents() async {
    await _recentSearch.clear();
    recents = [];
    notifyListeners();
  }

  @override
  void dispose() {
    _debounce?.cancel();
    super.dispose();
  }
}
