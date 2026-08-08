using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using ToDoApp.WinUI.Models;
using ToDoApp.WinUI.Services;

namespace ToDoApp.WinUI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private const int PageSize = 100;

        private readonly IDataService _dataService;
        private readonly DispatcherQueue _dispatcherQueue;

        public DispatcherQueue DispatcherQueue => _dispatcherQueue;
        private System.Threading.Timer? _autoSaveTimer;
        private List<TodoItem> _filteredSource = new();
        private int _visibleCount = PageSize;
        private int _filterGeneration;
        private bool _loadingMore;
        private readonly List<Window> _detailWindows = new();
        private bool _cleanedUp;
        private bool _suspendAutoFilter;

        [ObservableProperty]
        private ObservableCollection<TodoItem> _items = new();

        [ObservableProperty]
        private ObservableCollection<TodoItem> _filteredItems = new();

        [ObservableProperty]
        private ObservableCollection<int> _selectedIds = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _statusFilter = string.Empty;

        [ObservableProperty]
        private string _priorityFilter = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private TodoItem? _editingItem;

        [ObservableProperty]
        private bool _isDialogOpen;

        [ObservableProperty]
        private string _editingItemTitle = string.Empty;

        [ObservableProperty]
        private string _editingItemDescription = string.Empty;

        [ObservableProperty]
        private string _editingItemStatus = "未着手";

        [ObservableProperty]
        private string _editingItemPriority = "中";

        [ObservableProperty]
        private DateTimeOffset? _editingItemDueDate;

        public string DialogTitle => EditingItem == null ? "新しいアイテムを追加" : "アイテムを編集";

        public bool HasActiveFilters => !string.IsNullOrWhiteSpace(SearchText) || 
                                        !string.IsNullOrWhiteSpace(StatusFilter) || 
                                        !string.IsNullOrWhiteSpace(PriorityFilter);

        public bool CanOpenInNewWindow => SelectedIds.Count == 1;

        public MainWindowViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            SelectedIds.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(CanOpenInNewWindow));
                OnPropertyChanged(nameof(CanCopySelected));
                CopySelectedCommand.NotifyCanExecuteChanged();
            };
            _ = LoadDataAsync();
        }

        public bool CanCopySelected => SelectedIds.Count > 0;

        public async Task LoadDataAsync()
        {
            IsLoading = true;
            try
            {
                var data = await _dataService.LoadDataAsync();
                Items.Clear();
                foreach (var item in data.Items)
                {
                    Items.Add(item);
                }
                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveDataAsync()
        {
            await SaveDataInternalAsync(setLoading: true, notify: true);
        }

        private async Task SaveDataInternalAsync(bool setLoading = true, bool notify = false)
        {
            System.Diagnostics.Debug.WriteLine($"[ViewModel] SaveDataInternalAsync called. Items count: {Items.Count}, setLoading: {setLoading}");
            
            if (setLoading && _dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() => IsLoading = true);
            }
            
            try
            {
                var data = new ProjectData { Items = Items.ToList() };
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Creating ProjectData with {data.Items.Count} items");
                await _dataService.SaveDataAsync(data);
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Data saved successfully. Items count: {data.Items.Count}");
                if (notify)
                {
                    NotificationService.Show("Todo App", "保存しました");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Error saving data: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                if (setLoading && _dispatcherQueue != null)
                {
                    _dispatcherQueue.TryEnqueue(() => IsLoading = false);
                }
            }
        }

        [RelayCommand]
        private async Task ExportDataAsync()
        {
            try
            {
                var data = new ProjectData { Items = Items.ToList() };
                await _dataService.ExportDataAsync(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting data: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task ImportDataAsync()
        {
            try
            {
                var data = await _dataService.ImportDataAsync();
                if (data != null)
                {
                    await ApplyImportedDataAsync(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing data: {ex.Message}");
            }
        }

        public async Task ImportFromPathAsync(string path)
        {
            try
            {
                var data = await _dataService.ImportFromPathAsync(path);
                if (data != null)
                {
                    await ApplyImportedDataAsync(data);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing from path: {ex.Message}");
            }
        }

        private async Task ApplyImportedDataAsync(ProjectData data)
        {
            Items.Clear();
            foreach (var item in data.Items)
            {
                Items.Add(item);
            }
            SelectedIds.Clear();
            ApplyFilters();
            await SaveDataInternalAsync(setLoading: true, notify: false);
            NotificationService.Show("Todo App", "インポートしました");
        }

        [RelayCommand(CanExecute = nameof(CanCopySelected))]
        private void CopySelected()
        {
            var selected = Items.Where(item => SelectedIds.Contains(item.Id)).ToList();
            if (selected.Count == 0)
            {
                return;
            }

            ClipboardService.CopyTodoItems(selected);
        }

        public void NotifyItemUpdated(TodoItem item)
        {
            var index = Items.IndexOf(item);
            if (index >= 0)
            {
                Items.RemoveAt(index);
                Items.Insert(index, item);
            }

            ApplyFilters();
            TriggerAutoSave();
        }

        [RelayCommand]
        private async Task OpenDataFolderAsync()
        {
            try
            {
                await _dataService.OpenDataFolderAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening data folder: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddItem()
        {
            EditingItem = null;
            EditingItemTitle = string.Empty;
            EditingItemDescription = string.Empty;
            EditingItemStatus = "未着手";
            EditingItemPriority = "中";
            EditingItemDueDate = null;
            IsDialogOpen = true;
        }

        [RelayCommand]
        private void EditItem(TodoItem item)
        {
            EditingItem = item;
            EditingItemTitle = item.Title;
            EditingItemDescription = item.Description;
            EditingItemStatus = item.Status;
            EditingItemPriority = item.Priority;
            EditingItemDueDate = item.DueDate;
            IsDialogOpen = true;
        }

        [RelayCommand]
        private async Task DeleteSelectedItemsAsync()
        {
            if (SelectedIds.Count == 0) return;

            // 確認ダイアログはUI層で処理
            var idsToDelete = SelectedIds.ToList();
            foreach (var id in idsToDelete)
            {
                var item = Items.FirstOrDefault(i => i.Id == id);
                if (item != null)
                {
                    Items.Remove(item);
                }
            }
            SelectedIds.Clear();
            ApplyFilters();
            TriggerAutoSave();
        }

        [RelayCommand]
        private void ToggleSelection(int id)
        {
            if (SelectedIds.Contains(id))
            {
                SelectedIds.Remove(id);
            }
            else
            {
                SelectedIds.Add(id);
            }
        }

        [RelayCommand]
        private void SelectAll()
        {
            SelectedIds.Clear();
            foreach (var item in _filteredSource)
            {
                SelectedIds.Add(item.Id);
            }
        }

        [RelayCommand]
        private void DeselectAll()
        {
            SelectedIds.Clear();
        }

        [RelayCommand]
        private void SaveItem()
        {
            System.Diagnostics.Debug.WriteLine($"[ViewModel] SaveItem called. Title: {EditingItemTitle}, IsEdit: {EditingItem != null}");
            
            if (string.IsNullOrWhiteSpace(EditingItemTitle))
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] SaveItem: Validation failed - title is empty");
                return; // バリデーションエラー
            }

            if (EditingItem != null)
            {
                // 更新 - コレクション内のアイテムを更新
                var index = Items.IndexOf(EditingItem);
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Updating item at index: {index}");
                if (index >= 0)
                {
                    EditingItem.Title = EditingItemTitle;
                    EditingItem.Description = EditingItemDescription ?? string.Empty;
                    EditingItem.Status = EditingItemStatus;
                    EditingItem.Priority = EditingItemPriority;
                    EditingItem.DueDate = EditingItemDueDate;
                    EditingItem.UpdatedAt = DateTimeOffset.Now;
                    
                    // コレクションの変更通知を発火させるために、一度削除して再追加
                    Items.RemoveAt(index);
                    Items.Insert(index, EditingItem);
                    System.Diagnostics.Debug.WriteLine($"[ViewModel] Item updated. Items count: {Items.Count}");
                }
            }
            else
            {
                // 新規追加
                var maxId = Items.Count > 0 ? Items.Max(i => i.Id) : 0;
                var newItem = new TodoItem
                {
                    Id = maxId + 1,
                    Title = EditingItemTitle,
                    Description = EditingItemDescription ?? string.Empty,
                    Status = EditingItemStatus,
                    Priority = EditingItemPriority,
                    DueDate = EditingItemDueDate,
                    CreatedAt = DateTimeOffset.Now,
                    UpdatedAt = DateTimeOffset.Now,
                    IsCompleted = false
                };
                Items.Add(newItem);
                System.Diagnostics.Debug.WriteLine($"[ViewModel] New item added. ID: {newItem.Id}, Items count: {Items.Count}");
            }

            IsDialogOpen = false;
            EditingItem = null;
            ApplyFilters();
            OnPropertyChanged(nameof(FilteredItems));
            System.Diagnostics.Debug.WriteLine($"[ViewModel] SaveItem completed. Triggering auto-save. Items count: {Items.Count}, FilteredItems count: {FilteredItems.Count}");
            TriggerAutoSave();
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsDialogOpen = false;
            EditingItem = null;
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
            OnPropertyChanged(nameof(HasActiveFilters));
        }

        partial void OnStatusFilterChanged(string value)
        {
            ApplyFilters();
            OnPropertyChanged(nameof(HasActiveFilters));
        }

        partial void OnPriorityFilterChanged(string value)
        {
            ApplyFilters();
            OnPropertyChanged(nameof(HasActiveFilters));
        }

        private void ApplyFilters()
        {
            if (_suspendAutoFilter)
            {
                return;
            }

            _ = ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            var generation = ++_filterGeneration;
            var searchText = SearchText;
            var statusFilter = StatusFilter;
            var priorityFilter = PriorityFilter;
            var itemsSnapshot = Items.ToList();

            List<TodoItem> result;
            try
            {
                result = await Task.Run(() =>
                    ComputeFiltered(itemsSnapshot, searchText, statusFilter, priorityFilter)).ConfigureAwait(false);
            }
            catch
            {
                return;
            }

            void ApplyOnUi()
            {
                if (generation != _filterGeneration)
                {
                    return;
                }

                _filteredSource = result;
                _visibleCount = PageSize;
                RefreshVisibleItems();
            }

            if (_dispatcherQueue.HasThreadAccess)
            {
                ApplyOnUi();
            }
            else
            {
                _dispatcherQueue.TryEnqueue(ApplyOnUi);
            }
        }

        private static List<TodoItem> ComputeFiltered(
            List<TodoItem> items,
            string searchText,
            string statusFilter,
            string priorityFilter)
        {
            IEnumerable<TodoItem> filtered = items;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var searchLower = searchText.ToLower();
                filtered = filtered.Where(item =>
                    item.Title.ToLower().Contains(searchLower) ||
                    item.Description.ToLower().Contains(searchLower));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                filtered = filtered.Where(item => item.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(priorityFilter))
            {
                filtered = filtered.Where(item => item.Priority == priorityFilter);
            }

            return filtered.ToList();
        }

        private void RefreshVisibleItems()
        {
            var count = Math.Min(_visibleCount, _filteredSource.Count);
            var visible = new ObservableCollection<TodoItem>();
            for (var i = 0; i < count; i++)
            {
                visible.Add(_filteredSource[i]);
            }

            FilteredItems = visible;
            OnPropertyChanged(nameof(FilteredItems));
            OnPropertyChanged(nameof(AreAllFilteredSelected));
            OnPropertyChanged(nameof(AreSomeFilteredSelected));
        }

        public void CpuBenchAddOne(int n)
        {
            var maxId = Items.Count > 0 ? Items.Max(i => i.Id) : 0;
            var now = DateTimeOffset.Now;
            Items.Add(new TodoItem
            {
                Id = maxId + 1,
                Title = $"bench-{n}",
                Description = string.Empty,
                Status = "未着手",
                Priority = "中",
                DueDate = null,
                CreatedAt = now,
                UpdatedAt = now,
                IsCompleted = false
            });
            ApplyFiltersSync();
            OnPropertyChanged(nameof(FilteredItems));
        }

        public void CpuBenchToggleFilters(bool active)
        {
            _suspendAutoFilter = true;
#pragma warning disable MVVMTK0034
            _searchText = active ? "a" : string.Empty;
            _statusFilter = active ? "進行中" : string.Empty;
#pragma warning restore MVVMTK0034
            _suspendAutoFilter = false;
            ApplyFiltersSync();
            OnPropertyChanged(nameof(FilteredItems));
            OnPropertyChanged(nameof(HasActiveFilters));
        }

        public void ResetVisibleForBench()
        {
            _visibleCount = PageSize;
            RefreshVisibleItems();
        }

        public bool LoadMoreVisible()
        {
            if (_loadingMore || _visibleCount >= _filteredSource.Count)
            {
                return false;
            }

            _loadingMore = true;
            try
            {
                var previous = _visibleCount;
                _visibleCount = Math.Min(_visibleCount + PageSize, _filteredSource.Count);
                for (var i = previous; i < _visibleCount; i++)
                {
                    FilteredItems.Add(_filteredSource[i]);
                }

                OnPropertyChanged(nameof(AreAllFilteredSelected));
                OnPropertyChanged(nameof(AreSomeFilteredSelected));
                return true;
            }
            finally
            {
                _loadingMore = false;
            }
        }

        private void ApplyFiltersSync()
        {
            _filterGeneration++;
            _filteredSource = ComputeFiltered(
                Items.ToList(),
                SearchText,
                StatusFilter,
                PriorityFilter);
            _visibleCount = PageSize;
            RefreshVisibleItems();
        }

        private void TriggerAutoSave()
        {
            System.Diagnostics.Debug.WriteLine($"[ViewModel] TriggerAutoSave called. Items count: {Items.Count}");
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = new System.Threading.Timer(_ =>
            {
                System.Diagnostics.Debug.WriteLine($"[ViewModel] Auto-save timer fired. Items count: {Items.Count}");
                // 自動保存時はIsLoadingを設定しない（バックグラウンドで実行されるため）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ViewModel] Starting auto-save task");
                        await SaveDataInternalAsync(setLoading: false, notify: false);
                        System.Diagnostics.Debug.WriteLine($"[ViewModel] Auto-save task completed");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ViewModel] Error in auto-save: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"[ViewModel] Stack trace: {ex.StackTrace}");
                    }
                });
            }, null, TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
        }

        public void RegisterDetailWindow(Window window)
        {
            _detailWindows.Add(window);
            window.Closed += (_, _) => _detailWindows.Remove(window);
        }

        public void Cleanup()
        {
            if (_cleanedUp)
            {
                return;
            }

            _cleanedUp = true;
            _autoSaveTimer?.Dispose();
            _autoSaveTimer = null;

            foreach (var window in _detailWindows.ToList())
            {
                window.Close();
            }
            _detailWindows.Clear();
            _filteredSource = new List<TodoItem>();
            FilteredItems = new ObservableCollection<TodoItem>();
        }

        public bool AreAllFilteredSelected =>
            _filteredSource.Count > 0 && _filteredSource.All(item => SelectedIds.Contains(item.Id));

        public bool AreSomeFilteredSelected => _filteredSource.Any(item => SelectedIds.Contains(item.Id));

        public bool IsItemSelected(TodoItem item) => SelectedIds.Contains(item.Id);
    }
}

