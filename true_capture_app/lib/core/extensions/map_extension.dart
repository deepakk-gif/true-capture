extension MapExtension on Map<String, dynamic> {
  T? getOrNull<T>(String key) {
    final value = this[key];
    if (value is T) return value;
    return null;
  }

  Map<String, dynamic> withoutNulls() {
    return Map<String, dynamic>.from(this)
      ..removeWhere((_, value) => value == null);
  }
}
