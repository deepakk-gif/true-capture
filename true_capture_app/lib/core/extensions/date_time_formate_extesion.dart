import 'package:intl/intl.dart';

extension DateTimeFormatExtension on DateTime {
  String format(String pattern) => DateFormat(pattern).format(this);

  String get yyyyMMdd => format('yyyy-MM-dd');
  String get ddMMMyyyy => format('dd MMM yyyy');
  String get hhmma => format('hh:mm a');
  String get fullDateTime => format('dd MMM yyyy, hh:mm a');

  bool get isToday {
    final now = DateTime.now();
    return year == now.year && month == now.month && day == now.day;
  }
}
