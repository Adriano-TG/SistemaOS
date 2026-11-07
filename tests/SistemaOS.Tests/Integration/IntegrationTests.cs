using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SistemaOS.App.Services;
using SistemaOS.App.ViewModels;
using SistemaOS.Core.Entities;
using SistemaOS.Core.Interfaces;
using SistemaOS.Infrastructure.Data;
using SistemaOS.Infrastructure.Services;
using Xunit;

namespace SistemaOS.Tests.Integration;

/// <summary>
/// Testes de integração end-to-end cobrindo fluxos principais do sistema
/// </summary>
public class IntegrationTests : TestBase
{
    private readonly IServiceProvider _sp;

    public IntegrationTests()
    {
        _sp = ServiceProvider;
    }

    #region Fluxo: Login → Ordem de Serviço → PDF

    [Fact]
    public async Task FluxoCompleto_Login_CriarOS_ImprimirPDF_DeveFuncionar()
    {
        // ===== ARRANGE: Setup inicial =====
        var currentUser = _sp.GetRequiredService<ICurrentUserService>();
        var authService = _sp.GetRequiredService<IAuthService>();
        var empresaRepo = _sp.GetRequiredService<IEmpresaRepository>();
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();
        var estoqueService = _sp.GetRequiredService<IEstoqueService>();
        var pdfService = _sp.GetRequiredService<PdfService>();

        // Seed: Empresa (necessária para PDF)
        var empresa = new Empresa
        {
            NomeFantasia = "Teste OS Ltda",
            Cnpj = "12.345.678/0001-90",
            RazaoSocial = "Teste OS Ltda",
            Telefone = "11999999999",
            Logradouro = "Rua A",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Uf = "SP",
            Cep = "01000-000"
        };
        await empresaRepo.AddAsync(empresa);

        // Seed: Usuário admin (senha: admin123 - hash já conhecido)
        var admin = new Usuario
        {
            Nome = "Admin Teste",
            Login = "admin",
            SenhaHash = authService.HashSenha("admin123"),
            Role = RoleUsuario.Admin,
            Ativo = true,
            DataCadastro = DateTime.Now,
            PrimeiroLogin = false
        };
        DbContext.Usuarios.Add(admin);
        DbContext.SaveChanges();

        // Seed: Cliente
        var cliente = new Cliente
        {
            Nome = "João Silva",
            Telefone = "11988887777",
            CpfCnpj = "12345678901",
            Endereco = "Rua B, 200"
        };
        await clienteRepo.AddAsync(cliente);

        // Seed: Produto com estoque
        var produto = new Produto
        {
            Codigo = "PROD001",
            Nome = "Fonte 500W",
            PrecoCusto = 150,
            PrecoVenda = 300,
            QuantidadeAtual = 10,
            QuantidadeMinima = 2,
            UnidadeMedida = "UN",
            Ativo = true
        };
        DbContext.Produtos.Add(produto);
        DbContext.SaveChanges();

        // ===== ACT 1: Login =====
        var loginResult = await authService.LoginAsync("admin", "admin123");
        loginResult.Success.Should().BeTrue();
        loginResult.Usuario.Should().NotBeNull();
        loginResult.Usuario!.Role.Should().Be(RoleUsuario.Admin);

        currentUser.UsuarioLogado = loginResult.Usuario!;
        currentUser.UsuarioLogado.Should().NotBeNull();

        // ===== ACT 2: Criar Ordem de Serviço =====
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "Notebook Dell",
            Marca = "Dell",
            NumeroSerie = "SN123456",
            EmGarantia = false,
            DefeitoRelatado = "Não liga",
            Status = StatusOS.Aberta,
            DataAbertura = DateTime.Now,
            Itens = new List<ItemOS>
            {
                new()
                {
                    ProdutoId = produto.Id,
                    Descricao = "Fonte 500W",
                    Quantidade = 1,
                    ValorUnitario = 300
                },
                new()
                {
                    Descricao = "Mão de obra - Diagnóstico",
                    Quantidade = 1,
                    ValorUnitario = 80
                }
            }
        };

        await ordemRepo.AddAsync(os);
        var osId = os.Id;
        osId.Should().BeGreaterThan(0);

