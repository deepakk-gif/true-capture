import '../../../../log/app_logs.dart';
import '../../../../network/dto/social_models.dart';
import '../../../../network/helper/error_handler.dart';
import '../../../../repositories/social_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

/// A single follow-request row with its mutable UI state. Once [accepted] is
/// true the row stays in the list and offers a "Follow back" action;
/// [followState] tracks the viewer → requester relationship.
class FollowRequestRow {
  FollowRequestRow(this.user) : followState = user.followState;

  final FollowUserItem user;
  bool accepted = false;
  String followState; // viewer -> requester: none | following | requested
}

/// Incoming follow requests — accept (then optionally follow back) or cancel.
class FollowRequestsViewModel extends BaseViewModel {
  FollowRequestsViewModel(this._socialRepo);

  final SocialRepository _socialRepo;

  List<FollowRequestRow> rows = [];
  final Set<int> _busy = {};

  bool isBusy(int id) => _busy.contains(id);

  Future<void> load() async {
    await executeWithLoading(
      operation: () async {
        final result = await _socialRepo.followRequests();
        rows = result.items.map(FollowRequestRow.new).toList();
        if (rows.isEmpty) changeScreenState(ScreenState.empty);
      },
    );
  }

  /// Accepts the request — the row stays and now offers "Follow back".
  Future<void> accept(int requesterId) => _guard(requesterId, () async {
        await _socialRepo.acceptRequest(requesterId);
        _rowOf(requesterId)?.accepted = true;
      });

  /// Declines the request — the row is removed.
  Future<void> cancel(int requesterId) => _guard(requesterId, () async {
        await _socialRepo.rejectRequest(requesterId);
        rows = rows.where((r) => r.user.id != requesterId).toList();
        if (rows.isEmpty) changeScreenState(ScreenState.empty);
      });

  /// Follows / unfollows the requester back (only meaningful once accepted).
  Future<void> toggleFollowBack(int requesterId) => _guard(requesterId, () async {
        final row = _rowOf(requesterId);
        if (row == null) return;
        final result = row.followState == FollowState.none
            ? await _socialRepo.follow(requesterId)
            : await _socialRepo.unfollow(requesterId);
        row.followState = result.followState;
      });

  FollowRequestRow? _rowOf(int id) {
    for (final r in rows) {
      if (r.user.id == id) return r;
    }
    return null;
  }

  Future<void> _guard(int id, Future<void> Function() op) async {
    if (_busy.contains(id)) return;
    _busy.add(id);
    notifyListeners();
    try {
      await op();
    } catch (e, s) {
      appLogError(e, s, 'FOLLOW_REQUESTS');
      setError(ErrorHandler.handle(e).message);
    } finally {
      _busy.remove(id);
      notifyListeners();
    }
  }
}
