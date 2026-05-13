import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../services/local_service.dart';
import 'local_storage_provider.dart';

class LanguageNotifier extends StateNotifier<Locale> {
  LanguageNotifier(this._storage) : super(const Locale('en')) {
    _load();
  }

  final LocalStorageService _storage;

  Future<void> _load() async {
    final value = await _storage.read(StorageKeys.languageCodeKey);
    if (value != null && value.isNotEmpty) {
      state = Locale(value);
    }
  }

  Future<void> setLocale(Locale locale) async {
    state = locale;
    await _storage.write(StorageKeys.languageCodeKey, locale.languageCode);
  }
}

final languageProvider =
    StateNotifierProvider<LanguageNotifier, Locale>((ref) {
  return LanguageNotifier(ref.read(localStorageServiceProvider));
});
