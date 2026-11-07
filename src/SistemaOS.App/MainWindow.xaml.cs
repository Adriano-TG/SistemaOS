using System.Windows;
using SistemaOS.App.ViewModels;

namespace SistemaOS.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}