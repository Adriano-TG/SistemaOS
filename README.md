# SistemaOS - Sistema de Ordens de Serviço

Sistema de gestão de ordens de serviço para assistência técnica, desenvolvido em **WPF (.NET 10)** com arquitetura **MVVM**, **Entity Framework Core** (SQLite), e **QuestPDF** para geração de relatórios.

---

## 🏗️ Arquitetura

```
src/
├── SistemaOS.Core           # Domain (Entities, Interfaces, Validators)
├── SistemaOS.Infrastructure # Data (EF Core, Migrations, Services)
└── SistemaOS.App            # WPF App (ViewModels, Views, Services)

tests/
└── SistemaOS.Tests          # xUnit + Moq + FluentAssertions (138 testes)
```

---

## 🚀 Status do Projeto

| CI/CD | Cobertura | Mutation Score |
|-------|-----------|----------------|
| ![Build](https://github.com/SEU_USUARIO/SistemaOS/workflows/.NET%20CI/CD/badge.svg) | ![Coverage](https://img.shields.io/badge/coverage-31.4%25-yellow) | ![Mutation](https://img.shields.io/badge/mutation%20score-89.16%25-brightgreen) |

> **Nota:** Substitua `SEU_USUARIO` pelo seu usuário/organização do GitHub.

---

## ✅ Funcionalidades

- 📋 **Ordens de Serviço** — CRUD completo, itens, status, impressão PDF
- 👥 **Clientes** — Cadastro, busca por CEP (ViaCEP), CNPJ (ReceitaWS)
- ⚙️ **Configurações** — Dados da empresa, templates de impressão, migração legado
- 🎨 **Temas** — Claro/Escuro + cor de destaque personalizável
- 📊 **Relatórios** — PDF via QuestPDF (A4, bobina 58/80mm)

---

## 🛠️ Tecnologias

| Camada | Tecnologias |
|--------|-------------|
| **UI** | WPF, CommunityToolkit.Mvvm, MaterialDesignThemes |
| **Dados** | Entity Framework Core 10, SQLite |
| **Testes** | xUnit, Moq, FluentAssertions, coverlet |
| **Qualidade** | Stryker.NET (mutation testing) |
| **PDF** | QuestPDF |
| **CI/CD** | GitHub Actions |

---

## 📦 Como Executar

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 (WPF)

### Clonar e rodar
```bash
git clone https://github.com/SEU_USUARIO/SistemaOS.git
cd SistemaOS
dotnet restore
dotnet run --project src/SistemaOS.App
```

### Executar testes
```bash
# Todos os testes
dotnet test

# Com cobertura
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage

# Mutation testing (Stryker.NET)
cd tests/SistemaOS.Tests
dotnet stryker
```

---

## 📊 Qualidade de Código

### Cobertura de Testes (coverlet + ReportGenerator)
| Assembly | Linhas | Métodos | Branches |
|----------|--------|---------|----------|
| **SistemaOS.Core** | **89.1%** | 100% | — |
| **SistemaOS.App** | **64%** | ~70% | — |
| **SistemaOS.Infrastructure** | 3.1% | — | — |
| **Total** | **31.4%** | **57.4%** | **50.7%** |

> Views (XAML) e Migrations não são testáveis em headless — esperado.

### Mutation Testing (Stryker.NET)
- **Score global (Core): 89.16%**
- **Entities**: 100% (Cliente, Empresa, ItemOS, OrdemServico)
- **DocumentoValidator**: 86.4% (8 mutantes sobrevivem — edge cases CPF/CNPJ)

---

## 🔧 Estrutura de Pastas

```
SistemaOS/
├── .github/workflows/dotnet.yml    # CI/CD GitHub Actions
├── SistemaOS.slnx                  # Solution file
├── src/
│   ├── SistemaOS.Core/
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Validations/
│   ├── SistemaOS.Infrastructure/
│   │   ├── Data/
│   │   ├── Migrations/
│   │   └── Services/
│   └── SistemaOS.App/
│       ├── ViewModels/
│       ├── Views/
│       └── Services/
├── tests/
│   └── SistemaOS.Tests/
│       ├── ViewModels/
│       ├── Services/
│       ├── Helpers/
│       ├── Core/
│       ├── Infrastructure/
│       └── TestBase.cs
└── coverage-report/                # Gerado após testes com cobertura
```

---

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch: `git checkout -b feature/nova-funcionalidade`
3. Commit suas mudanças: `git commit -m 'Adiciona nova funcionalidade'`
4. Push para a branch: `git push origin feature/nova-funcionalidade`
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para detalhes.

---

## 🙋‍♂️ Autor

Desenvolvido com ❤️ usando .NET 10, WPF e boas práticas de arquitetura limpa.

---

**Badges personalizados (substitua `SEU_USUARIO`):**

```markdown
![Build](https://github.com/SEU_USUARIO/SistemaOS/workflows/.NET%20CI/CD/badge.svg)
![Coverage](https://img.shields.io/badge/coverage-31.4%25-yellow)
![Mutation](https://img.shields.io/badge/mutation%20score-89.16%25-brightgreen)
![.NET](https://img.shields.io/badge/.NET-10-purple)
![WPF](https://img.shields.io/badge/WPF-Desktop-blue)
![License](https://img.shields.io/badge/license-MIT-green)
```