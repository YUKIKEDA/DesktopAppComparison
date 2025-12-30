using System;
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
        private readonly IDataService _dataService;
        private readonly DispatcherQueue _dispatcherQueue;
        private System.Threading.Timer? _autoSaveTimer;

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

        public MainWindowViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _ = LoadDataAsync();
        }

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
            await SaveDataInternalAsync();
        }

        private async Task SaveDataInternalAsync(bool setLoading = true)
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
                    Items.Clear();
                    foreach (var item in data.Items)
                    {
                        Items.Add(item);
                    }
                    ApplyFilters();
                    await SaveDataAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error importing data: {ex.Message}");
            }
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
            foreach (var item in FilteredItems)
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
            System.Diagnostics.Debug.WriteLine($"[ViewModel] ApplyFilters called. Items count: {Items.Count}");
            var filtered = Items.AsEnumerable();

            // テキスト検索（タイトルと説明）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                filtered = filtered.Where(item =>
                    item.Title.ToLower().Contains(searchLower) ||
                    item.Description.ToLower().Contains(searchLower));
            }

            // ステータスフィルタ
            if (!string.IsNullOrWhiteSpace(StatusFilter))
            {
                filtered = filtered.Where(item => item.Status == StatusFilter);
            }

            // 優先度フィルタ
            if (!string.IsNullOrWhiteSpace(PriorityFilter))
            {
                filtered = filtered.Where(item => item.Priority == PriorityFilter);
            }

            var filteredList = filtered.ToList();
            System.Diagnostics.Debug.WriteLine($"[ViewModel] Filtered items count: {filteredList.Count}");
            
            // FilteredItemsを完全に新しいコレクションに置き換える
            FilteredItems = new ObservableCollection<TodoItem>(filteredList);
            
            System.Diagnostics.Debug.WriteLine($"[ViewModel] FilteredItems updated. Count: {FilteredItems.Count}");
            OnPropertyChanged(nameof(FilteredItems));
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
                        await SaveDataInternalAsync(setLoading: false);
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

        public bool AreAllFilteredSelected => FilteredItems.Count > 0 && FilteredItems.All(item => SelectedIds.Contains(item.Id));

        public bool AreSomeFilteredSelected => FilteredItems.Any(item => SelectedIds.Contains(item.Id));

        public bool IsItemSelected(TodoItem item) => SelectedIds.Contains(item.Id);
    }
}

