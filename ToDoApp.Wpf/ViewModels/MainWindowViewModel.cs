using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoApp.Wpf.Models;
using ToDoApp.Wpf.Services;

namespace ToDoApp.Wpf.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IDataService _dataService;
        private readonly System.Timers.Timer? _autoSaveTimer;

        [ObservableProperty]
        private ObservableCollection<TodoItem> _items = [];

        [ObservableProperty]
        private ObservableCollection<TodoItem> _filteredItems = [];

        [ObservableProperty]
        private HashSet<int> _selectedIds = [];

        public int SelectedCount => SelectedIds.Count;

        private void NotifySelectedCountChanged()
        {
            // HashSetの内容変更を検知させるため、新しいインスタンスを作成
            var newSet = new HashSet<int>(SelectedIds);
            SelectedIds = newSet;
            OnPropertyChanged(nameof(SelectedCount));
            UpdateAllFilteredSelected();
        }

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedStatus = string.Empty;

        [ObservableProperty]
        private string _selectedPriority = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private TodoItem? _editingItem;

        [ObservableProperty]
        private bool _isDialogOpen;

        private string _sortColumn = string.Empty;

        public string SortColumn
        {
            get => _sortColumn;
            set => SetProperty(ref _sortColumn, value);
        }

        [ObservableProperty]
        private ListSortDirection _sortDirection = ListSortDirection.Ascending;

        [ObservableProperty]
        private TodoItem? _selectedItem;

        private readonly ICollectionView? _itemsView;

        private bool _allFilteredSelected;

        public bool AllFilteredSelected
        {
            get => _allFilteredSelected;
            set
            {
                if (SetProperty(ref _allFilteredSelected, value))
                {
                    if (value)
                    {
                        SelectAll();
                    }
                    else
                    {
                        DeselectAll();
                    }
                }
            }
        }

        public MainWindowViewModel(IDataService dataService)
        {
            _dataService = dataService;
            _filteredItems = new ObservableCollection<TodoItem>();
            _itemsView = CollectionViewSource.GetDefaultView(_filteredItems);
            _itemsView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            // 自動保存タイマーの設定（2秒のデバウンス）
            _autoSaveTimer = new System.Timers.Timer(2000);
            _autoSaveTimer.Elapsed += async (s, e) =>
            {
                _autoSaveTimer.Stop();
                if (Items.Count > 0)
                {
                    await SaveDataAsync();
                }
            };
            _autoSaveTimer.AutoReset = false;

            // プロパティ変更時にフィルタを適用
            PropertyChanged += OnPropertyChanged;

            // 初期データ読み込み
            LoadDataCommand.Execute(null);
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Items) || 
                e.PropertyName == nameof(SearchText) || 
                e.PropertyName == nameof(SelectedStatus) || 
                e.PropertyName == nameof(SelectedPriority))
            {
                ApplyFilters();
            }

            if (e.PropertyName == nameof(Items))
            {
                // 自動保存タイマーをリセット
                _autoSaveTimer?.Stop();
                _autoSaveTimer?.Start();
            }

            if (e.PropertyName == nameof(FilteredItems))
            {
                UpdateAllFilteredSelected();
            }
        }

        private void UpdateAllFilteredSelected()
        {
            if (FilteredItems.Count == 0)
            {
                _allFilteredSelected = false;
                OnPropertyChanged(nameof(AllFilteredSelected));
                return;
            }

            var allSelected = FilteredItems.All(item => SelectedIds.Contains(item.Id));
            var someSelected = FilteredItems.Any(item => SelectedIds.Contains(item.Id));

            if (allSelected != _allFilteredSelected)
            {
                _allFilteredSelected = allSelected;
                OnPropertyChanged(nameof(AllFilteredSelected));
            }
        }

        [RelayCommand]
        private async Task LoadDataAsync()
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
                MessageBox.Show($"データの読み込みに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SaveDataAsync()
        {
            try
            {
                var data = new ProjectData { Items = Items.ToList() };
                await _dataService.SaveDataAsync(data);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"データの保存に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void AddItem()
        {
            EditingItem = new TodoItem
            {
                Status = "未着手",
                Priority = "中"
            };
            IsDialogOpen = true;
        }

        [RelayCommand]
        private void EditItem(TodoItem? item)
        {
            if (item != null)
            {
                EditingItem = new TodoItem
                {
                    Id = item.Id,
                    Title = item.Title,
                    Description = item.Description,
                    Status = item.Status,
                    Priority = item.Priority,
                    DueDate = item.DueDate,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,
                    IsCompleted = item.IsCompleted
                };
                IsDialogOpen = true;
            }
        }

        [RelayCommand]
        private void SaveItem(TodoItem? item)
        {
            if (item == null) return;

            if (string.IsNullOrWhiteSpace(item.Title))
            {
                MessageBox.Show("タイトルは必須です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (item.Title.Length > 200)
            {
                MessageBox.Show("タイトルは200文字以内です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(item.Description) && item.Description.Length > 500)
            {
                MessageBox.Show("説明は500文字以内です。", "バリデーションエラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (EditingItem != null && EditingItem.Id > 0)
            {
                // 更新
                var existingItem = Items.FirstOrDefault(i => i.Id == EditingItem.Id);
                if (existingItem != null)
                {
                    existingItem.Title = item.Title;
                    existingItem.Description = item.Description;
                    existingItem.Status = item.Status;
                    existingItem.Priority = item.Priority;
                    existingItem.DueDate = item.DueDate;
                    existingItem.UpdatedAt = DateTime.Now;
                    existingItem.IsCompleted = item.IsCompleted;
                }
            }
            else
            {
                // 新規追加
                var maxId = Items.Count > 0 ? Items.Max(i => i.Id) : 0;
                var newItem = new TodoItem
                {
                    Id = maxId + 1,
                    Title = item.Title,
                    Description = item.Description ?? string.Empty,
                    Status = item.Status,
                    Priority = item.Priority,
                    DueDate = item.DueDate,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsCompleted = item.IsCompleted
                };
                Items.Add(newItem);
            }

            IsDialogOpen = false;
            EditingItem = null;
            ApplyFilters();
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsDialogOpen = false;
            EditingItem = null;
        }

        [RelayCommand]
        private async Task DeleteSelectedItemsAsync()
        {
            if (SelectedIds.Count == 0) return;

            var result = MessageBox.Show(
                $"{SelectedIds.Count}件のアイテムを削除しますか？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
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
                NotifySelectedCountChanged();
                ApplyFilters();
                await SaveDataAsync();
            }
        }

        [RelayCommand]
        private void ToggleSelection(int id)
        {
            if (!SelectedIds.Remove(id))
            {
                SelectedIds.Add(id);
            }
            NotifySelectedCountChanged();
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var item in FilteredItems)
            {
                if (!SelectedIds.Contains(item.Id))
                {
                    SelectedIds.Add(item.Id);
                }
            }
            NotifySelectedCountChanged();
        }

        [RelayCommand]
        private void DeselectAll()
        {
            // フィルタリングされたアイテムだけを解除
            foreach (var item in FilteredItems)
            {
                SelectedIds.Remove(item.Id);
            }
            NotifySelectedCountChanged();
        }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedStatus = string.Empty;
            SelectedPriority = string.Empty;
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
                MessageBox.Show($"エクスポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"インポートに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"フォルダを開くのに失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void SortColumnCommand(string columnName)
        {
            if (string.IsNullOrEmpty(columnName)) return;

            if (_sortColumn == columnName)
            {
                // 同じ列をクリックした場合はソート方向を切り替え
                SortDirection = SortDirection == ListSortDirection.Ascending 
                    ? ListSortDirection.Descending 
                    : ListSortDirection.Ascending;
            }
            else
            {
                _sortColumn = columnName;
                SortColumn = columnName;
                SortDirection = ListSortDirection.Ascending;
            }

            ApplySorting();
        }

        private void ApplyFilters()
        {
            FilteredItems.Clear();

            var filtered = Items.AsEnumerable();

            // テキスト検索フィルタ（タイトルと説明の両方を検索）
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLower();
                filtered = filtered.Where(item =>
                    item.Title.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    item.Description.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase)
                );
            }

            // ステータスフィルタ
            if (!string.IsNullOrEmpty(SelectedStatus))
            {
                filtered = filtered.Where(item => item.Status == SelectedStatus);
            }

            // 優先度フィルタ
            if (!string.IsNullOrEmpty(SelectedPriority))
            {
                filtered = filtered.Where(item => item.Priority == SelectedPriority);
            }

            foreach (var item in filtered)
            {
                FilteredItems.Add(item);
            }

            ApplySorting();
            UpdateAllFilteredSelected();
        }

        private void ApplySorting()
        {
            if (_itemsView == null) return;

            _itemsView.SortDescriptions.Clear();

            if (!string.IsNullOrEmpty(_sortColumn))
            {
                _itemsView.SortDescriptions.Add(new SortDescription(_sortColumn, SortDirection));
            }
            else
            {
                _itemsView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            }
        }

        public void HandleKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete && SelectedIds.Count > 0)
            {
                DeleteSelectedItemsCommand.Execute(null);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.N)
                {
                    AddItemCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.S)
                {
                    SaveDataCommand.Execute(null);
                    e.Handled = true;
                }
                else if (e.Key == Key.F)
                {
                    // 検索ボックスにフォーカスを移動（Viewで処理）
                    e.Handled = true;
                }
            }
        }
    }
}
