import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class CustomOtpField extends StatefulWidget {
  const CustomOtpField({
    super.key,
    this.length = 6,
    required this.onCompleted,
    this.onChanged,
    this.initialValue,
  });

  final int length;
  final ValueChanged<String> onCompleted;
  final ValueChanged<String>? onChanged;

  /// Pre-populates the cells (used for the local-env testing OTP). Purely
  /// visual — it does NOT auto-submit. Auto-submission is driven by the screen
  /// instead, so it can't re-fire when this widget is rebuilt/recreated.
  final String? initialValue;

  @override
  State<CustomOtpField> createState() => _CustomOtpFieldState();
}

class _CustomOtpFieldState extends State<CustomOtpField> {
  late final List<TextEditingController> _controllers;
  late final List<FocusNode> _focusNodes;

  @override
  void initState() {
    super.initState();
    final seed = widget.initialValue;
    final prefill = seed != null && seed.length == widget.length;
    _controllers = List.generate(
      widget.length,
      (i) => TextEditingController(text: prefill ? seed[i] : ''),
    );
    _focusNodes = List.generate(widget.length, (_) => FocusNode());
  }

  @override
  void dispose() {
    for (final c in _controllers) {
      c.dispose();
    }
    for (final n in _focusNodes) {
      n.dispose();
    }
    super.dispose();
  }

  void _onChanged(int index, String value) {
    if (value.isNotEmpty && index < widget.length - 1) {
      _focusNodes[index + 1].requestFocus();
    } else if (value.isEmpty && index > 0) {
      _focusNodes[index - 1].requestFocus();
    }
    final code = _controllers.map((c) => c.text).join();
    widget.onChanged?.call(code);
    if (code.length == widget.length) {
      widget.onCompleted(code);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: List.generate(widget.length, (i) {
        return SizedBox(
          width: 48,
          child: TextField(
            controller: _controllers[i],
            focusNode: _focusNodes[i],
            keyboardType: TextInputType.number,
            inputFormatters: [
              LengthLimitingTextInputFormatter(1),
              FilteringTextInputFormatter.digitsOnly,
            ],
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 22, fontWeight: FontWeight.w600),
            onChanged: (v) => _onChanged(i, v),
          ),
        );
      }),
    );
  }
}
