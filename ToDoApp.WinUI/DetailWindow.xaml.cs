using System;
using Microsoft.UI.Xaml;
using ToDoApp.WinUI.Models;
using ToDoApp.WinUI.ViewModels;
using Windows.Graphics;

namespace ToDoApp.WinUI
{
    public sealed partial class DetailWindow : Window
    {
        private readonly TodoItem _item;
        private readonly MainWindowViewModel _mainViewModel;

        public DetailWindow(TodoItem item, MainWindowViewModel mainViewModel)
        {
            InitializeComponent();
            _item = item;
            _mainViewModel = mainViewModel;

            Title = $"詳細 - {item.Title}";
            AppWindow.Resize(new SizeInt32(520, 560));

            if (Content is FrameworkElement root &&
                App.Current is App app &&
                app.MainWindow?.Content is FrameworkElement mainRoot)
            {
                root.RequestedTheme = mainRoot.RequestedTheme;
            }

            IdTextBlock.Text = item.Id.ToString();
            TitleTextBox.Text = item.Title;
            DescriptionTextBox.Text = item.Description;
            StatusComboBox.SelectedItem = item.Status;
            PriorityComboBox.SelectedItem = item.Priority;
            if (item.DueDate.HasValue)
            {
                DueDatePicker.Date = item.DueDate.Value;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                return;
            }

            _item.Title = TitleTextBox.Text.Trim();
            _item.Description = DescriptionTextBox.Text ?? string.Empty;
            _item.Status = StatusComboBox.SelectedItem as string ?? "未着手";
            _item.Priority = PriorityComboBox.SelectedItem as string ?? "中";
            _item.DueDate = DueDatePicker.Date;
            _item.UpdatedAt = DateTimeOffset.Now;

            _mainViewModel.NotifyItemUpdated(_item);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
