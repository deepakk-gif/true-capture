import '../../../../log/app_logs.dart';
import '../../../../network/dto/activity_models.dart';
import '../../../../repositories/notification_repository.dart';
import '../../base/base_view_model.dart';
import '../../base/screen_state.dart';

/// The activity feed. Marks everything read on open (clears the badge).
class NotificationFeedViewModel extends BaseViewModel {
  NotificationFeedViewModel(this._repo);

  final NotificationRepository _repo;

  List<NotificationItem> items = [];

  Future<void> load() async {
    await executeWithLoading(
      operation: () async {
        items = await _repo.feed();
        if (items.isEmpty) changeScreenState(ScreenState.empty);
        // Opening the feed clears the unread badge — best-effort.
        try {
          await _repo.markAllRead();
        } catch (e, s) {
          appLogError(e, s, 'NOTIFICATIONS');
        }
      },
    );
  }
}
