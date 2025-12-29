import 'package:json_annotation/json_annotation.dart';

part 'filter_config.g.dart';

@JsonSerializable()
class FilterConfig {
  @JsonKey(name: 'columnId')
  final String columnId;
  final String type; // "text" | "date" | "select"
  final dynamic
  value; // string | string[] | { from: string | null; to: string | null }

  FilterConfig({
    required this.columnId,
    required this.type,
    required this.value,
  });

  factory FilterConfig.fromJson(Map<String, dynamic> json) =>
      _$FilterConfigFromJson(json);

  Map<String, dynamic> toJson() => _$FilterConfigToJson(this);
}
