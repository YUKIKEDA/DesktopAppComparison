package com.example.todoappkotlinmultiplatform.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.todoappkotlinmultiplatform.model.*
import com.example.todoappkotlinmultiplatform.util.currentTimeISOString
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.serialization.encodeToString
import kotlinx.serialization.json.Json

class TodoViewModel(
    private val dataService: IDataService,
    private val onNotify: (String) -> Unit = {}
) : ViewModel() {
    private val json = Json { prettyPrint = true }
    private val _items = MutableStateFlow<List<TodoItem>>(emptyList())
    val items: StateFlow<List<TodoItem>> = _items.asStateFlow()

    private val _selectedIds = MutableStateFlow<Set<Int>>(emptySet())
    val selectedIds: StateFlow<Set<Int>> = _selectedIds.asStateFlow()

    private val _filters = MutableStateFlow<List<FilterConfig>>(emptyList())
    val filters: StateFlow<List<FilterConfig>> = _filters.asStateFlow()

    private val _sorts = MutableStateFlow<List<SortConfig>>(emptyList())
    val sorts: StateFlow<List<SortConfig>> = _sorts.asStateFlow()

    private val _isLoading = MutableStateFlow(false)
    val isLoading: StateFlow<Boolean> = _isLoading.asStateFlow()

    private val _theme = MutableStateFlow("light")
    val theme: StateFlow<String> = _theme.asStateFlow()

    private var saveJob: Job? = null

    init {
        loadData()
        loadTheme()
    }

    private fun loadTheme() {
        viewModelScope.launch {
            try {
                _theme.value = dataService.loadTheme()
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    fun toggleTheme() {
        viewModelScope.launch {
            val next = if (_theme.value == "dark") "light" else "dark"
            _theme.value = next
            try {
                dataService.saveTheme(next)
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
    }

    private fun loadData() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                val data = dataService.loadData()
                _items.value = data.items
            } catch (e: Exception) {
                e.printStackTrace()
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun addItem(itemData: TodoItemInput) {
        val maxId = _items.value.maxOfOrNull { it.id } ?: 0
        val now = currentTimeISOString()
        val newItem = TodoItem(
            id = maxId + 1,
            title = itemData.title,
            description = itemData.description,
            status = itemData.status,
            priority = itemData.priority,
            dueDate = itemData.dueDate,
            createdAt = now,
            updatedAt = now,
            isCompleted = itemData.isCompleted
        )
        _items.update { it + newItem }
        scheduleSave()
    }

    fun updateItem(id: Int, updates: TodoItemInput) {
        _items.update { items ->
            items.map { item ->
                if (item.id == id) {
                    item.copy(
                        title = updates.title,
                        description = updates.description,
                        status = updates.status,
                        priority = updates.priority,
                        dueDate = updates.dueDate,
                        updatedAt = currentTimeISOString(),
                        isCompleted = updates.isCompleted
                    )
                } else {
                    item
                }
            }
        }
        scheduleSave()
    }

    fun deleteItems(ids: List<Int>) {
        _items.update { it.filterNot { item -> ids.contains(item.id) } }
        _selectedIds.update { it.filterNot { id -> ids.contains(id) }.toSet() }
        scheduleSave()
    }

    fun toggleSelection(id: Int) {
        _selectedIds.update { selected ->
            if (selected.contains(id)) {
                selected - id
            } else {
                selected + id
            }
        }
    }

    fun selectAll(filteredItemIds: List<Int>) {
        _selectedIds.update { selected ->
            selected + filteredItemIds.toSet()
        }
    }

    fun deselectAll(filteredItemIds: List<Int>) {
        _selectedIds.update { selected ->
            selected - filteredItemIds.toSet()
        }
    }

    fun setFilters(filters: List<FilterConfig>) {
        _filters.value = filters
    }

    fun setSorts(sorts: List<SortConfig>) {
        _sorts.value = sorts
    }

    fun toggleSort(columnId: String) {
        val currentSort = _sorts.value.find { it.columnId == columnId }
        val newSorts = if (currentSort != null) {
            when (currentSort.direction) {
                SortDirection.ASC -> {
                    _sorts.value.filter { it.columnId != columnId } +
                            SortConfig(columnId, SortDirection.DESC)
                }
                SortDirection.DESC -> {
                    _sorts.value.filter { it.columnId != columnId }
                }
            }
        } else {
            _sorts.value + SortConfig(columnId, SortDirection.ASC)
        }
        _sorts.value = newSorts
    }

    fun getFilteredItems(): List<TodoItem> {
        var result = _items.value

        // Apply filters
        _filters.value.forEach { filter ->
            result = when (filter.type) {
                FilterType.TEXT -> {
                    if (filter.value is FilterValue.Text) {
                        val searchTerm = filter.value.value.lowercase()
                        result.filter { item ->
                            when (filter.columnId) {
                                "title" -> {
                                    item.title.lowercase().contains(searchTerm) ||
                                            item.description.lowercase().contains(searchTerm)
                                }
                                "description" -> item.description.lowercase().contains(searchTerm)
                                else -> true
                            }
                        }
                    } else {
                        result
                    }
                }
                FilterType.SELECT -> {
                    if (filter.value is FilterValue.Select) {
                        val filterValues = filter.value.values
                        result.filter { item ->
                            when (filter.columnId) {
                                "status" -> filterValues.contains(item.status.name)
                                "priority" -> filterValues.contains(item.priority.name)
                                else -> true
                            }
                        }
                    } else {
                        result
                    }
                }
                FilterType.DATE -> result // TODO: Implement date filtering if needed
            }
        }

        // Apply sorts
        _sorts.value.forEach { sort ->
            result = result.sortedWith { a, b ->
                val comparison = when (sort.columnId) {
                    "id" -> a.id.compareTo(b.id)
                    "title" -> a.title.compareTo(b.title)
                    "description" -> a.description.compareTo(b.description)
                    "status" -> a.status.name.compareTo(b.status.name)
                    "priority" -> a.priority.name.compareTo(b.priority.name)
                    "dueDate" -> {
                        val aDate = a.dueDate ?: ""
                        val bDate = b.dueDate ?: ""
                        aDate.compareTo(bDate)
                    }
                    "createdAt" -> a.createdAt.compareTo(b.createdAt)
                    "updatedAt" -> a.updatedAt.compareTo(b.updatedAt)
                    else -> 0
                }
                if (sort.direction == SortDirection.DESC) -comparison else comparison
            }
        }

        return result
    }

    private fun scheduleSave() {
        saveJob?.cancel()
        saveJob = viewModelScope.launch {
            delay(2000) // 2 seconds debounce
            saveData()
        }
    }

    fun saveData(notify: Boolean = false) {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                dataService.saveData(ProjectData(_items.value))
                if (notify) {
                    onNotify("保存しました")
                }
            } catch (e: Exception) {
                e.printStackTrace()
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun exportData() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                dataService.exportData(ProjectData(_items.value))
            } catch (e: Exception) {
                e.printStackTrace()
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun importData() {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                val result = dataService.importData()
                result.onSuccess { data ->
                    applyImportedData(data)
                    onNotify("インポートしました")
                }
            } catch (e: Exception) {
                e.printStackTrace()
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun importFromPath(path: String) {
        viewModelScope.launch {
            _isLoading.value = true
            try {
                val result = dataService.importFromPath(path)
                result.onSuccess { data ->
                    applyImportedData(data)
                    onNotify("インポートしました")
                }
            } catch (e: Exception) {
                e.printStackTrace()
            } finally {
                _isLoading.value = false
            }
        }
    }

    fun copySelectedAsJson(): String? {
        val selected = _items.value.filter { _selectedIds.value.contains(it.id) }
        if (selected.isEmpty()) return null
        return json.encodeToString(selected)
    }

    private fun applyImportedData(data: ProjectData) {
        _items.value = data.items
        _selectedIds.value = emptySet()
        saveData(notify = false)
    }

    fun getItemById(id: Int): TodoItem? = _items.value.find { it.id == id }

    fun openDataFolder() {
        viewModelScope.launch {
            dataService.openDataFolder()
        }
    }
}

data class TodoItemInput(
    val title: String,
    val description: String = "",
    val status: TodoStatus,
    val priority: TodoPriority,
    val dueDate: String? = null,
    val isCompleted: Boolean = false
)
