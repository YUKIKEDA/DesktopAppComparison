using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ToDoApp.WinUI.Models;
using ToDoApp.WinUI.Services;
using ToDoApp.WinUI.ViewModels;

namespace ToDoApp.WinUI.Views
{
    public sealed partial class MainPage : UserControl
    {
        public MainWindowViewModel ViewModel { get; }

        public MainPage()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel(new DataService());
        }

        private async void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync();
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.FilteredItems))
            {
                UpdateCheckBoxStates();
            }
            else if (e.PropertyName == nameof(ViewModel.IsDialogOpen))
            {
                if (ViewModel.IsDialogOpen)
                {
                    // 既にダイアログが開いていない場合のみ開く
                    _ = ShowDialogAsync();
                }
            }
        }

        private void UpdateCheckBoxStates()
        {
            TodoDataGrid.UpdateLayout();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddItemCommand.Execute(null);
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SearchText = string.Empty;
            ViewModel.StatusFilter = string.Empty;
            ViewModel.PriorityFilter = string.Empty;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await DeleteSelectedItemsWithConfirmation();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is TodoItem item)
            {
                if (!ViewModel.SelectedIds.Contains(item.Id))
                {
                    ViewModel.SelectedIds.Add(item.Id);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is TodoItem item)
            {
                if (ViewModel.SelectedIds.Contains(item.Id))
                {
                    ViewModel.SelectedIds.Remove(item.Id);
                }
            }
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is TodoItem item)
            {
                checkBox.IsChecked = ViewModel.SelectedIds.Contains(item.Id);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is TodoItem item)
            {
                ViewModel.EditItemCommand.Execute(item);
            }
        }

        private void TodoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // DataGridの選択変更は手動で処理しない（CheckBoxで管理）
        }

        private void TodoDataGrid_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            if (TodoDataGrid.SelectedItem is TodoItem item)
            {
                ViewModel.EditItemCommand.Execute(item);
            }
        }

        private async void TodoDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var deferral = args.GetDeferral();
            try
            {
                if (string.IsNullOrWhiteSpace(ViewModel.EditingItemTitle))
                {
                    args.Cancel = true;
                    return;
                }

                ViewModel.SaveItemCommand.Execute(null);
                await Task.Delay(100);
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void TodoDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            ViewModel.CancelEditCommand.Execute(null);
        }

        private bool _isDialogShowing = false;

        private async Task ShowDialogAsync()
        {
            if (_isDialogShowing)
            {
                return; // 既にダイアログが開いている場合は何もしない
            }

            try
            {
                _isDialogShowing = true;
                await TodoDialog.ShowAsync();
            }
            finally
            {
                _isDialogShowing = false;
            }
        }

        private async Task DeleteSelectedItemsWithConfirmation()
        {
            var count = ViewModel.SelectedIds.Count;
            var dialog = new ContentDialog
            {
                Title = "削除の確認",
                Content = $"{count}件のアイテムを削除しますか？",
                PrimaryButtonText = "削除",
                SecondaryButtonText = "キャンセル",
                XamlRoot = XamlRoot,
                DefaultButton = ContentDialogButton.Secondary
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteSelectedItemsCommand.ExecuteAsync(null);
            }
        }

        private void Grid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            var ctrlKeyState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
            var ctrl = ctrlKeyState.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (ctrl)
            {
                if (e.Key == Windows.System.VirtualKey.N)
                {
                    e.Handled = true;
                    ViewModel.AddItemCommand.Execute(null);
                }
                else if (e.Key == Windows.System.VirtualKey.S)
                {
                    e.Handled = true;
                    ViewModel.SaveDataCommand.Execute(null);
                }
                else if (e.Key == Windows.System.VirtualKey.F)
                {
                    e.Handled = true;
                    SearchTextBox.Focus(FocusState.Programmatic);
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Delete)
            {
                if (ViewModel.SelectedIds.Count > 0)
                {
                    e.Handled = true;
                    _ = DeleteSelectedItemsWithConfirmation();
                }
            }
        }
    }
}

