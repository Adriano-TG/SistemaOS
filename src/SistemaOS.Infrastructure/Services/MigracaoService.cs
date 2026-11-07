using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using SistemaOS.Core.Entities;
using SistemaOS.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace SistemaOS.Infrastructure.Services;

public class MigracaoService
{
    private readonly AppDbContext _dbNovo;

    public MigracaoService(AppDbContext dbNovo)
    {
        _dbNovo = dbNovo;
    }

    // Leitor Seguro
    private string ObterString(SqliteDataReader reader, string nomeColuna)
    {
        try
        {
            int index = reader.GetOrdinal(nomeColuna);
            if (!reader.IsDBNull(index))
                return reader.GetValue(index)?.ToString()?.Trim() ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }

    public void MigrarClientesLegado(string caminhoBancoAntigo)
    {
        var log = new List<string>();
        log.Add("=== RELATÓRIO DE MIGRAÇÃO DO SH OFICINA (SOMENTE CLIENTES) ===");
        log.Add($"Iniciado em: {DateTime.Now}");

        try
        {
            log.Add("\n[1/2] A limpar dados e remover ordens antigas...");

            // Apaga as ordens pesadas que foram importadas no teste anterior
            _dbNovo.Ordens.RemoveRange(_dbNovo.Ordens);
            _dbNovo.SaveChanges();

            _dbNovo.Clientes.RemoveRange(_dbNovo.Clientes);
            _dbNovo.SaveChanges();
            log.Add("Base de dados limpa com sucesso. (Ordens antigas removidas)");

            using var conexaoAntiga = new SqliteConnection($"Data Source={caminhoBancoAntigo}");
            conexaoAntiga.Open();

            // ==========================================
            // FASE 1: APENAS CLIENTES
            // ==========================================
            log.Add("\n[2/2] A importar apenas o cadastro de Clientes...");
            var cmdClientes = conexaoAntiga.CreateCommand();
            cmdClientes.CommandText = "SELECT * FROM CLIENTES";
            using var readerClientes = cmdClientes.ExecuteReader();

            int clientesLidos = 0;

            while (readerClientes.Read())
            {
                try
                {
                    int codIndex = readerClientes.GetOrdinal("CODIGO");
                    if (readerClientes.IsDBNull(codIndex)) continue;

                    var partesEnd = new List<string>();
                    var rua = ObterString(readerClientes, "ENDERECO");
                    var num = ObterString(readerClientes, "NUMERO");
                    if (!string.IsNullOrWhiteSpace(rua)) partesEnd.Add($"{rua}, {num}");

                    var bairro = ObterString(readerClientes, "BAIRRO");
                    if (!string.IsNullOrWhiteSpace(bairro)) partesEnd.Add(bairro);

                    var cidade = ObterString(readerClientes, "CIDADE");
                    if (!string.IsNullOrWhiteSpace(cidade)) partesEnd.Add(cidade);

                    var novoCliente = new Cliente
                    {
                        Nome = ObterString(readerClientes, "NOME"),
                        CpfCnpj = ObterString(readerClientes, "CPF_CNPJ"),
                        Telefone = ObterString(readerClientes, "TELEFONE"),
                        Endereco = string.Join(" - ", partesEnd)
                    };

                    if (string.IsNullOrWhiteSpace(novoCliente.Nome))
                        novoCliente.Nome = "Cliente sem nome";

                    _dbNovo.Clientes.Add(novoCliente);
                    clientesLidos++;
                }
                catch (Exception ex)
                {
                    log.Add($"- Aviso: Falha ao ler um cliente. Motivo: {ex.Message}");
                }
            }

            _dbNovo.SaveChanges();
            log.Add($"> Sucesso: {clientesLidos} clientes guardados na nova base.");

            log.Add("\n=== MIGRAÇÃO FINALIZADA (Histórico de Aparelhos ignorado a seu pedido) ===");
        }
        catch (Exception ex)
        {
            log.Add($"\nERRO FATAL: {ex.Message}");
        }
        finally
        {
            try
            {
                var pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaOS");
                Directory.CreateDirectory(pastaDestino);
                var caminhoLog = Path.Combine(pastaDestino, "Relatorio_Migracao.txt");
                File.WriteAllLines(caminhoLog, log);
                // Process.Start desabilitado para não abrir automaticamente o Notepad
                // Process.Start(new ProcessStartInfo(caminhoLog) { UseShellExecute = true });
            }
            catch { }
        }
    }

    public void ExportarLegadoParaExcel(string caminhoBancoAntigo)
    {
        if (!File.Exists(caminhoBancoAntigo))
            throw new FileNotFoundException("Ficheiro dados_legado.sqlite não encontrado!");

        var pastaDestino = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SistemaOS");
        Directory.CreateDirectory(pastaDestino);
        var caminhoExcel = Path.Combine(pastaDestino, "DadosAntigos_Completo.xlsx");

        using var workbook = new XLWorkbook();
        using var connection = new SqliteConnection($"Data Source={caminhoBancoAntigo}");
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        using var reader = command.ExecuteReader();
        var nomesTabelas = new List<string>();
        while (reader.Read()) nomesTabelas.Add(reader.GetString(0));
        reader.Close();

        foreach (var tabela in nomesTabelas)
        {
            var worksheet = workbook.Worksheets.Add(tabela);
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM {tabela}";
            using var tableReader = cmd.ExecuteReader();

            for (int i = 0; i < tableReader.FieldCount; i++)
            {
                worksheet.Cell(1, i + 1).Value = tableReader.GetName(i);
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
            }

            int linha = 2;
            while (tableReader.Read())
            {
                for (int col = 0; col < tableReader.FieldCount; col++)
                {
                    worksheet.Cell(linha, col + 1).Value = tableReader.IsDBNull(col) ? "" : tableReader.GetValue(col).ToString();
                }
                linha++;
            }
            worksheet.Columns().AdjustToContents();
        }

        workbook.SaveAs(caminhoExcel);
        // Process.Start desabilitado para não abrir automaticamente o Excel
        // Process.Start(new ProcessStartInfo(caminhoExcel) { UseShellExecute = true });
    }
}