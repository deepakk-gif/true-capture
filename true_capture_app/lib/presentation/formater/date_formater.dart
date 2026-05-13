import 'package:intl/intl.dart';

class DateFormater {
  DateFormater._();

  static String format(DateTime date, {String pattern = 'dd MMM yyyy'}) =>
      DateFormat(pattern).format(date);

  static String? parseAndFormat(String? source,
      {String pattern = 'dd MMM yyyy'}) {
    if (source == null || source.isEmpty) return null;
    final parsed = DateTime.tryParse(source);
    if (parsed == null) return null;
    return format(parsed, pattern: pattern);
  }
}
