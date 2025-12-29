import 'package:json_annotation/json_annotation.dart';

part 'sort_config.g.dart';

@JsonSerializable()
class SortConfig {
  @JsonKey(name: 'columnId')
  final String columnId;
  final String? direction; // "asc" | "desc" | null

  SortConfig({required this.columnId, this.direction});

  factory SortConfig.fromJson(Map<String, dynamic> json) =>
      _$SortConfigFromJson(json);

  Map<String, dynamic> toJson() => _$SortConfigToJson(this);
}
