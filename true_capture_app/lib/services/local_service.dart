import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class LocalStorageService {
  factory LocalStorageService() => _instance;
  LocalStorageService._internal();
  static final LocalStorageService _instance = LocalStorageService._internal();

  final FlutterSecureStorage _storage = const FlutterSecureStorage(
    aOptions: AndroidOptions(encryptedSharedPreferences: true),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock_this_device,
    ),
  );

  Future<void> write(String key, String value) =>
      _storage.write(key: key, value: value);

  Future<String?> read(String key) => _storage.read(key: key);

  Future<bool> readBool(String key) async =>
      (await _storage.read(key: key)) == 'true';

  Future<void> writeBool(String key, bool value) =>
      _storage.write(key: key, value: value.toString());

  Future<void> delete(String key) => _storage.delete(key: key);

  Future<void> deleteAll() => _storage.deleteAll();

  Future<bool> contains(String key) => _storage.containsKey(key: key);
}
