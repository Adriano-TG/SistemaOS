using CommunityToolkit.Mvvm.ComponentModel;

namespace SistemaOS.App.ViewModels;

public class EmConstrucaoViewModel : ObservableObject
{
    public string Titulo { get; }

    public EmConstrucaoViewModel(string titulo)
    {
        Titulo = titulo;
    }
}