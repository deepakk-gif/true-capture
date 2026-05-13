class DropdownValueModel<T> {
  const DropdownValueModel({required this.label, required this.value});

  final String label;
  final T value;

  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is DropdownValueModel<T> && other.value == value);

  @override
  int get hashCode => value.hashCode;
}
