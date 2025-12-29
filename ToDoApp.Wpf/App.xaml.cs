using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ToDoApp.Wpf.Services;

namespace ToDoApp.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 依存関係注入の設定
            var services = new ServiceCollection();
            services.AddSingleton<IDataService, DataService>();
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
