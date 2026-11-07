using SistemaOS.Core.Interfaces;

namespace SistemaOS.Infrastructure.Services;

public class UIStateService : IUIStateService
{
    public bool CadastrandoDaOS { get; set; } = false;
    public int? ClienteIdPendente { get; set; } = null;
    public bool SelecionandoParaPdv { get; set; } = false;
}