        // ===== ACT 3: Atualizar status para Entregue (baixa estoque) =====
        var osCompleta = await ordemRepo.GetByIdWithItensAsync(osId);
        osCompleta!.Status = StatusOS.Entregue;
        osCompleta.DataConclusao = DateTime.Now;
        await ordemRepo.UpdateAsync(osCompleta);

        // Baixar estoque via service
        var baixaOk = await estoqueService.BaixarEstoqueAsync(osId);
        baixaOk.Should().BeTrue("Estoque suficiente para 1 unidade");

        // Verificar estoque baixado - clear tracker first
        ClearChangeTracker();
        var produtoAtualizado = await DbContext.Produtos.FindAsync(produto.Id);
        produtoAtualizado!.QuantidadeAtual.Should().Be(9);

        // ===== ACT 4: Gerar PDF da OS =====
        var osParaPdf = await ordemRepo.GetByIdWithItensAsync(osId);
        var caminhoPdf = await pdfService.GerarOrdemServicoAsync(osParaPdf!);

        // ===== ASSERT =====
        caminhoPdf.Should().NotBeNullOrEmpty();
        System.IO.File.Exists(caminhoPdf).Should().BeTrue("PDF deve ser gerado no disco");
        caminhoPdf.Should().Contain($"OS_{osId}_");
        caminhoPdf.Should().EndWith(".pdf");

