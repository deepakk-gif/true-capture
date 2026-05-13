import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../package/bottom_navigation/bottom_navigation.dart';
import '../../providers/vm_provider.dart';
import '../base/base_consumer_state.dart';
import 'main_view_model.dart';
import 'tabs/tab_home/tab_home.dart';
import 'tabs/tab_profile/tab_profile.dart';
import 'tabs/tab_settings/tab_settings.dart';

class MainScreen extends ConsumerStatefulWidget {
  const MainScreen({super.key});

  @override
  ConsumerState<MainScreen> createState() => _MainScreenState();
}

class _MainScreenState extends BaseConsumerState<MainScreen, MainViewModel> {
  static const _tabs = <Widget>[
    TabHome(),
    TabProfile(),
    TabSettings(),
  ];

  static const _items = <BottomNavItem>[
    BottomNavItem(icon: Icons.home_outlined, activeIcon: Icons.home, label: 'Home'),
    BottomNavItem(
        icon: Icons.person_outline, activeIcon: Icons.person, label: 'Profile'),
    BottomNavItem(
        icon: Icons.settings_outlined,
        activeIcon: Icons.settings,
        label: 'Settings'),
  ];

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
