import 'package:json_annotation/json_annotation.dart';
import 'todo_item.dart';

part 'project_data.g.dart';

@JsonSerializable()
class ProjectData {
  final List<TodoItem> items;

  ProjectData({required this.items});

  factory ProjectData.fromJson(Map<String, dynamic> json) =>
      _$ProjectDataFromJson(json);

  Map<String, dynamic> toJson() => _$ProjectDataToJson(this);
}
