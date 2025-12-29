// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'filter_config.dart';

// **************************************************************************
// JsonSerializableGenerator
// **************************************************************************

FilterConfig _$FilterConfigFromJson(Map<String, dynamic> json) => FilterConfig(
  columnId: json['columnId'] as String,
  type: json['type'] as String,
  value: json['value'],
);

Map<String, dynamic> _$FilterConfigToJson(FilterConfig instance) =>
    <String, dynamic>{
      'columnId': instance.columnId,
      'type': instance.type,
      'value': instance.value,
    };
