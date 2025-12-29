import 'package:flutter/material.dart';
import 'package:flutter_form_builder/flutter_form_builder.dart';
import 'package:form_builder_validators/form_builder_validators.dart';
import 'package:intl/intl.dart';
import '../models/todo_item.dart';

class TodoForm extends StatefulWidget {
  final TodoItem? item;
  final Function({
    required String title,
    required String description,
    required String status,
    required String priority,
    String? dueDate,
  })
  onSubmit;
  final VoidCallback onCancel;

  const TodoForm({
    super.key,
    this.item,
    required this.onSubmit,
    required this.onCancel,
  });

  @override
  State<TodoForm> createState() => _TodoFormState();
}

class _TodoFormState extends State<TodoForm> {
  final _formKey = GlobalKey<FormBuilderState>();
  final _titleController = TextEditingController();
  final _descriptionController = TextEditingController();
  String _status = '未着手';
  String _priority = '中';
  DateTime? _dueDate;

  @override
  void initState() {
    super.initState();
    if (widget.item != null) {
      _titleController.text = widget.item!.title;
      _descriptionController.text = widget.item!.description;
      _status = widget.item!.status;
      _priority = widget.item!.priority;
      if (widget.item!.dueDate != null) {
        _dueDate = DateTime.tryParse(widget.item!.dueDate!);
      }
    }
  }

  @override
  void dispose() {
    _titleController.dispose();
    _descriptionController.dispose();
    super.dispose();
  }

  Future<void> _selectDate() async {
    if (!mounted) return;
    final picked = await showDatePicker(
      context: context,
      initialDate: _dueDate ?? DateTime.now(),
      firstDate: DateTime(2000),
      lastDate: DateTime(2100),
    );
    if (picked != null && mounted) {
      final time = await showTimePicker(
        context: context,
        initialTime: _dueDate != null
            ? TimeOfDay.fromDateTime(_dueDate!)
            : TimeOfDay.now(),
      );
      if (time != null && mounted) {
        setState(() {
          _dueDate = DateTime(
            picked.year,
            picked.month,
            picked.day,
            time.hour,
            time.minute,
          );
        });
      }
    }
  }

  void _handleSubmit() {
    if (_formKey.currentState?.saveAndValidate() ?? false) {
      widget.onSubmit(
        title: _titleController.text,
        description: _descriptionController.text,
        status: _status,
        priority: _priority,
        dueDate: _dueDate?.toIso8601String(),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return FormBuilder(
      key: _formKey,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          FormBuilderTextField(
            name: 'title',
            controller: _titleController,
            decoration: const InputDecoration(
              labelText: 'タイトル *',
              border: OutlineInputBorder(),
            ),
            validator: FormBuilderValidators.compose([
              FormBuilderValidators.required(errorText: 'タイトルは必須です'),
              FormBuilderValidators.maxLength(200, errorText: 'タイトルは200文字以内です'),
            ]),
          ),
          const SizedBox(height: 16),
          FormBuilderTextField(
            name: 'description',
            controller: _descriptionController,
            decoration: const InputDecoration(
              labelText: '説明',
              border: OutlineInputBorder(),
            ),
            maxLines: 3,
            validator: FormBuilderValidators.maxLength(
              500,
              errorText: '説明は500文字以内です',
            ),
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: FormBuilderDropdown<String>(
                  name: 'status',
                  decoration: const InputDecoration(
                    labelText: 'ステータス',
                    border: OutlineInputBorder(),
                  ),
                  initialValue: _status,
                  items: const [
                    DropdownMenuItem(value: '未着手', child: Text('未着手')),
                    DropdownMenuItem(value: '進行中', child: Text('進行中')),
                    DropdownMenuItem(value: '完了', child: Text('完了')),
                  ],
                  onChanged: (value) {
                    if (value != null) {
                      setState(() {
                        _status = value;
                      });
                    }
                  },
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: FormBuilderDropdown<String>(
                  name: 'priority',
                  decoration: const InputDecoration(
                    labelText: '優先度',
                    border: OutlineInputBorder(),
                  ),
                  initialValue: _priority,
                  items: const [
                    DropdownMenuItem(value: '低', child: Text('低')),
                    DropdownMenuItem(value: '中', child: Text('中')),
                    DropdownMenuItem(value: '高', child: Text('高')),
                  ],
                  onChanged: (value) {
                    if (value != null) {
                      setState(() {
                        _priority = value;
                      });
                    }
                  },
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          InkWell(
            onTap: _selectDate,
            child: InputDecorator(
              decoration: const InputDecoration(
                labelText: '期限',
                border: OutlineInputBorder(),
                suffixIcon: Icon(Icons.calendar_today),
              ),
              child: Text(
                _dueDate != null
                    ? DateFormat('yyyy-MM-dd HH:mm').format(_dueDate!)
                    : '期限を選択',
                style: TextStyle(
                  color: _dueDate != null ? Colors.black87 : Colors.grey,
                ),
              ),
            ),
          ),
          const SizedBox(height: 24),
          Row(
            mainAxisAlignment: MainAxisAlignment.end,
            children: [
              OutlinedButton(
                onPressed: widget.onCancel,
                child: const Text('キャンセル'),
              ),
              const SizedBox(width: 8),
              ElevatedButton(
                onPressed: _handleSubmit,
                child: Text(widget.item != null ? '更新' : '追加'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
