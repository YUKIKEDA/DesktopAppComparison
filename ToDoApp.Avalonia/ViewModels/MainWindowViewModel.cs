using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ToDoApp.Avalonia.Models;
using ToDoApp.Avalonia.Services;

namespace ToDoApp.Avalonia.ViewModels;

    public partial class MainWindowViewModel : ViewModelBase
    {
        private const int PageSize = 100;

        private readonly IDataService _dataService;
        private readonly ThemeService _themeService;
        private System.Timers.Timer? _autoSaveTimer;
        private Window? _window;
        private List<TodoItem> _filteredSource = [];
        private int _visibleCount = PageSize;
        private int _filterGeneration;
        private bool _loadingMore;
        private readonly List<Window> _detailWindows = [];
        private bool _cleanedUp;
        private bool _suspendAutoFilter;

    [ObservableProperty]
    private ObservableCollection<TodoItem> _items = [];

    [ObservableProperty]
    private ObservableCollection<TodoItem> _filteredItems = [];

    [ObservableProperty]
    private HashSet<int> _selectedIds = [];

    public int SelectedCount => SelectedIds.Count;

    public bool CanOpenInNewWindow => SelectedCount == 1;

    private void NotifySelectedCountChanged()
    {
        // HashSetの内容変更を検知させるため、新しいインスタンスを作成
        var newSet = new HashSet<int>(SelectedIds);
        SelectedIds = newSet;
        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanOpenInNewWindow));
        OpenInNewWindowCommand.NotifyCanExecuteChanged();
        CopySelectedCommand.NotifyCanExecuteChanged();
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

    [ObservableProperty]
    private double _dialogOpacity;

    [ObservableProperty]
    private bool _isDarkTheme;

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

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "未着手", "進行中", "完了" };

    public IReadOnlyList<string> PriorityOptions { get; } = new[] { "低", "中", "高" };

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

    public MainWindowViewModel(IDataService dataService, Window? window = null)
    {
        _dataService = dataService;
        _themeService = new ThemeService(dataService.DataDirectory);
        _window = window;
        // FilteredItemsは[ObservableProperty]で自動的に初期化される
        // 明示的に初期化して確実に空のコレクションを設定
        FilteredItems = new ObservableCollection<TodoItem>();

        var theme = _themeService.LoadTheme();
        _themeService.ApplyTheme(theme);
        IsDarkTheme = theme == "dark";

        // 自動保存タイマーの設定（2秒のデバウンス）
        _autoSaveTimer = new System.Timers.Timer(2000);
        _autoSaveTimer.Elapsed += async (s, e) =>
        {
            _autoSaveTimer.Stop();
            if (Items.Count > 0)
            {
                await SaveDataCoreAsync(notify: false);
            }
        };
        _autoSaveTimer.AutoReset = false;

        // プロパティ変更時にフィルタを適用
        PropertyChanged += OnPropertyChanged;

        // 初期データ読み込み
        LoadDataCommand.Execute(null);
    }

    partial void OnIsDialogOpenChanged(bool value)
    {
        if (value)
        {
            DialogOpacity = 0;
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => DialogOpacity = 1);
        }
        else
        {
            DialogOpacity = 0;
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        var theme = _themeService.ToggleTheme();
        IsDarkTheme = theme == "dark";
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedStatusChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedPriorityChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnItemsChanged(ObservableCollection<TodoItem> value)
    {
        ApplyFilters();
        // 自動保存タイマーをリセット
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    partial void OnFilteredItemsChanged(ObservableCollection<TodoItem> value)
    {
        UpdateAllFilteredSelected();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FilteredItems))
        {
            UpdateAllFilteredSelected();
        }
    }

    private void UpdateAllFilteredSelected()
    {
        if (_filteredSource.Count == 0)
        {
            _allFilteredSelected = false;
            OnPropertyChanged(nameof(AllFilteredSelected));
            return;
        }

        var allSelected = _filteredSource.All(item => SelectedIds.Contains(item.Id));

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
        catch (Exception)
        {
            // エラーはDataServiceで表示される
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveDataAsync()
    {
        await SaveDataCoreAsync(notify: true);
    }

    private async Task SaveDataCoreAsync(bool notify)
    {
        try
        {
            var snapshot = Items.ToList();
            await Task.Run(async () =>
            {
                await _dataService.SaveDataAsync(new ProjectData { Items = snapshot });
            }).ConfigureAwait(true);
            if (notify)
            {
                WindowsBalloonNotification.Show("Todo App", "保存しました");
            }
        }
        catch (Exception)
        {
            // エラーはDataServiceで表示される
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
    private async Task SaveItem()
    {
        if (EditingItem == null) return;

        if (string.IsNullOrWhiteSpace(EditingItem.Title))
        {
            if (_window != null)
            {
                await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "タイトルは必須です。");
            }
            return;
        }

        if (EditingItem.Title.Length > 200)
        {
            if (_window != null)
            {
                await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "タイトルは200文字以内です。");
            }
            return;
        }

        if (!string.IsNullOrEmpty(EditingItem.Description) && EditingItem.Description.Length > 500)
        {
            if (_window != null)
            {
                await Views.MessageDialog.ShowAsync(_window, "バリデーションエラー", "説明は500文字以内です。");
            }
            return;
        }

        if (EditingItem.Id > 0)
        {
            // 更新: 既存のアイテムを削除して新しいアイテムで置き換える
            var existingItem = Items.FirstOrDefault(i => i.Id == EditingItem.Id);
            if (existingItem != null)
            {
                var index = Items.IndexOf(existingItem);
                Items.RemoveAt(index);
                
                var updatedItem = new TodoItem
                {
                    Id = EditingItem.Id,
                    Title = EditingItem.Title,
                    Description = EditingItem.Description,
                    Status = EditingItem.Status,
                    Priority = EditingItem.Priority,
                    DueDate = EditingItem.DueDate,
                    CreatedAt = existingItem.CreatedAt,
                    UpdatedAt = DateTime.Now,
                    IsCompleted = EditingItem.IsCompleted
                };
                Items.Insert(index, updatedItem);
            }
        }
        else
        {
            // 新規追加
            var maxId = Items.Count > 0 ? Items.Max(i => i.Id) : 0;
            var newItem = new TodoItem
            {
                Id = maxId + 1,
                Title = EditingItem.Title,
                Description = EditingItem.Description ?? string.Empty,
                Status = EditingItem.Status,
                Priority = EditingItem.Priority,
                DueDate = EditingItem.DueDate,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsCompleted = EditingItem.IsCompleted
            };
            Items.Add(newItem);
        }

        IsDialogOpen = false;
        EditingItem = null;
        
        // フィルタを再適用して表示を更新
        ApplyFilters();
        
        // データを明示的に保存
        await SaveDataCoreAsync(notify: false);
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

        if (_window != null)
        {
            var result = await Views.MessageDialog.ShowAsync(
                _window,
                "確認",
                $"{SelectedIds.Count}件のアイテムを削除しますか？",
                "削除",
                "キャンセル",
                true);
            if (!result)
            {
                return;
            }
        }

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
        await SaveDataCoreAsync(notify: false);
    }

    private bool CanCopySelected() => SelectedIds.Count > 0;

    [RelayCommand(CanExecute = nameof(CanCopySelected))]
    private async Task CopySelectedAsync()
    {
        var selected = Items.Where(item => SelectedIds.Contains(item.Id)).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        await ClipboardService.CopyTodoItemsAsync(_window, selected);
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
        foreach (var item in _filteredSource)
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
        foreach (var item in _filteredSource)
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
        catch (Exception)
        {
            // エラーはDataServiceで表示される
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
        catch (Exception)
        {
            // エラーはDataServiceで表示される
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
        catch (Exception)
        {
            // エラーはDataServiceで表示される
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
        NotifySelectedCountChanged();
        ApplyFilters();
        await SaveDataCoreAsync(notify: false);
        WindowsBalloonNotification.Show("Todo App", "インポートしました");
    }

    public async Task ApplyItemUpdateAsync(TodoItem editingItem)
    {
        var existingItem = Items.FirstOrDefault(i => i.Id == editingItem.Id);
        if (existingItem == null)
        {
            return;
        }

        var index = Items.IndexOf(existingItem);
        Items.RemoveAt(index);

        var updatedItem = new TodoItem
        {
            Id = editingItem.Id,
            Title = editingItem.Title,
            Description = editingItem.Description ?? string.Empty,
            Status = editingItem.Status,
            Priority = editingItem.Priority,
            DueDate = editingItem.DueDate,
            CreatedAt = existingItem.CreatedAt,
            UpdatedAt = DateTime.Now,
            IsCompleted = editingItem.IsCompleted
        };
        Items.Insert(index, updatedItem);
        ApplyFilters();
        await SaveDataCoreAsync(notify: false);
    }

    [RelayCommand(CanExecute = nameof(CanOpenInNewWindow))]
    private void OpenInNewWindow()
    {
        if (SelectedIds.Count != 1)
        {
            return;
        }

        var id = SelectedIds.First();
        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item == null)
        {
            return;
        }

        var detailWindow = new Views.ItemDetailWindow(this, item);
        _detailWindows.Add(detailWindow);
        detailWindow.Closed += (_, _) =>
        {
            _detailWindows.Remove(detailWindow);
            detailWindow.DataContext = null;
        };
        if (_window != null)
        {
            detailWindow.Show(_window);
        }
        else
        {
            detailWindow.Show();
        }
    }

    [RelayCommand]
    private async Task OpenDataFolderAsync()
    {
        try
        {
            await _dataService.OpenDataFolderAsync();
        }
        catch (Exception)
        {
            // エラーはDataServiceで表示される
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
        if (_suspendAutoFilter)
        {
            return;
        }

        _ = ApplyFiltersAsync();
    }

    private void ApplySorting()
    {
        _ = ApplyFiltersAsync();
    }

    private async Task ApplyFiltersAsync()
    {
        var generation = ++_filterGeneration;
        var searchText = SearchText;
        var selectedStatus = SelectedStatus;
        var selectedPriority = SelectedPriority;
        var sortColumn = _sortColumn;
        var sortDirection = SortDirection;
        var itemsSnapshot = Items.ToList();

        List<TodoItem> result;
        try
        {
            result = await Task.Run(() =>
                ComputeFilteredSorted(
                    itemsSnapshot,
                    searchText,
                    selectedStatus,
                    selectedPriority,
                    sortColumn,
                    sortDirection)).ConfigureAwait(false);
        }
        catch
        {
            return;
        }

        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (generation != _filterGeneration)
            {
                return;
            }

            _filteredSource = result;
            _visibleCount = PageSize;
            RefreshVisibleItems();
            UpdateAllFilteredSelected();
        });
    }

    private static List<TodoItem> ComputeFilteredSorted(
        List<TodoItem> items,
        string searchText,
        string selectedStatus,
        string selectedPriority,
        string sortColumn,
        ListSortDirection sortDirection)
    {
        IEnumerable<TodoItem> filtered = items;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(item =>
                item.Title.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                item.Description.Contains(searchText, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrEmpty(selectedStatus))
        {
            filtered = filtered.Where(item => item.Status == selectedStatus);
        }

        if (!string.IsNullOrEmpty(selectedPriority))
        {
            filtered = filtered.Where(item => item.Priority == selectedPriority);
        }

        if (string.IsNullOrEmpty(sortColumn))
        {
            return filtered.OrderBy(item => item.Id).ToList();
        }

        return sortDirection == ListSortDirection.Ascending
            ? filtered.OrderBy(item => GetPropertyValue(item, sortColumn)).ToList()
            : filtered.OrderByDescending(item => GetPropertyValue(item, sortColumn)).ToList();
    }

    private void RefreshVisibleItems()
    {
        FilteredItems.Clear();
        var count = Math.Min(_visibleCount, _filteredSource.Count);
        for (var i = 0; i < count; i++)
        {
            FilteredItems.Add(_filteredSource[i]);
        }
    }

    private void ApplyFiltersSync()
    {
        _filterGeneration++;
        _filteredSource = ComputeFilteredSorted(
            Items.ToList(),
            SearchText,
            SelectedStatus,
            SelectedPriority,
            _sortColumn,
            SortDirection);
        _visibleCount = PageSize;
        RefreshVisibleItems();
        UpdateAllFilteredSelected();
    }

    public void CpuBenchAddOne(int n)
    {
        var maxId = Items.Count > 0 ? Items.Max(i => i.Id) : 0;
        var now = DateTime.Now;
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
    }

    public void CpuBenchToggleFilters(bool active)
    {
        _suspendAutoFilter = true;
#pragma warning disable MVVMTK0034
        _searchText = active ? "a" : string.Empty;
        _selectedStatus = active ? "進行中" : string.Empty;
#pragma warning restore MVVMTK0034
        _suspendAutoFilter = false;
        ApplyFiltersSync();
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

            return true;
        }
        finally
        {
            _loadingMore = false;
        }
    }

    private static object? GetPropertyValue(TodoItem item, string propertyName)
    {
        return propertyName switch
        {
            "Id" => item.Id,
            "Title" => item.Title,
            "Description" => item.Description,
            "Status" => item.Status,
            "Priority" => item.Priority,
            "DueDate" => item.DueDate,
            "CreatedAt" => item.CreatedAt,
            "UpdatedAt" => item.UpdatedAt,
            _ => null
        };
    }

    public void Cleanup()
    {
        if (_cleanedUp)
        {
            return;
        }

        _cleanedUp = true;
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = null;

        foreach (var window in _detailWindows.ToList())
        {
            window.Close();
        }
        _detailWindows.Clear();
        _filteredSource = [];
        FilteredItems.Clear();
    }

    public void HandleKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Delete && SelectedIds.Count > 0)
        {
            DeleteSelectedItemsCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.KeyModifiers == KeyModifiers.Control)
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
            else if (e.Key == Key.C)
            {
                if (SelectedIds.Count > 0)
                {
                    CopySelectedCommand.Execute(null);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F)
            {
                // 検索ボックスにフォーカスを移動（Viewで処理）
                e.Handled = true;
            }
        }
    }

    public void SetWindow(Window window)
    {
        _window = window;
    }
}
