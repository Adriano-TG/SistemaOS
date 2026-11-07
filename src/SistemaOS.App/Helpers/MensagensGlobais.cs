using CommunityToolkit.Mvvm.Messaging.Messages;
using SistemaOS.Core.Entities;

namespace SistemaOS.App.Helpers;

// Mensagem que transporta o Cliente que acabou de ser salvo
public class ClienteSalvoMessage : ValueChangedMessage<Cliente>
{
    public ClienteSalvoMessage(Cliente value) : base(value) { }
}

// Mensagem para seleção de cliente no PDV/OS
public class ClienteSelecionadoMessage : ValueChangedMessage<Cliente>
{
    public ClienteSelecionadoMessage(Cliente value) : base(value) { }
}

// Mensagem que pede ao sistema para trocar de ecrã
public class MudarTelaMessage : ValueChangedMessage<string>
{
    public MudarTelaMessage(string value) : base(value) { }
}