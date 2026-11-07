using System.Windows;
using System.Windows.Controls;
using SistemaOS.App.ViewModels;

namespace SistemaOS.App.Views
{
    public partial class UsuariosView : Window
    {
        public UsuariosView(UsuariosViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void SenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsuariosViewModel vm && sender is PasswordBox pb)
            {
                vm.Senha = pb.Password;
            }
        }

        private void ConfirmarSenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UsuariosViewModel vm && sender is PasswordBox pb)
            {
                vm.ConfirmarSenha = pb.Password;
            }
        }
    }
}