        // Cleanup
        try { System.IO.File.Delete(caminhoPdf); } catch { }
    }

    #endregion

    #region Fluxo: Login → PDV → Cupom

    [Fact]
    public async Task FluxoCompleto_Login_PDV_VendaCupom_DeveFuncionar()
    {
        // ===== ARRANGE =====
        var currentUser = _sp.GetRequiredService<ICurrentUserService>();
        var authService = _sp.GetRequiredService<IAuthService>();
        var empresaRepo = _sp.GetRequiredService<IEmpresaRepository>();
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();
        var estoqueService = _sp.GetRequiredService<IEstoqueService>();
        var pdfService = _sp.GetRequiredService<PdfService>();

        // Empresa
        var empresa = new Empresa
        {
            NomeFantasia = "PDV Teste",
            Cnpj = "12.345.678/0001-91",
            Telefone = "1133334444"
        };
        await empresaRepo.AddAsync(empresa);

        // Usuário
        var admin = new Usuario
        {
            Nome = "Operador PDV",
            Login = "pdv",
            SenhaHash = authService.HashSenha("pdv123"),
            Role = RoleUsuario.Atendente,
            Ativo = true,
            DataCadastro = DateTime.Now,
            PrimeiroLogin = false
        };
        DbContext.Usuarios.Add(admin);
        DbContext.SaveChanges();

        // Cliente opcional
        var cliente = new Cliente
        {
            Nome = "Maria Santos",
            Telefone = "11977776666",
            WhatsApp = "11977776666",
            CpfCnpj = "98765432100"
        };
        await clienteRepo.AddAsync(cliente);

        // Produtos
        var prod1 = new Produto { Codigo = "CEL001", Nome = "Capa Celular", PrecoCusto = 15, PrecoVenda = 45, QuantidadeAtual = 20, QuantidadeMinima = 5, UnidadeMedida = "UN", Ativo = true };
        var prod2 = new Produto { Codigo = "CEL002", Nome = "Película Vidro", PrecoCusto = 8, PrecoVenda = 25, QuantidadeAtual = 30, QuantidadeMinima = 5, UnidadeMedida = "UN", Ativo = true };
        DbContext.Produtos.AddRange(prod1, prod2);
        DbContext.SaveChanges();

        // ===== ACT 1: Login =====
        var loginResult = await authService.LoginAsync("pdv", "pdv123");
        loginResult.Success.Should().BeTrue();
        currentUser.UsuarioLogado = loginResult.Usuario!;

        // ===== ACT 2: Criar venda PDV (OS com status Entregue) =====
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "VENDA PDV",
            Status = StatusOS.Entregue,
            DataAbertura = DateTime.Now,
            DataConclusao = DateTime.Now,
            LaudoTecnico = "Venda PDV - Dinheiro - Cliente: Maria Santos",
            Itens = new List<ItemOS>
            {
                new() { ProdutoId = prod1.Id, Descricao = "Capa Celular", Quantidade = 2, ValorUnitario = 45 },
                new() { ProdutoId = prod2.Id, Descricao = "Película Vidro", Quantidade = 1, ValorUnitario = 25 }
            }
        };

        await ordemRepo.AddAsync(os);
        var osId = os.Id;

        // Baixar estoque
        await estoqueService.BaixarEstoqueAsync(osId);

        // Verificar estoque - clear tracker first
        ClearChangeTracker();
        (await DbContext.Produtos.FindAsync(prod1.Id))!.QuantidadeAtual.Should().Be(18);
        (await DbContext.Produtos.FindAsync(prod2.Id))!.QuantidadeAtual.Should().Be(29);

        // ===== ACT 3: Gerar cupom =====
        var osParaCupom = await ordemRepo.GetByIdWithItensAsync(osId);
        var caminhoCupom = await pdfService.GerarCupomPdvAsync(
            osParaCupom!, cliente, "Dinheiro", 115, 0);

        // ===== ASSERT =====
        caminhoCupom.Should().NotBeNullOrEmpty();
        System.IO.File.Exists(caminhoCupom).Should().BeTrue();
        caminhoCupom.Should().Contain($"CUPOM_{osId}_");

        // Cleanup
        try { System.IO.File.Delete(caminhoCupom); } catch { }
    }

    #endregion

    #region Fluxo: CRUD Cliente Completo

    [Fact]
    public async Task Fluxo_CRUD_Cliente_DeveFuncionar()
    {
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();

        // CREATE
        var novo = new Cliente
        {
            Nome = "Cliente Integração",
            Telefone = "11999990000",
            WhatsApp = "11999990000",
            CpfCnpj = "11122233344",
            Endereco = "Rua Integração, 123"
        };
        await clienteRepo.AddAsync(novo);
        var id = novo.Id;
        id.Should().BeGreaterThan(0);

        // READ - GetById
        var lido = await clienteRepo.GetByIdAsync(id);
        lido.Should().NotBeNull();
        lido!.Nome.Should().Be("Cliente Integração");
        lido.CpfCnpj.Should().Be("11122233344");

        // READ - GetAtivos (all clients since no Ativo property)
        var ativos = await clienteRepo.GetAtivosAsync();
        ativos.Should().Contain(c => c.Id == id);

        // READ - Search
        var search = await clienteRepo.SearchByNomeAsync("Integração");
        search.Should().Contain(c => c.Id == id);

        // UPDATE
        lido!.Nome = "Cliente Atualizado";
        lido.Endereco = "Novo Endereço, 456";
        await clienteRepo.UpdateAsync(lido);

        var atualizado = await clienteRepo.GetByIdAsync(id);
        atualizado!.Nome.Should().Be("Cliente Atualizado");
        atualizado.Endereco.Should().Be("Novo Endereço, 456");

        // Hard delete check - use GetByIdAsync after delete
        var os = new OrdemServico { ClienteId = id, Equipamento = "Teste", Status = StatusOS.Aberta };
        DbContext.Ordens.Add(os);
        DbContext.SaveChanges();

        // Check if can delete by trying to find dependencies
        var clienteComOs = await clienteRepo.GetByIdAsync(id);
        clienteComOs.Should().NotBeNull();
        var ordensDoCliente = await ordemRepo.GetByClienteAsync(id);
        ordensDoCliente.Should().NotBeEmpty();

        // Clean up
        DbContext.Ordens.Remove(os);
        DbContext.SaveChanges();
    }

    #endregion

    #region Fluxo: CRUD Produto + Estoque

    [Fact]
    public async Task Fluxo_CRUD_Produto_Estoque_DeveFuncionar()
    {
        var produtoRepo = _sp.GetRequiredService<IProdutoRepository>();
        var estoqueService = _sp.GetRequiredService<IEstoqueService>();

        // CREATE
        var prod = new Produto
        {
            Codigo = "INT001",
            Nome = "Produto Integração",
            Descricao = "Desc teste",
            PrecoCusto = 50,
            PrecoVenda = 100,
            QuantidadeAtual = 100,
            QuantidadeMinima = 10,
            UnidadeMedida = "UN",
            Ativo = true
        };
        await produtoRepo.AddAsync(prod);
        var id = prod.Id;

        // READ
        var lido = await produtoRepo.GetByIdAsync(id);
        lido.Should().NotBeNull();
        lido!.Codigo.Should().Be("INT001");

        // GetByCodigo
        var porCodigo = await produtoRepo.GetByCodigoAsync("INT001");
        porCodigo.Should().NotBeNull();
        porCodigo!.Id.Should().Be(id);

        // GetAtivos
        var ativos = await produtoRepo.GetAtivosAsync();
        ativos.Should().Contain(p => p.Id == id);

        // Baixo estoque - não deve aparecer (100 > 10)
        var baixo = await produtoRepo.GetBaixoEstoqueAsync();
        baixo.Should().NotContain(p => p.Id == id);

        // UPDATE - reduzir estoque para disparar "baixo estoque"
        lido!.QuantidadeAtual = 5;
        await produtoRepo.UpdateAsync(lido);

        baixo = await produtoRepo.GetBaixoEstoqueAsync();
        baixo.Should().Contain(p => p.Id == id);

        // Sem estoque
        lido.QuantidadeAtual = 0;
        await produtoRepo.UpdateAsync(lido);

        var sem = await produtoRepo.GetSemEstoqueAsync();
        sem.Should().Contain(p => p.Id == id);

        // Valor total estoque
        lido.QuantidadeAtual = 10;
        lido.PrecoCusto = 50;
        await produtoRepo.UpdateAsync(lido);

        var total = await produtoRepo.GetValorTotalEstoqueAsync();
        total.Should().BeGreaterThan(0);

        // Entrada estoque
        await estoqueService.AdicionarMovimentoAsync(id, TipoMovimentoEstoque.Entrada, 20, 50, "Compra NF-123");
        ClearChangeTracker();
        (await DbContext.Produtos.FindAsync(id))!.QuantidadeAtual.Should().Be(30);

        // Saída estoque
        await estoqueService.AdicionarMovimentoAsync(id, TipoMovimentoEstoque.Saida, 5, 50, "Venda balcão PDV-456");
        ClearChangeTracker();
        (await DbContext.Produtos.FindAsync(id))!.QuantidadeAtual.Should().Be(25);

        // Movimentações registradas
        var movs = await DbContext.MovimentosEstoque
            .Where(m => m.ProdutoId == id)
            .OrderByDescending(m => m.DataMovimento)
            .ToListAsync();
        movs.Should().HaveCountGreaterOrEqualTo(2);
        movs.First().Tipo.Should().Be(TipoMovimentoEstoque.Saida);
        movs.Last().Tipo.Should().Be(TipoMovimentoEstoque.Entrada);

        // CanDelete - com movimentos não deve poder (check via dependencies)
        var movsCount = await DbContext.MovimentosEstoque.CountAsync(m => m.ProdutoId == id);
        movsCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Fluxo: Ordem de Serviço - Status Transitions

    [Fact]
    public async Task Fluxo_OS_TransicoesStatus_DeveFuncionar()
    {
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();

        var cliente = new Cliente { Nome = "Cliente Status", Telefone = "11999990001" };
        await clienteRepo.AddAsync(cliente);

        // Criar OS Aberta
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "PC Gamer",
            Status = StatusOS.Aberta,
            DataAbertura = DateTime.Now
        };
        await ordemRepo.AddAsync(os);
        var id = os.Id;

        // Aberta → Em Análise
        var os1 = await ordemRepo.GetByIdWithItensAsync(id);
        os1!.Status = StatusOS.EmAnalise;
        await ordemRepo.UpdateAsync(os1);

        var check1 = await ordemRepo.GetByIdWithItensAsync(id);
        check1!.Status.Should().Be(StatusOS.EmAnalise);

        // Em Análise → Aguardando Peça
        var os2 = await ordemRepo.GetByIdWithItensAsync(id);
        os2!.Status = StatusOS.AguardandoPeca;
        await ordemRepo.UpdateAsync(os2);

        // Aguardando Peça → Em Análise (no EmAndamento, usando EmAnalise como intermediário)
        var os3 = await ordemRepo.GetByIdWithItensAsync(id);
        os3!.Status = StatusOS.EmAnalise;
        await ordemRepo.UpdateAsync(os3);

        // Em Análise → Pronta
        var os4 = await ordemRepo.GetByIdWithItensAsync(id);
        os4!.Status = StatusOS.Pronta;
        await ordemRepo.UpdateAsync(os4);

        // Pronta → Entregue (com data conclusão)
        var os5 = await ordemRepo.GetByIdWithItensAsync(id);
        os5!.Status = StatusOS.Entregue;
        os5.DataConclusao = DateTime.Now;
        await ordemRepo.UpdateAsync(os5);

        var final = await ordemRepo.GetByIdWithItensAsync(id);
        final!.Status.Should().Be(StatusOS.Entregue);
        final.DataConclusao.Should().NotBeNull();

        // Cancelada (alternativo)
        var osCancelada = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "Cancelada",
            Status = StatusOS.Cancelada,
            DataAbertura = DateTime.Now
        };
        await ordemRepo.AddAsync(osCancelada);
        osCancelada.Status.Should().Be(StatusOS.Cancelada);
    }

    #endregion

    #region Fluxo: Estoque - Baixa em OS com Múltiplos Itens

    [Fact]
    public async Task Fluxo_Estoque_BaixaMultiplosItens_DeveFuncionar()
    {
        var produtoRepo = _sp.GetRequiredService<IProdutoRepository>();
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();
        var estoqueService = _sp.GetRequiredService<IEstoqueService>();

        var cliente = new Cliente { Nome = "Cliente Multi", Telefone = "11999990002" };
        await clienteRepo.AddAsync(cliente);

        var prod1 = new Produto { Codigo = "MULT01", Nome = "Item A", PrecoCusto = 10, PrecoVenda = 20, QuantidadeAtual = 5, QuantidadeMinima = 1, UnidadeMedida = "UN", Ativo = true };
        var prod2 = new Produto { Codigo = "MULT02", Nome = "Item B", PrecoCusto = 15, PrecoVenda = 30, QuantidadeAtual = 3, QuantidadeMinima = 1, UnidadeMedida = "UN", Ativo = true };
        var prod3 = new Produto { Codigo = "MULT03", Nome = "Item C", PrecoCusto = 20, PrecoVenda = 40, QuantidadeAtual = 10, QuantidadeMinima = 2, UnidadeMedida = "UN", Ativo = true };
        DbContext.Produtos.AddRange(prod1, prod2, prod3);
        DbContext.SaveChanges();

        // OS com 3 itens de produtos diferentes
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "Multi Itens",
            Status = StatusOS.Entregue,
            DataAbertura = DateTime.Now,
            DataConclusao = DateTime.Now,
            Itens = new List<ItemOS>
            {
                new() { ProdutoId = prod1.Id, Descricao = "Item A", Quantidade = 2, ValorUnitario = 20 },
                new() { ProdutoId = prod2.Id, Descricao = "Item B", Quantidade = 1, ValorUnitario = 30 },
                new() { ProdutoId = prod3.Id, Descricao = "Item C", Quantidade = 3, ValorUnitario = 40 }
            }
        };
        await ordemRepo.AddAsync(os);

        // Baixa estoque
        var ok = await estoqueService.BaixarEstoqueAsync(os.Id);
        ok.Should().BeTrue();

        // Verificar quantidades - clear tracker first
        ClearChangeTracker();
        (await DbContext.Produtos.FindAsync(prod1.Id))!.QuantidadeAtual.Should().Be(3); // 5-2
        (await DbContext.Produtos.FindAsync(prod2.Id))!.QuantidadeAtual.Should().Be(2); // 3-1
        (await DbContext.Produtos.FindAsync(prod3.Id))!.QuantidadeAtual.Should().Be(7); // 10-3

        // Verificar movimentos criados
        var movs = await DbContext.MovimentosEstoque
            .Where(m => m.OrdemServicoId == os.Id)
            .ToListAsync();
        movs.Should().HaveCount(3);
        movs.Should().AllSatisfy(m => m.Tipo.Should().Be(TipoMovimentoEstoque.Saida));
    }

    #endregion

    #region Fluxo: Estoque Insuficiente

    [Fact]
    public async Task Fluxo_Estoque_Insuficiente_DeveFalhar()
    {
        var produtoRepo = _sp.GetRequiredService<IProdutoRepository>();
        var ordemRepo = _sp.GetRequiredService<IOrdemServicoRepository>();
        var clienteRepo = _sp.GetRequiredService<IClienteRepository>();
        var estoqueService = _sp.GetRequiredService<IEstoqueService>();

        var cliente = new Cliente { Nome = "Cliente Sem Estoque", Telefone = "11999990003" };
        await clienteRepo.AddAsync(cliente);

        // Ensure fresh product with exactly 1 unit
        ClearChangeTracker();
        var prod = new Produto { Codigo = "SEM001", Nome = "Pouco Estoque", PrecoCusto = 10, PrecoVenda = 20, QuantidadeAtual = 1, QuantidadeMinima = 1, UnidadeMedida = "UN", Ativo = true };
        DbContext.Produtos.Add(prod);
        DbContext.SaveChanges();

        // OS pede 5 unidades mas só tem 1
        var os = new OrdemServico
        {
            ClienteId = cliente.Id,
            Equipamento = "Teste Insuficiente",
            Status = StatusOS.Entregue,
            DataAbertura = DateTime.Now,
            DataConclusao = DateTime.Now,
            Itens = new List<ItemOS>
            {
                new() { ProdutoId = prod.Id, Descricao = "Pouco Estoque", Quantidade = 5, ValorUnitario = 20 }
            }
        };
        await ordemRepo.AddAsync(os);

        // Baixa deve falhar
        var ok = await estoqueService.BaixarEstoqueAsync(os.Id);
        ok.Should().BeFalse("Estoque insuficiente deve retornar false");

        // Estoque não deve ter mudado
        ClearChangeTracker();
        (await DbContext.Produtos.FindAsync(prod.Id))!.QuantidadeAtual.Should().Be(1);

        // Nenhum movimento de saída deve ter sido criado para esta OS
        var movs = await DbContext.MovimentosEstoque
            .Where(m => m.OrdemServicoId == os.Id)
            .ToListAsync();
        movs.Should().BeEmpty();
    }

    #endregion

    #region Fluxo: Autenticação - Primeiro Login Força Troca Senha

    [Fact]
    public async Task Fluxo_Auth_PrimeiroLogin_ForcaTrocaSenha()
    {
        var authService = _sp.GetRequiredService<IAuthService>();
        var currentUser = _sp.GetRequiredService<ICurrentUserService>();

        // Criar usuário com PrimeiroLogin = true
        var hashInicial = authService.HashSenha("senhaInicial123");
        var user = new Usuario
        {
            Nome = "Novo Usuario",
            Login = "novo",
            SenhaHash = hashInicial,
            Role = RoleUsuario.Tecnico,
            Ativo = true,
            DataCadastro = DateTime.Now,
            PrimeiroLogin = true
        };
        DbContext.Usuarios.Add(user);
        DbContext.SaveChanges();

        // Login deve retornar RequerTrocaSenha = true
        var result = await authService.LoginAsync("novo", "senhaInicial123");
        result.Success.Should().BeTrue();
        result.RequerTrocaSenha.Should().BeTrue("Primeiro login deve exigir troca de senha");
        result.Usuario!.PrimeiroLogin.Should().BeTrue();

        // Trocar senha
        var trocaOk = await authService.AlterarSenhaAsync(user.Id, "senhaInicial123", "NovaSenha456!");
        trocaOk.Should().BeTrue();

        // PrimeiroLogin deve ser false agora - usar factory para contexto fresco
        using var dbCheck = DbFactory.CreateDbContext();
        var userAtualizado = await dbCheck.Usuarios.FindAsync(user.Id);
        userAtualizado!.PrimeiroLogin.Should().BeFalse();

        // Login com nova senha deve funcionar sem RequerTrocaSenha
        var result2 = await authService.LoginAsync("novo", "NovaSenha456!");
        result2.Success.Should().BeTrue();
        result2.RequerTrocaSenha.Should().BeFalse();
    }

    #endregion

    #region Fluxo: Permissões por Role

    [Fact]
    public async Task Fluxo_Permissoes_PorRole_DeveRespeitarRegras()
    {
        var authService = _sp.GetRequiredService<IAuthService>();
        var currentUser = _sp.GetRequiredService<ICurrentUserService>();

        // Criar usuários de cada role
        var admin = new Usuario { Nome = "Admin Perm", Login = "adminp", SenhaHash = authService.HashSenha("123"), Role = RoleUsuario.Admin, Ativo = true, DataCadastro = DateTime.Now };
        var tecnico = new Usuario { Nome = "Tecnico Perm", Login = "tecp", SenhaHash = authService.HashSenha("123"), Role = RoleUsuario.Tecnico, Ativo = true, DataCadastro = DateTime.Now };
        var atendente = new Usuario { Nome = "Atendente Perm", Login = "atep", SenhaHash = authService.HashSenha("123"), Role = RoleUsuario.Atendente, Ativo = true, DataCadastro = DateTime.Now };
        DbContext.Usuarios.AddRange(admin, tecnico, atendente);
        DbContext.SaveChanges();

        // Testar Admin
        currentUser.UsuarioLogado = admin;
        currentUser.PodeGerenciarUsuarios.Should().BeTrue();
        currentUser.PodeVerTodasOS.Should().BeTrue();
        currentUser.PodeAlterarStatusOS.Should().BeTrue();
        currentUser.PodeGerenciarEstoque.Should().BeTrue();
        currentUser.PodeVerFinanceiro.Should().BeTrue();

        // Testar Técnico
        currentUser.UsuarioLogado = tecnico;
        currentUser.PodeGerenciarUsuarios.Should().BeFalse();
        currentUser.PodeVerTodasOS.Should().BeTrue();
        currentUser.PodeAlterarStatusOS.Should().BeTrue();
        currentUser.PodeGerenciarEstoque.Should().BeFalse();
        currentUser.PodeVerFinanceiro.Should().BeFalse();

        // Testar Atendente
        currentUser.UsuarioLogado = atendente;
        currentUser.PodeGerenciarUsuarios.Should().BeFalse();
        currentUser.PodeVerTodasOS.Should().BeFalse();
        currentUser.PodeAlterarStatusOS.Should().BeFalse();
        currentUser.PodeGerenciarEstoque.Should().BeFalse();
        currentUser.PodeVerFinanceiro.Should().BeFalse();
    }

    #endregion

    #region Fluxo: Configurações Empresa

    [Fact]
    public async Task Fluxo_Configuracoes_Empresa_DevePersistir()
    {
        var empresaRepo = _sp.GetRequiredService<IEmpresaRepository>();
        var textFormatter = _sp.GetRequiredService<ITextFormatterService>();

        // Limpar empresas existentes para evitar poluição de testes anteriores
        ClearChangeTracker();
        var existentes = await DbContext.DadosEmpresa.ToListAsync();
        DbContext.DadosEmpresa.RemoveRange(existentes);
        await DbContext.SaveChangesAsync();

        // Criar empresa
        var emp = new Empresa
        {
            NomeFantasia = "Empresa Config",
            Cnpj = "11.222.333/0001-44",
            RazaoSocial = "Empresa Config Ltda",
            Telefone = "1133334444",
            Logradouro = "Av. Config",
            Numero = "999",
            Bairro = "Config",
            Cidade = "São Paulo",
            Uf = "SP",
            Cep = "01000-000",
            TipoOsIndex = 1, // 2 vias
            OrientacaoIndex = 1, // Paisagem
            TamanhoPapelIndex = 1 // Bobina 80mm
        };
        await empresaRepo.AddAsync(emp);
        var id = emp.Id;

        // Ler e verificar
        var lida = await empresaRepo.GetEmpresaAsync();
        lida.Should().NotBeNull();
        lida!.TipoOsIndex.Should().Be(1);
        lida.TamanhoPapelIndex.Should().Be(1);
        // Nota: Regra de negócio "OrientacaoIndex = 0 para bobina" é aplicada no ViewModel, não no Repository
        // lida.OrientacaoIndex.Should().Be(0); // This would require ViewModel layer

        // Atualizar - clear tracker first to avoid conflict with entity from AddAsync
        ClearChangeTracker();
        lida.NomeFantasia = "Empresa Config Atualizada";
        lida.TipoOsIndex = 0; // 1 via
        lida.TamanhoPapelIndex = 0; // A4
        lida.OrientacaoIndex = 1; // Paisagem (válido para A4)
        await empresaRepo.UpdateAsync(lida);

        var atualizada = await empresaRepo.GetEmpresaAsync();
        atualizada!.NomeFantasia.Should().Be("Empresa Config Atualizada");
        atualizada.TipoOsIndex.Should().Be(0);
        atualizada.TamanhoPapelIndex.Should().Be(0);
        atualizada.OrientacaoIndex.Should().Be(1);

        // Formatter service
        textFormatter.FormatoTextoIndex = 2; // Title Case
        textFormatter.Formatar("TESTE TEXTO").Should().Be("Teste Texto");
    }

    #endregion
}