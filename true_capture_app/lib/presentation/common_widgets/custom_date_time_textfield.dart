import 'package:flutter/material.dart';

import '../formater/date_formater.dart';

class CustomDateTimeTextField extends StatelessWidget {
  const CustomDateTimeTextField({
    super.key,
    this.label,
    this.value,
    required this.onChanged,
    this.firstDate,
    this.lastDate,
  });

  final String? label;
  final DateTime? value;
  final ValueChanged<DateTime> onChanged;
  final DateTime? firstDate;
  final DateTime? lastDate;

  Future<void> _pick(BuildContext context) async {
    final now = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: value ?? now,
      firstDate: firstDate ?? DateTime(now.year - 100),
      lastDate: lastDate ?? DateTime(now.year + 10),
    );
    if (picked != null) onChanged(picked);
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => _pick(context),
      child: AbsorbPointer(
        child: TextFormField(
          decoration: InputDecoration(
            labelText: label,
            suffixIcon: const Icon(Icons.calendar_today, size: 18),
          ),
          controller: TextEditingController(
            text: value == null ? '' : DateFormater.format(value!),
          ),
        ),
      ),
    );
  }
}
