using Avalonia.Controls;
using Avalonia.Interactivity;
using TreeGrowth.Avalonia.ViewModels;

namespace TreeGrowth.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Pass StorageProvider to ViewModel for file dialogs
        Opened += (_, _) =>
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.StorageProvider = StorageProvider;
            }
        };
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}