import 'package:flutter/material.dart';

/// What the user chose in the report sheet — a backend reason code + optional
/// free text (required when [reason] is `other`).
class ReportSelection {
  const ReportSelection(this.reason, this.otherText);
  final String reason;
  final String? otherText;
}

const _reasons = <({String code, String label})>[
  (code: 'spam',             label: 'Spam or misleading'),
  (code: 'misinformation',   label: 'False information'),
  (code: 'hateOrHarassment', label: 'Hate or harassment'),
  (code: 'nudityOrSexual',   label: 'Nudity or sexual content'),
  (code: 'violenceOrDanger', label: 'Violence or dangerous acts'),
  (code: 'other',            label: 'Something else'),
];

/// Shows the report bottom sheet and resolves to the user's choice (or null).
Future<ReportSelection?> showReportSheet(BuildContext context) {
  return showModalBottomSheet<ReportSelection>(
    context: context,
    isScrollControlled: true,
    builder: (_) => const _ReportSheet(),
  );
}

class _ReportSheet extends StatefulWidget {
  const _ReportSheet();

  @override
  State<_ReportSheet> createState() => _ReportSheetState();
}

class _ReportSheetState extends State<_ReportSheet> {
  String? _reason;
  final _otherController = TextEditingController();

  @override
  void dispose() {
    _otherController.dispose();
    super.dispose();
  }

  void _submit() {
    if (_reason == null) return;
    final other = _otherController.text.trim();
    if (_reason == 'other' && other.isEmpty) return;
    Navigator.pop(
      context,
      ReportSelection(_reason!, other.isEmpty ? null : other),
    );
  }

  @override
  Widget build(BuildContext context) {
    final canSubmit = _reason != null &&
        (_reason != 'other' || _otherController.text.trim().isNotEmpty);

    return Padding(
      padding: EdgeInsets.only(
        bottom: MediaQuery.of(context).viewInsets.bottom,
      ),
      child: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.fromLTRB(20, 16, 20, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text('Report this post',
                  style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              Text('Why are you reporting this post?',
                  style: TextStyle(color: Theme.of(context).hintColor)),
              const SizedBox(height: 8),
              RadioGroup<String>(
                groupValue: _reason,
                onChanged: (v) => setState(() => _reason = v),
                child: Column(
                  children: [
                    for (final r in _reasons)
                      RadioListTile<String>(
                        contentPadding: EdgeInsets.zero,
                        dense: true,
                        value: r.code,
                        title: Text(r.label),
                      ),
                  ],
                ),
              ),
              if (_reason == 'other')
                Padding(
                  padding: const EdgeInsets.only(top: 8),
                  child: TextField(
                    controller: _otherController,
                    maxLines: 3,
                    onChanged: (_) => setState(() {}),
                    decoration: const InputDecoration(
                      labelText: 'Tell us more',
                      border: OutlineInputBorder(),
                    ),
                  ),
                ),
              const SizedBox(height: 16),
              FilledButton(
                onPressed: canSubmit ? _submit : null,
                child: const Text('Submit report'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
