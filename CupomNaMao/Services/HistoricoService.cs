using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CupomNaMao.Models;

namespace CupomNaMao.Services
{
    /// <summary>
    /// Serviço para gerenciar o histórico de arquivos XML abertos recentemente.
    /// </summary>
    public class HistoricoService
    {
        private const int MaxItens = 20;
        private readonly string _caminhoArquivo;
        private List<HistoricoItem> _historico = new();

        public HistoricoService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pasta = Path.Combine(appData, "CupomNaMao");
            Directory.CreateDirectory(pasta);
            _caminhoArquivo = Path.Combine(pasta, "historico.json");
            Carregar();
        }

        /// <summary>
        /// Retorna todos os itens do histórico, do mais recente ao mais antigo.
        /// </summary>
        public IReadOnlyList<HistoricoItem> Itens => _historico.AsReadOnly();

        /// <summary>
        /// Adiciona ou atualiza um item no histórico.
        /// </summary>
        public void Adicionar(NfceModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CaminhoArquivo)) return;

            // Remover entrada duplicada se existir
            _historico.RemoveAll(h => h.CaminhoArquivo.Equals(model.CaminhoArquivo, StringComparison.OrdinalIgnoreCase));

            _historico.Insert(0, new HistoricoItem
            {
                CaminhoArquivo = model.CaminhoArquivo,
                NomeEmitente = model.NomeEmitente,
                ValorTotal = model.TotalNota,
                DataEmissao = model.DataEmissao,
                DataAbertura = DateTime.Now
            });

            // Limitar tamanho
            if (_historico.Count > MaxItens)
                _historico = _historico.Take(MaxItens).ToList();

            Salvar();
        }

        /// <summary>
        /// Remove um item do histórico pelo caminho do arquivo.
        /// </summary>
        public void Remover(string caminho)
        {
            _historico.RemoveAll(h => h.CaminhoArquivo.Equals(caminho, StringComparison.OrdinalIgnoreCase));
            Salvar();
        }

        /// <summary>
        /// Limpa todo o histórico.
        /// </summary>
        public void Limpar()
        {
            _historico.Clear();
            Salvar();
        }

        private void Carregar()
        {
            try
            {
                if (!File.Exists(_caminhoArquivo)) return;
                var json = File.ReadAllText(_caminhoArquivo);
                _historico = JsonSerializer.Deserialize<List<HistoricoItem>>(json) ?? new List<HistoricoItem>();
                // Remover entradas de arquivos que não existem mais
                _historico.RemoveAll(h => !File.Exists(h.CaminhoArquivo));
            }
            catch
            {
                _historico = new List<HistoricoItem>();
            }
        }

        private void Salvar()
        {
            try
            {
                var json = JsonSerializer.Serialize(_historico, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_caminhoArquivo, json);
            }
            catch { /* Ignorar erros de escrita */ }
        }
    }
}
