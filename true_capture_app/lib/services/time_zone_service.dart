class TimeZoneService {
  TimeZoneService._();
  static final TimeZoneService instance = TimeZoneService._();

  String get deviceTimeZoneName => DateTime.now().timeZoneName;

  Duration get deviceTimeZoneOffset => DateTime.now().timeZoneOffset;

  String get formattedOffset {
    final offset = deviceTimeZoneOffset;
    final sign = offset.isNegative ? '-' : '+';
    final hours = offset.inHours.abs().toString().padLeft(2, '0');
    final minutes = (offset.inMinutes.abs() % 60).toString().padLeft(2, '0');
    return 'GMT$sign$hours:$minutes';
  }
}
