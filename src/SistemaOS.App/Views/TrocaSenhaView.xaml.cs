using System.Windows;
using System.Windows.Controls;
using SistemaOS.App.ViewModels;

namespace SistemaOS.App.Views
{
    public partial class TrocaSenhaView : Window
    {
        public TrocaSenhaView(TrocaSenhaViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void SenhaAtualBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrocaSenhaViewModel vm)
            {
                vm.SenhaAtual = SenhaAtualBox.Password;
            }
        }

        private void NovaSenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrocaSenhaViewModel vm)
            {
                vm.NovaSenha = NovaSenhaBox.Password;
            }
        }

        private void ConfirmarNovaSenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is TrocaSenhaViewModel vm)
            {
                vm.ConfirmarNovaSenha = ConfirmarNovaSenhaBox.Password;
            }
        }

        private void MensagemText_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Auto-show/hide based on message content
            if (sender is TextBlock tb)
            {
                tb.Visibility = string.IsNullOrEmpty(tb.Text) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }
}