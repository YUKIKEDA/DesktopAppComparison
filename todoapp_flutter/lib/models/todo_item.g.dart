// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'todo_item.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

TodoItem _$TodoItemFromJson(Map<String, dynamic> json) => TodoItem(
  id: (json['id'] as num).toInt(),
  title: json['title'] as String,
  description: json['description'] as String,
  status: json['status'] as String,
  priority: json['priority'] as String,
  dueDate: json['dueDate'] as String?,
  createdAt: json['createdAt'] as String,
  updatedAt: json['updatedAt'] as String,
  isCompleted: json['isCompleted'] as bool,
);

Map<String, dynamic> _$TodoItemToJson(TodoItem instance) => <String, dynamic>{
  'id': instance.id,
  'title': instance.title,
  'description': instance.description,
  'status': instance.status,
  'priority': instance.priority,
  'dueDate': instance.dueDate,
  'createdAt': instance.createdAt,
  'updatedAt': instance.updatedAt,
  'isCompleted': instance.isCompleted,
};
