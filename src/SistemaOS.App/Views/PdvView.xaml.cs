using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SistemaOS.App.ViewModels;

namespace SistemaOS.App.Views;

public partial class PdvView : UserControl
{
    private PdvViewModel? _viewModel;

    public PdvView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is PdvViewModel oldVm)
        {
            oldVm.FocarCodigoBarras -= FocusCodigoBarras;
        }

        if (e.NewValue is PdvViewModel newVm)
        {
            _viewModel = newVm;
            newVm.FocarCodigoBarras += FocusCodigoBarras;
            // Foco inicial
            Loaded += (s, e) => TxtCodigoBarras?.Focus();
        }
    }

    private void FocusCodigoBarras()
    {
        Dispatcher.InvokeAsync(() => TxtCodigoBarras?.Focus());
    }

    private void BtnAlterarQtd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ItemPdv item)
        {
            if (DataContext is PdvViewModel vm)
            {
                var delta = btn.Tag?.ToString() == "1" ? 1 : -1;
                vm.AlterarQuantidadeCommand.Execute((item, delta));
            }
        }
    }

    private void BtnValorRapido_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && DataContext is PdvViewModel vm)
        {
            var tag = btn.Tag?.ToString();
            if (tag == "exato")
            {
                vm.ValorRecebido = vm.Total;
            }
            else if (decimal.TryParse(tag, out var valor))
            {
                vm.ValorRecebido = valor;
            }
        }
    }
}