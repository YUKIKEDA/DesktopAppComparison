// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'project_data.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

ProjectData _$ProjectDataFromJson(Map<String, dynamic> json) => ProjectData(
  items: (json['items'] as List<dynamic>)
      .map((e) => TodoItem.fromJson(e as Map<String, dynamic>))
      .toList(),
);

Map<String, dynamic> _$ProjectDataToJson(ProjectData instance) =>
    <String, dynamic>{'items': instance.items};
