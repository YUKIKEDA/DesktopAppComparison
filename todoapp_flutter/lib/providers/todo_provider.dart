import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../models/todo_item.dart';
import '../models/project_data.dart';
import '../models/filter_config.dart';
import '../models/sort_config.dart';
import '../services/data_service.dart';

class TodoState {
  final List<TodoItem> items;
  final Set<int> selectedIds;
  final List<FilterConfig> filters;
  final List<SortConfig> sorts;
  final bool isLoading;

  TodoState({
    this.items = const [],
    this.selectedIds = const {},
    this.filters = const [],
    this.sorts = const [],
    this.isLoading = false,
  });

  TodoState copyWith({
    List<TodoItem>? items,
    Set<int>? selectedIds,
    List<FilterConfig>? filters,
    List<SortConfig>? sorts,
    bool? isLoading,
  }) {
    return TodoState(
      items: items ?? this.items,
      selectedIds: selectedIds ?? this.selectedIds,
      filters: filters ?? this.filters,
      sorts: sorts ?? this.sorts,
      isLoading: isLoading ?? this.isLoading,
    );
  }
}

class TodoNotifier extends StateNotifier<TodoState> {
  TodoNotifier() : super(TodoState());

  void setItems(List<TodoItem> items) {
    state = state.copyWith(items: items);
  }

  void addItem({
    required String title,
    required String description,
    required String status,
    required String priority,
    String? dueDate,
    bool isCompleted = false,
  }) {
    final now = DateTime.now().toIso8601String();
    final maxId = state.items.isEmpty
        ? 0
        : state.items.map((i) => i.id).reduce((a, b) => a > b ? a : b);
    final newItem = TodoItem(
      id: maxId + 1,
      title: title,
      description: description,
      status: status,
      priority: priority,
      dueDate: dueDate,
      createdAt: now,
      updatedAt: now,
      isCompleted: isCompleted,
    );
    state = state.copyWith(items: [...state.items, newItem]);
  }

  void updateItem(
    int id, {
    String? title,
    String? description,
    String? status,
    String? priority,
    String? dueDate,
    bool? isCompleted,
  }) {
    state = state.copyWith(
      items: state.items.map((item) {
        if (item.id == id) {
          return item.copyWith(
            title: title,
            description: description,
            status: status,
            priority: priority,
            dueDate: dueDate,
            isCompleted: isCompleted,
            updatedAt: DateTime.now().toIso8601String(),
          );
        }
        return item;
      }).toList(),
    );
  }

  void deleteItems(List<int> ids) {
    final newItems = state.items
        .where((item) => !ids.contains(item.id))
        .toList();
    final newSelectedIds = state.selectedIds
        .where((id) => !ids.contains(id))
        .toSet();
    state = state.copyWith(items: newItems, selectedIds: newSelectedIds);
  }

  void toggleSelection(int id) {
    final newSelectedIds = Set<int>.from(state.selectedIds);
    if (newSelectedIds.contains(id)) {
      newSelectedIds.remove(id);
    } else {
      newSelectedIds.add(id);
    }
    state = state.copyWith(selectedIds: newSelectedIds);
  }

  void selectAll(List<TodoItem> items) {
    state = state.copyWith(selectedIds: items.map((item) => item.id).toSet());
  }

  void deselectAll() {
    state = state.copyWith(selectedIds: {});
  }

  void setFilters(List<FilterConfig> filters) {
    state = state.copyWith(filters: filters);
  }

  void setSorts(List<SortConfig> sorts) {
    state = state.copyWith(sorts: sorts);
  }

  void setLoading(bool loading) {
    state = state.copyWith(isLoading: loading);
  }

  Future<void> loadData() async {
    setLoading(true);
    try {
      final data = await DataService.loadData();
      setItems(data.items);
    } catch (e) {
      debugPrint('Failed to load data: $e');
    } finally {
      setLoading(false);
    }
  }

  Future<void> saveData() async {
    setLoading(true);
    try {
      final projectData = ProjectData(items: state.items);
      await DataService.saveData(projectData);
    } catch (e) {
      debugPrint('Failed to save data: $e');
      rethrow;
    } finally {
      setLoading(false);
    }
  }
}

final todoProvider = StateNotifierProvider<TodoNotifier, TodoState>((ref) {
  return TodoNotifier();
});
