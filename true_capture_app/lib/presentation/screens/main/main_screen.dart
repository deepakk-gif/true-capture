import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/router/app_router.dart';
import '../../../package/bottom_navigation/bottom_navigation.dart';
import '../../../network/dto/message_models.dart';
import '../../../services/chat_socket_service.dart';
import '../../../services/firebase_service.dart';
import '../../../services/local_notification_service.dart';
import '../../providers/local_storage_provider.dart';
import '../../providers/user_data_provider.dart';
import '../../providers/vm_provider.dart';
import '../base/base_consumer_state.dart';
import 'main_view_model.dart';
import 'tabs/tab_create_post/tab_create_post.dart';
import 'tabs/tab_fake_vs_real/tab_fake_vs_real.dart';
import 'tabs/tab_home/tab_home.dart';
import 'tabs/tab_message/tab_message.dart';
import 'tabs/tab_profile/tab_profile.dart';

class MainScreen extends ConsumerStatefulWidget {
  const MainScreen({super.key});

  @override
  ConsumerState<MainScreen> createState() => _MainScreenState();
}

class _MainScreenState extends BaseConsumerState<MainScreen, MainViewModel> {
  static const _tabs = <Widget>[
    TabHome(),
    TabFakeVsReal(),
    TabCreatePost(),
    TabMessage(),
    TabProfile(),
  ];

  static const _items = <BottomNavItem>[
    BottomNavItem(
        icon: Icons.home_outlined, activeIcon: Icons.home, label: 'Home'),
    BottomNavItem(
        icon: Icons.fact_check_outlined,
        activeIcon: Icons.fact_check,
        label: 'Fake vs Real'),
    BottomNavItem(
        icon: Icons.add_box_outlined,
        activeIcon: Icons.add_box,
        label: 'Create'),
    BottomNavItem(
        icon: Icons.chat_bubble_outline,
        activeIcon: Icons.chat_bubble,
        label: 'Message'),
    BottomNavItem(
        icon: Icons.person_outline, activeIcon: Icons.person, label: 'Profile'),
  ];

  StreamSubscription<MessageDto>? _messageSub;

  /// Opens the realtime chat connection and shows a local notification for any
  /// inbound message that isn't from the user and isn't in the open chat.
  @override
  void onModelReady(MainViewModel model) {
    unawaited(_bootstrapChat());
  }

  Future<void> _bootstrapChat() async {
    await LocalNotificationService.instance.initialize();
    final storage = ref.read(localStorageServiceProvider);
    await ChatSocketService.instance
        .connect(() => storage.read(StorageKeys.accessTokenKey));

    final myId = int.tryParse(ref.read(authStateNotifierProvider)?.id ?? '') ?? 0;
    _messageSub = ChatSocketService.instance.onMessage.listen((m) {
      if (m.senderId == myId) return;
      if (m.conversationId == ChatSocketService.instance.activeConversationId) {
        return;
      }
      LocalNotificationService.instance.showMessage(
        conversationId: m.conversationId,
        title: 'New message',
        body: m.text ?? (m.isVideo ? '🎞 Video' : '📷 Photo'),
      );
    });

    // Honour a notification that launched the app from a killed state.
    final launchData = FirebaseService.takePendingLaunch();
    if (launchData != null) {
      FirebaseService.routeNotificationData(launchData);
    } else {
      final convId = await LocalNotificationService.instance.launchConversationId();
      if (convId != null && mounted) {
        AppRouter.push(context, ScreenPath.routeChat,
            extra: {'conversationId': convId});
      }
    }
  }

  @override
  void dispose() {
    _messageSub?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: viewModel,
      builder: (context, _) => Scaffold(
        body: IndexedStack(index: viewModel.currentIndex, children: _tabs),
        bottomNavigationBar: CustomBottomNavigation(
          items: _items,
          currentIndex: viewModel.currentIndex,
          onTap: viewModel.changeTab,
        ),
      ),
    );
  }

  @override
  MainViewModel createViewModel() => ref.read(mainVm);

  @override
  String screenName() => 'MAIN';
}
