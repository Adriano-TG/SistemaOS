namespace SistemaOS.Core.Interfaces;

public interface IUIStateService
{
    bool CadastrandoDaOS { get; set; }
    int? ClienteIdPendente { get; set; }
    bool SelecionandoParaPdv { get; set; }
}